using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using VPureLux.Bom;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using VPureLux.Customers;
using VPureLux.Customers.CustomerGroups;
using VPureLux.Inventory;
using VPureLux.Sales;
using Volo.Abp.EntityFrameworkCore;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Sales;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class SalesOrderPaymentReadModelTests : VPureLuxEntityFrameworkCoreTestBase
{
    private readonly ISalesOrderAppService _sales;
    private readonly ISalesOrderPaymentRepository _payments;
    private readonly ICustomerAppService _customers;
    private readonly ICustomerGroupAppService _groups;
    private readonly IWarehouseAppService _warehouses;
    private readonly IComponentAppService _components;
    private readonly IProductAppService _products;
    private readonly IStockItemRepository _stockItems;
    private readonly IInventoryTransactionAppService _inventory;
    private readonly IBomAppService _boms;

    public SalesOrderPaymentReadModelTests()
    {
        _sales = GetRequiredService<ISalesOrderAppService>();
        _payments = GetRequiredService<ISalesOrderPaymentRepository>();
        _customers = GetRequiredService<ICustomerAppService>();
        _groups = GetRequiredService<ICustomerGroupAppService>();
        _warehouses = GetRequiredService<IWarehouseAppService>();
        _components = GetRequiredService<IComponentAppService>();
        _products = GetRequiredService<IProductAppService>();
        _stockItems = GetRequiredService<IStockItemRepository>();
        _inventory = GetRequiredService<IInventoryTransactionAppService>();
        _boms = GetRequiredService<IBomAppService>();
    }

    [Fact]
    public async Task SalesOrderPayment_Read_Model_Should_Derive_Unpaid_Partial_Paid_And_Ignore_Voided()
    {
        var context = await CreateContextAsync();
        var order = await CreateConfirmedOrderAsync(context, quantity: 2, price: 1_000);
        var confirmed = await _sales.GetAsync(order.Id);
        var inventoryTransactionId = confirmed.Lines.Single().InventoryTransactionId;
        var inventoryTransactionCount = await CountInventoryTransactionsAsync();

        var unpaid = await _sales.GetPaymentSummaryAsync(order.Id);
        unpaid.TotalAmount.ShouldBe(2_000);
        unpaid.PaidAmount.ShouldBe(0);
        unpaid.RemainingAmount.ShouldBe(2_000);
        unpaid.PaymentStatus.ShouldBe(SalesOrderReceivableStatus.Unpaid);
        confirmed.PaymentSummary.PaymentStatus.ShouldBe(SalesOrderReceivableStatus.Unpaid);

        await InsertPaymentAsync(order.Id, context.CustomerId, 500, DateTime.UtcNow.AddMinutes(-20), "PAY-001");
        await InsertPaymentAsync(order.Id, context.CustomerId, 250, DateTime.UtcNow.AddMinutes(-10), "PAY-VOID", SalesOrderPaymentStatus.Voided);

        var partial = await _sales.GetPaymentSummaryAsync(order.Id);
        partial.PaidAmount.ShouldBe(500);
        partial.RemainingAmount.ShouldBe(1_500);
        partial.PaymentStatus.ShouldBe(SalesOrderReceivableStatus.PartiallyPaid);

        await InsertPaymentAsync(order.Id, context.CustomerId, 1_500, DateTime.UtcNow, "PAY-002");

        var paid = await _sales.GetPaymentSummaryAsync(order.Id);
        paid.PaidAmount.ShouldBe(2_000);
        paid.RemainingAmount.ShouldBe(0);
        paid.PaymentStatus.ShouldBe(SalesOrderReceivableStatus.Paid);

        var detail = await _sales.GetAsync(order.Id);
        detail.PaymentSummary.PaidAmount.ShouldBe(2_000);
        detail.PaymentSummary.PaymentStatus.ShouldBe(SalesOrderReceivableStatus.Paid);

        var list = await _sales.GetListAsync(new GetSalesOrderListInput { CustomerId = context.CustomerId });
        list.Items.Single(x => x.Id == order.Id).PaymentSummary.PaymentStatus.ShouldBe(SalesOrderReceivableStatus.Paid);

        var refreshed = await _sales.GetAsync(order.Id);
        refreshed.TotalRevenueAmount.ShouldBe(confirmed.TotalRevenueAmount);
        refreshed.TotalCostAmount.ShouldBe(confirmed.TotalCostAmount);
        refreshed.TotalProfitAmount.ShouldBe(confirmed.TotalProfitAmount);
        refreshed.Lines.Single().InventoryTransactionId.ShouldBe(inventoryTransactionId);
        (await CountInventoryTransactionsAsync()).ShouldBe(inventoryTransactionCount);
    }

    [Fact]
    public async Task SalesOrderPayment_History_Should_Return_Expected_Order()
    {
        var context = await CreateContextAsync();
        var order = await CreateConfirmedOrderAsync(context, quantity: 1, price: 1_000);
        var older = DateTime.UtcNow.AddDays(-2);
        var newer = DateTime.UtcNow.AddDays(-1);
        await InsertPaymentAsync(order.Id, context.CustomerId, 100, older, "PAY-OLDER");
        await InsertPaymentAsync(order.Id, context.CustomerId, 200, newer, "PAY-NEWER");

        var payments = await _sales.GetPaymentsAsync(order.Id);

        payments.Select(x => x.ReferenceNo).Take(2).ToArray().ShouldBe(["PAY-NEWER", "PAY-OLDER"]);
        payments.Single(x => x.ReferenceNo == "PAY-NEWER").Amount.ShouldBe(200);
        payments.Single(x => x.ReferenceNo == "PAY-OLDER").Amount.ShouldBe(100);
    }

    private async Task<SalesOrderDto> CreateConfirmedOrderAsync(
        (Guid CustomerId, Guid WarehouseId, Guid ProductId) context,
        decimal quantity,
        decimal price)
    {
        var order = await _sales.CreateAsync(new CreateSalesOrderDto
        {
            CustomerId = context.CustomerId,
            WarehouseId = context.WarehouseId,
            Lines = [new CreateSalesOrderLineDto { ProductId = context.ProductId, Quantity = quantity, ActualSellingPrice = price }]
        });
        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });
        return await _sales.GetAsync(order.Id);
    }

    private async Task<(Guid CustomerId, Guid WarehouseId, Guid ProductId)> CreateContextAsync()
    {
        var group = await _groups.CreateAsync(new CreateCustomerGroupDto { Code = Unique("PG"), Name = "Payment Group" });
        var customer = await _customers.CreateAsync(new CreateCustomerDto { Code = Unique("PC"), Name = "Payment Customer", CustomerGroupId = group.Id });
        var warehouse = await _warehouses.CreateAsync(new CreateWarehouseDto { Code = Unique("PW"), Name = "Payment Warehouse" });
        var component = await _components.CreateAsync(new CreateComponentDto { Code = Unique("PI"), Name = "Payment Inventory", Unit = "Piece" });
        var stockItem = (await _stockItems.FindByCatalogItemAsync(StockItemType.Component, component.Id))!;
        await _inventory.PostReceiptAsync(new PostReceiptDto
        {
            WarehouseId = warehouse.Id,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines =
            [
                new ReceiptLineInput
                {
                    StockItemId = stockItem.Id,
                    Quantity = 20,
                    UnitCost = 50,
                    LotNo = Unique("PL"),
                    ReceivedAt = DateTime.UtcNow
                }
            ]
        });
        var product = await _products.CreateAsync(new CreateProductDto { Code = Unique("PP"), Name = "Payment Product" });
        var bom = await _boms.CreateAsync(product.Id, new CreateBomVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Items = [new CreateBomItemDto { ComponentId = component.Id, Quantity = 1 }]
        });
        await _boms.PublishAsync(bom.Id);
        return (customer.Id, warehouse.Id, product.Id);
    }

    private async Task InsertPaymentAsync(
        Guid salesOrderId,
        Guid customerId,
        decimal amount,
        DateTime paymentDate,
        string referenceNo,
        SalesOrderPaymentStatus status = SalesOrderPaymentStatus.Posted)
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var payment = new SalesOrderPayment(
                Guid.NewGuid(),
                salesOrderId,
                customerId,
                amount,
                paymentDate,
                SalesPaymentMethod.BankTransfer,
                referenceNo,
                "Read model test payment",
                Guid.NewGuid().ToString("N"));
            if (status != SalesOrderPaymentStatus.Posted)
            {
                typeof(SalesOrderPayment)
                    .GetProperty(nameof(SalesOrderPayment.Status))!
                    .SetValue(payment, status);
            }
            await _payments.InsertAsync(payment, autoSave: true);
        });
    }

    private async Task<int> CountInventoryTransactionsAsync() =>
        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            return await db.InventoryTransactions.CountAsync();
        });

    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];
}
