using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Migrations;
using Shouldly;
using VPureLux.Bom;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using VPureLux.Customers;
using VPureLux.Customers.CustomerGroups;
using VPureLux.Inventory;
using VPureLux.Migrations;
using VPureLux.Permissions;
using VPureLux.Reports;
using VPureLux.Sales;
using Volo.Abp.Authorization.Permissions;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Reports;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class SalesReportsReadModelTests : VPureLuxEntityFrameworkCoreTestBase
{
    private readonly ISalesReportsAppService _reports;
    private readonly ISalesOrderAppService _sales;
    private readonly ICustomerAppService _customers;
    private readonly ICustomerGroupAppService _groups;
    private readonly IWarehouseAppService _warehouses;
    private readonly IComponentAppService _components;
    private readonly IProductAppService _products;
    private readonly IStockItemRepository _stockItems;
    private readonly IInventoryTransactionAppService _inventory;
    private readonly IBomAppService _boms;
    private readonly ISalesOrderPaymentRepository _payments;

    public SalesReportsReadModelTests()
    {
        _reports = GetRequiredService<ISalesReportsAppService>();
        _sales = GetRequiredService<ISalesOrderAppService>();
        _customers = GetRequiredService<ICustomerAppService>();
        _groups = GetRequiredService<ICustomerGroupAppService>();
        _warehouses = GetRequiredService<IWarehouseAppService>();
        _components = GetRequiredService<IComponentAppService>();
        _products = GetRequiredService<IProductAppService>();
        _stockItems = GetRequiredService<IStockItemRepository>();
        _inventory = GetRequiredService<IInventoryTransactionAppService>();
        _boms = GetRequiredService<IBomAppService>();
        _payments = GetRequiredService<ISalesOrderPaymentRepository>();
    }

    [Fact]
    public async Task Should_Define_Report_Permissions()
    {
        var manager = GetRequiredService<IPermissionDefinitionManager>();

        (await manager.GetAsync(VPureLuxPermissions.Reports.Default)).ShouldNotBeNull();
        (await manager.GetAsync(VPureLuxPermissions.Reports.Sales.View)).ShouldNotBeNull();
        (await manager.GetAsync(VPureLuxPermissions.Reports.Profit.View)).ShouldNotBeNull();
        (await manager.GetAsync(VPureLuxPermissions.Reports.Export)).ShouldNotBeNull();
    }

    [Fact]
    public void Sales_Report_Stored_Procedure_Migration_Should_Be_Discoverable()
    {
        var attribute = typeof(AddSalesReportStoredProcedures)
            .GetCustomAttributes(typeof(MigrationAttribute), false)
            .Single()
            .ShouldBeOfType<MigrationAttribute>();

        attribute.Id.ShouldBe("20260803090000_AddSalesReportStoredProcedures");
    }

    [Fact]
    public async Task SalesRevenueReport_Should_Count_Confirmed_Sales_And_Posted_Payments_Only()
    {
        var context = await CreateBaseAsync();
        var component = await CreateComponentWithStockAsync(context.Warehouse.Id, 10, 500);
        var (product, _) = await CreateProductForComponentAsync(component);
        var confirmed = await CreateConfirmedOrderAsync(context, product.Id, 2, 1_000);
        var draft = await _sales.CreateAsync(Input(context, product.Id, 1, 1_000));
        await InsertPaymentAsync(confirmed.Id, context.Customer.Id, 500, SalesOrderPaymentStatus.Posted);
        await InsertPaymentAsync(confirmed.Id, context.Customer.Id, 800, SalesOrderPaymentStatus.Voided);

        var report = await _reports.GetSalesRevenueAsync(new SalesRevenueReportInput
        {
            FromDate = DateTime.Today.AddDays(-1),
            ToDate = DateTime.Today.AddDays(1),
            GroupBy = ReportPeriodGroup.Day
        });

        report.Summary.TotalRevenue.ShouldBe(2_000);
        report.Summary.ConfirmedOrderCount.ShouldBe(1);
        report.Summary.TotalQuantity.ShouldBe(2);
        report.Summary.AverageOrderValue.ShouldBe(2_000);
        report.Summary.PaidAmount.ShouldBe(500);
        report.Summary.RemainingAmount.ShouldBe(1_500);
        report.Summary.PartiallyPaidOrderCount.ShouldBe(1);
        report.Orders.ShouldHaveSingleItem().SalesOrderId.ShouldBe(confirmed.Id);
        report.Orders.Single().OrderNo.ShouldBe(confirmed.OrderNo);
        report.Orders.Single().PaymentStatus.ShouldBe(SalesOrderReceivableStatus.PartiallyPaid);
        report.Orders.Select(x => x.SalesOrderId).ShouldNotContain(draft.Id);
        report.ByProduct.ShouldHaveSingleItem().ProductId.ShouldBe(product.Id);
        report.ByCustomer.ShouldHaveSingleItem().CustomerId.ShouldBe(context.Customer.Id);
        report.ByPeriod.ShouldHaveSingleItem().Revenue.ShouldBe(2_000);

        var byProduct = await _reports.GetSalesRevenueAsync(new SalesRevenueReportInput
        {
            FromDate = DateTime.Today.AddDays(-1),
            ToDate = DateTime.Today.AddDays(1),
            ProductId = product.Id
        });
        byProduct.Summary.TotalRevenue.ShouldBe(2_000);

        var byCustomer = await _reports.GetSalesRevenueAsync(new SalesRevenueReportInput
        {
            FromDate = DateTime.Today.AddDays(-1),
            ToDate = DateTime.Today.AddDays(1),
            CustomerId = context.Customer.Id
        });
        byCustomer.Summary.ConfirmedOrderCount.ShouldBe(1);

        var byWarehouse = await _reports.GetSalesRevenueAsync(new SalesRevenueReportInput
        {
            FromDate = DateTime.Today.AddDays(-1),
            ToDate = DateTime.Today.AddDays(1),
            WarehouseId = context.Warehouse.Id
        });
        byWarehouse.Summary.TotalQuantity.ShouldBe(2);

        var byMonth = await _reports.GetSalesRevenueAsync(new SalesRevenueReportInput
        {
            FromDate = DateTime.Today.AddDays(-1),
            ToDate = DateTime.Today.AddDays(1),
            GroupBy = ReportPeriodGroup.Month
        });
        byMonth.ByPeriod.ShouldHaveSingleItem().PeriodKey.ShouldContain(DateTime.Today.ToString("yyyy-MM"));

        var paidOnly = await _reports.GetSalesRevenueAsync(new SalesRevenueReportInput
        {
            FromDate = DateTime.Today.AddDays(-1),
            ToDate = DateTime.Today.AddDays(1),
            PaymentStatus = SalesOrderReceivableStatus.Paid
        });
        paidOnly.Summary.ConfirmedOrderCount.ShouldBe(0);

        var outsideRange = await _reports.GetSalesRevenueAsync(new SalesRevenueReportInput
        {
            FromDate = DateTime.Today.AddDays(2),
            ToDate = DateTime.Today.AddDays(3)
        });
        outsideRange.Summary.TotalRevenue.ShouldBe(0);
    }

    [Fact]
    public async Task SalesProfitReport_Should_Use_Confirmation_Snapshots_And_Filter_Losses()
    {
        var context = await CreateBaseAsync();
        var component = await CreateComponentWithStockAsync(context.Warehouse.Id, 10, 1_000);
        var (product, _) = await CreateProductForComponentAsync(component);
        var profitable = await CreateConfirmedOrderAsync(context, product.Id, 1, 1_500);
        var loss = await CreateConfirmedOrderAsync(context, product.Id, 1, 500);
        await _sales.CreateAsync(Input(context, product.Id, 1, 1_500));
        await InsertPaymentAsync(profitable.Id, context.Customer.Id, 500, SalesOrderPaymentStatus.Posted);
        await PostReceiptAsync(context.Warehouse.Id, await GetComponentStockItemIdAsync(component.Id), 1, 9_999, Unique("AFTER"));

        var report = await _reports.GetSalesProfitAsync(new SalesProfitReportInput
        {
            FromDate = DateTime.Today.AddDays(-1),
            ToDate = DateTime.Today.AddDays(1),
            GroupBy = ReportPeriodGroup.Day
        });

        report.Summary.Revenue.ShouldBe(2_000);
        report.Summary.CostAmount.ShouldBe(2_000);
        report.Summary.ProfitAmount.ShouldBe(0);
        report.Summary.ProfitMarginPercent.ShouldBe(0);
        report.Summary.ConfirmedOrderCount.ShouldBe(2);
        report.Summary.LossOrderCount.ShouldBe(1);
        report.Summary.MissingCostLineCount.ShouldBe(0);
        report.Lines.Count.ShouldBe(2);
        report.Lines.Single(x => x.SalesOrderId == profitable.Id).CostAmount.ShouldBe(1_000);
        report.Lines.Single(x => x.SalesOrderId == loss.Id).ProfitAmount.ShouldBe(-500);
        report.ByProduct.ShouldHaveSingleItem().ProfitAmount.ShouldBe(0);
        report.ByCustomer.ShouldHaveSingleItem().RemainingAmount.ShouldBe(1_500);
        report.ByPeriod.ShouldHaveSingleItem().Revenue.ShouldBe(2_000);

        var lossOnly = await _reports.GetSalesProfitAsync(new SalesProfitReportInput
        {
            FromDate = DateTime.Today.AddDays(-1),
            ToDate = DateTime.Today.AddDays(1),
            LossOnly = true
        });
        lossOnly.Summary.Revenue.ShouldBe(500);
        lossOnly.Summary.ProfitAmount.ShouldBe(-500);
        lossOnly.Lines.ShouldHaveSingleItem().SalesOrderId.ShouldBe(loss.Id);

        var missingCostOnly = await _reports.GetSalesProfitAsync(new SalesProfitReportInput
        {
            FromDate = DateTime.Today.AddDays(-1),
            ToDate = DateTime.Today.AddDays(1),
            MissingCostOnly = true
        });
        missingCostOnly.Summary.ConfirmedOrderCount.ShouldBe(0);

        var byMonth = await _reports.GetSalesProfitAsync(new SalesProfitReportInput
        {
            FromDate = DateTime.Today.AddDays(-1),
            ToDate = DateTime.Today.AddDays(1),
            GroupBy = ReportPeriodGroup.Month,
            ProductId = product.Id,
            CustomerId = context.Customer.Id,
            WarehouseId = context.Warehouse.Id
        });
        byMonth.ByPeriod.ShouldHaveSingleItem().PeriodKey.ShouldContain(DateTime.Today.ToString("yyyy-MM"));
    }

    private async Task<SalesOrderDto> CreateConfirmedOrderAsync(
        (CustomerDto Customer, WarehouseDto Warehouse) context,
        Guid productId,
        decimal quantity,
        decimal actualPrice)
    {
        var order = await _sales.CreateAsync(Input(context, productId, quantity, actualPrice));
        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });
        return await _sales.GetAsync(order.Id);
    }

    private async Task<(CustomerDto Customer, WarehouseDto Warehouse)> CreateBaseAsync()
    {
        var group = await _groups.CreateAsync(new CreateCustomerGroupDto { Code = Unique("RG"), Name = "Report Group" });
        var customer = await _customers.CreateAsync(new CreateCustomerDto { Code = Unique("RC"), Name = "Report Customer", CustomerGroupId = group.Id });
        var warehouse = await _warehouses.CreateAsync(new CreateWarehouseDto { Code = Unique("RW"), Name = "Report Warehouse" });
        return (customer, warehouse);
    }

    private async Task<ComponentDto> CreateComponentWithStockAsync(Guid warehouseId, decimal quantity, decimal cost)
    {
        var component = await _components.CreateAsync(new CreateComponentDto { Code = Unique("RI"), Name = "Report Material", Unit = "Piece" });
        await PostReceiptAsync(warehouseId, await GetComponentStockItemIdAsync(component.Id), quantity, cost, Unique("LOT"));
        return component;
    }

    private async Task<Guid> GetComponentStockItemIdAsync(Guid componentId) =>
        (await _stockItems.FindByCatalogItemAsync(StockItemType.Component, componentId))?.Id
        ?? throw new InvalidOperationException($"Component StockItem was not synchronized for {componentId}.");

    private async Task PostReceiptAsync(Guid warehouseId, Guid stockItemId, decimal quantity, decimal unitCost, string lotNo)
    {
        await _inventory.PostReceiptAsync(new PostReceiptDto
        {
            WarehouseId = warehouseId,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines =
            [
                new ReceiptLineInput
                {
                    StockItemId = stockItemId,
                    Quantity = quantity,
                    UnitCost = unitCost,
                    LotNo = lotNo,
                    ReceivedAt = DateTime.UtcNow
                }
            ]
        });
    }

    private async Task<(ProductDto Product, BomVersionDto Bom)> CreateProductForComponentAsync(ComponentDto component)
    {
        var product = await _products.CreateAsync(new CreateProductDto { Code = Unique("RP"), Name = $"Report SKU {component.Code}" });
        var bom = await _boms.CreateAsync(product.Id, new CreateBomVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Items = [new CreateBomItemDto { ComponentId = component.Id, Quantity = 1 }]
        });
        await _boms.PublishAsync(bom.Id);
        return (product, bom);
    }

    private async Task InsertPaymentAsync(
        Guid salesOrderId,
        Guid customerId,
        decimal amount,
        SalesOrderPaymentStatus status)
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var payment = new SalesOrderPayment(
                Guid.NewGuid(),
                salesOrderId,
                customerId,
                amount,
                DateTime.UtcNow,
                SalesPaymentMethod.BankTransfer,
                Unique("PAY"),
                "Report test payment",
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

    private static CreateSalesOrderDto Input(
        (CustomerDto Customer, WarehouseDto Warehouse) context,
        Guid productId,
        decimal quantity,
        decimal actual) => new()
    {
        CustomerId = context.Customer.Id,
        WarehouseId = context.Warehouse.Id,
        OrderDate = DateTime.Now.Date,
        Lines = [new CreateSalesOrderLineDto { ProductId = productId, Quantity = quantity, ActualSellingPrice = actual }]
    };

    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];
}
