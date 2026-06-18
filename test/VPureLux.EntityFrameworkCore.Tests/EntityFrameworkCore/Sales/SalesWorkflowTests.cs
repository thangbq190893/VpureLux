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
using VPureLux.Pricing;
using VPureLux.Sales;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Sales;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class SalesWorkflowTests : VPureLuxEntityFrameworkCoreTestBase
{
    private readonly ISalesOrderAppService _sales;
    private readonly ICustomerAppService _customers;
    private readonly ICustomerGroupAppService _groups;
    private readonly IWarehouseAppService _warehouses;
    private readonly IComponentAppService _components;
    private readonly IProductAppService _products;
    private readonly IStockItemRepository _stockItems;
    private readonly IInventoryTransactionAppService _inventory;
    private readonly IInventoryQueryAppService _inventoryQuery;
    private readonly IBomAppService _boms;
    private readonly IComponentSuggestedSellingPriceAppService _componentPrices;
    private readonly IProductSuggestedPriceAppService _prices;

    public SalesWorkflowTests()
    {
        _sales = GetRequiredService<ISalesOrderAppService>();
        _customers = GetRequiredService<ICustomerAppService>();
        _groups = GetRequiredService<ICustomerGroupAppService>();
        _warehouses = GetRequiredService<IWarehouseAppService>();
        _components = GetRequiredService<IComponentAppService>();
        _products = GetRequiredService<IProductAppService>();
        _stockItems = GetRequiredService<IStockItemRepository>();
        _inventory = GetRequiredService<IInventoryTransactionAppService>();
        _inventoryQuery = GetRequiredService<IInventoryQueryAppService>();
        _boms = GetRequiredService<IBomAppService>();
        _componentPrices = GetRequiredService<IComponentSuggestedSellingPriceAppService>();
        _prices = GetRequiredService<IProductSuggestedPriceAppService>();
    }

    [Fact]
    public async Task V2_End_To_End_Product_Sale_Should_Expand_Bom_Consume_Fifo_And_Calculate_Snapshots()
    {
        var context = await CreateBaseAsync();
        var pp = await _components.CreateAsync(new CreateComponentDto { Code = Unique("PP"), Name = "PP 1 micron", Unit = "Piece" });
        var cto = await _components.CreateAsync(new CreateComponentDto { Code = Unique("CTO"), Name = "CTO", Unit = "Piece" });
        var ppStockItem = await GetComponentStockItemAsync(pp.Id);
        var ctoStockItem = await GetComponentStockItemAsync(cto.Id);
        await PostReceiptAsync(context.Warehouse.Id, ppStockItem.Id, 5, 10_000, Unique("PPA"));
        await PostReceiptAsync(context.Warehouse.Id, ppStockItem.Id, 5, 12_000, Unique("PPB"));
        await PostReceiptAsync(context.Warehouse.Id, ctoStockItem.Id, 10, 20_000, Unique("CTO"));
        var product = await _products.CreateAsync(new CreateProductDto { Code = Unique("RH8"), Name = "Máy lọc nước RH8" });
        var bom = await _boms.CreateAsync(product.Id, new CreateBomVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Items =
            [
                new CreateBomItemDto { ComponentId = pp.Id, Quantity = 2 },
                new CreateBomItemDto { ComponentId = cto.Id, Quantity = 1 }
            ]
        });
        await _boms.PublishAsync(bom.Id);
        await _componentPrices.CreateAsync(pp.Id, new CreateComponentSuggestedSellingPriceVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Price = 30_000,
            Reason = "UAT component suggested price"
        });
        await _componentPrices.CreateAsync(cto.Id, new CreateComponentSuggestedSellingPriceVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Price = 40_000,
            Reason = "UAT component suggested price"
        });
        var productPrice = await _prices.CreateAsync(product.Id, new CreateProductSuggestedPriceVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Price = 100_000,
            Reason = "UAT product suggested price"
        });

        var order = await _sales.CreateAsync(Input(context, product.Id, 3, null));
        order.Lines.Single().ProductId.ShouldBe(product.Id);
        order.Lines.Single().SuggestedPriceVersionId.ShouldBe(productPrice.Id);
        order.Lines.Single().SuggestedPriceSnapshot.ShouldBe(100_000);

        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });

        var confirmed = await _sales.GetAsync(order.Id);
        var line = confirmed.Lines.Single();
        line.ProductId.ShouldBe(product.Id);
        line.BomVersionId.ShouldBe(bom.Id);
        line.BomVersionNoSnapshot.ShouldBe(bom.VersionNo);
        line.BomSnapshotItems.Count.ShouldBe(2);
        line.BomSnapshotItems.Single(x => x.ComponentId == pp.Id).TotalRequiredQuantity.ShouldBe(6);
        line.BomSnapshotItems.Single(x => x.ComponentId == cto.Id).TotalRequiredQuantity.ShouldBe(3);
        line.CostAmountSnapshot.ShouldBe(122_000);
        line.CostPriceSnapshot.ShouldBe(decimal.Round(122_000m / 3m, SalesConsts.MoneyScale, MidpointRounding.AwayFromZero));
        line.RevenueAmount.ShouldBe(300_000);
        line.ProfitAmount.ShouldBe(178_000);
        line.MarginPercent.ShouldBe(decimal.Round(178_000m / 300_000m * 100m, SalesConsts.MarginScale, MidpointRounding.AwayFromZero));

        var ppBalance = (await _inventoryQuery.GetBalancesAsync(context.Warehouse.Id, ppStockItem.Id)).Single();
        var ctoBalance = (await _inventoryQuery.GetBalancesAsync(context.Warehouse.Id, ctoStockItem.Id)).Single();
        ppBalance.QuantityOnHand.ShouldBe(4);
        ppBalance.InventoryValue.ShouldBe(48_000);
        ctoBalance.QuantityOnHand.ShouldBe(7);
        ctoBalance.InventoryValue.ShouldBe(140_000);

        var ledger = await _inventoryQuery.GetLedgerAsync(context.Warehouse.Id);
        var salesIssue = ledger.Single(x => x.ReferenceType == "SalesOrderLine" && x.ReferenceId == line.Id);
        salesIssue.BomVersionId.ShouldBe(bom.Id);
        salesIssue.TotalIssueCost.ShouldBe(122_000);
        var ppIssueLine = salesIssue.Lines.Single(x => x.StockItemId == ppStockItem.Id);
        var ppFifoLots = await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            return await db.InventoryLots
                .AsNoTracking()
                .Where(x => ppIssueLine.Allocations.Select(a => a.InventoryLotId).Contains(x.Id))
                .OrderBy(x => x.ReceivedAt)
                .ThenBy(x => x.CreationTime)
                .ThenBy(x => x.Id)
                .ToListAsync();
        });
        ppFifoLots.Select(x => ppIssueLine.Allocations.Single(a => a.InventoryLotId == x.Id).Quantity).ToArray()
            .ShouldBe([5, 1]);
        salesIssue.Lines.Single(x => x.StockItemId == ppStockItem.Id).Allocations.Sum(x => x.TotalCost).ShouldBe(62_000);
        salesIssue.Lines.Single(x => x.StockItemId == ctoStockItem.Id).Allocations.Sum(x => x.TotalCost).ShouldBe(60_000);

        var persisted = await GetRequiredService<ISalesOrderRepository>().GetAsync(order.Id, includeDetails: true);
        var persistedLine = persisted.Lines.Single();
        persistedLine.LineType.ShouldBe(SalesOrderLineType.Product);
        persistedLine.ProductId.ShouldBe(product.Id);
        persistedLine.ProductId.ShouldNotBe(pp.Id);
        persistedLine.ProductId.ShouldNotBe(cto.Id);
    }

    [Fact]
    public async Task V2_Loose_Component_Sale_Should_Use_Product_With_One_Component_Bom()
    {
        var context = await CreateBaseAsync();
        var pp = await _components.CreateAsync(new CreateComponentDto { Code = Unique("PP"), Name = "PP bán rời", Unit = "Piece" });
        var stockItem = await GetComponentStockItemAsync(pp.Id);
        await PostReceiptAsync(context.Warehouse.Id, stockItem.Id, 10, 25_000, Unique("PPL"));
        await _componentPrices.CreateAsync(pp.Id, new CreateComponentSuggestedSellingPriceVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Price = 35_000,
            Reason = "Loose component suggested price"
        });
        var looseSku = await _products.CreateAsync(new CreateProductDto
        {
            Code = Unique("LPP"),
            Name = "Lõi PP bán rời"
        });
        var bom = await _boms.CreateAsync(looseSku.Id, new CreateBomVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Items = [new CreateBomItemDto { ComponentId = pp.Id, Quantity = 1 }]
        });
        await _boms.PublishAsync(bom.Id);
        await _prices.CreateAsync(looseSku.Id, new CreateProductSuggestedPriceVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Price = 50_000,
            Reason = "Loose SKU suggested price"
        });

        var order = await _sales.CreateAsync(Input(context, looseSku.Id, 2, null));
        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });

        var confirmed = await _sales.GetAsync(order.Id);
        var line = confirmed.Lines.Single();
        line.ProductId.ShouldBe(looseSku.Id);
        line.BomVersionId.ShouldBe(bom.Id);
        line.BomSnapshotItems.ShouldHaveSingleItem();
        line.BomSnapshotItems.Single().ComponentId.ShouldBe(pp.Id);
        line.BomSnapshotItems.Single().QuantityPerProduct.ShouldBe(1);
        line.BomSnapshotItems.Single().TotalRequiredQuantity.ShouldBe(2);
        line.CostAmountSnapshot.ShouldBe(50_000);
        line.RevenueAmount.ShouldBe(100_000);
        line.ProfitAmount.ShouldBe(50_000);

        var balance = (await _inventoryQuery.GetBalancesAsync(context.Warehouse.Id, stockItem.Id)).Single();
        balance.QuantityOnHand.ShouldBe(8);
        balance.InventoryValue.ShouldBe(200_000);

        var persisted = await GetRequiredService<ISalesOrderRepository>().GetAsync(order.Id, includeDetails: true);
        var persistedLine = persisted.Lines.Single();
        persistedLine.LineType.ShouldBe(SalesOrderLineType.Product);
        persistedLine.ProductId.ShouldBe(looseSku.Id);
        persistedLine.ProductId.ShouldNotBe(pp.Id);
    }

    [Fact]
    public async Task Should_Confirm_Product_Line_Idempotently_And_Calculate_Profit_History()
    {
        var context = await CreateBaseAsync();
        var component = await CreateComponentWithStockAsync(context.Warehouse.Id, 10, 650_000);
        var (product, _) = await CreateProductForComponentAsync(component);
        var order = await _sales.CreateAsync(Input(context, product.Id, 1, 1_000_000));
        var key = Guid.NewGuid().ToString("N");

        var first = await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = key });
        var replay = await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = key });
        var confirmed = await _sales.GetAsync(order.Id);

        replay.SalesOrderId.ShouldBe(first.SalesOrderId);
        confirmed.Status.ShouldBe(SalesOrderStatus.Confirmed);
        confirmed.TotalRevenueAmount.ShouldBe(1_000_000);
        confirmed.TotalCostAmount.ShouldBe(650_000);
        confirmed.TotalProfitAmount.ShouldBe(350_000);
        confirmed.Lines.Single().MarginPercent.ShouldBe(35);
        confirmed.CustomerCodeSnapshot.ShouldBe(context.Customer.Code);
        confirmed.Lines.Single().ItemCodeSnapshot.ShouldBe(product.Code);
        confirmed.Lines.Single().BomSnapshotItems.Single().ComponentCode.ShouldBe(component.Code);

        var history = (await _sales.GetCustomerHistoryAsync(context.Customer.Id)).Single();
        history.LastPurchasePrice.ShouldBe(1_000_000);
        history.AveragePurchasePrice.ShouldBe(1_000_000);
        history.Revenue.ShouldBe(1_000_000);
        history.Profit.ShouldBe(350_000);

        (await Should.ThrowAsync<BusinessException>(() =>
            _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = "different" })))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.DuplicateConfirmationKey);
    }

    [Fact]
    public async Task Should_Confirm_Product_Line_Using_Published_Bom_And_Pricing_Default()
    {
        var context = await CreateBaseAsync();
        var component = await CreateComponentWithStockAsync(context.Warehouse.Id, 20, 30_000);
        var product = await _products.CreateAsync(new CreateProductDto { Code = Unique("SP"), Name = "Sales Product" });
        var bom = await _boms.CreateAsync(product.Id, new CreateBomVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Items = [new CreateBomItemDto { ComponentId = component.Id, Quantity = 2 }]
        });
        await _boms.PublishAsync(bom.Id);
        var price = await _prices.CreateAsync(product.Id, new CreateProductSuggestedPriceVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Price = 100_000,
            Reason = "Initial price"
        });

        var order = await _sales.CreateAsync(Input(context, product.Id, 3, null));
        order.Lines.Single().SuggestedPriceSnapshot.ShouldBe(100_000);
        order.Lines.Single().SuggestedPriceVersionId.ShouldBe(price.Id);

        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });
        var confirmed = await _sales.GetAsync(order.Id);
        var line = confirmed.Lines.Single();
        line.BomVersionId.ShouldBe(bom.Id);
        line.BomVersionNoSnapshot.ShouldBe(bom.VersionNo);
        line.BomSnapshotItems.Single().TotalRequiredQuantity.ShouldBe(6);
        line.CostAmountSnapshot.ShouldBe(180_000);
        line.RevenueAmount.ShouldBe(300_000);
        line.ProfitAmount.ShouldBe(120_000);
    }

    [Fact]
    public async Task Inventory_Failure_Should_Leave_Order_Draft_Without_Snapshot()
    {
        var context = await CreateBaseAsync();
        var component = await _components.CreateAsync(new CreateComponentDto { Code = Unique("NS"), Name = "No Stock", Unit = "Piece" });
        var (product, _) = await CreateProductForComponentAsync(component);
        var order = await _sales.CreateAsync(Input(context, product.Id, 1, 100));

        (await Should.ThrowAsync<BusinessException>(() =>
            _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") })))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.SalesInventoryValidationFailed);

        var reloaded = await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            db.ChangeTracker.Clear();
            return await GetRequiredService<ISalesOrderRepository>().GetAsync(order.Id, includeDetails: true);
        });
        reloaded.Status.ShouldBe(SalesOrderStatus.Draft);
        reloaded.CustomerCodeSnapshot.ShouldBeEmpty();
        reloaded.Lines.Single().InventoryTransactionId.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Validate_Customer_Warehouse_And_Bom()
    {
        var context = await CreateBaseAsync();
        var component = await _components.CreateAsync(new CreateComponentDto { Code = Unique("VC"), Name = "Validation Component", Unit = "Piece" });
        var (validProduct, _) = await CreateProductForComponentAsync(component);
        var valid = Input(context, validProduct.Id, 1, 100);
        valid.CustomerId = Guid.NewGuid();
        (await Should.ThrowAsync<BusinessException>(() => _sales.CreateAsync(valid))).Code.ShouldBe(VPureLuxDomainErrorCodes.CustomerNotFound);

        valid = Input(context, validProduct.Id, 1, 100);
        valid.WarehouseId = Guid.NewGuid();
        (await Should.ThrowAsync<BusinessException>(() => _sales.CreateAsync(valid))).Code.ShouldBe(VPureLuxDomainErrorCodes.WarehouseNotFound);

        var product = await _products.CreateAsync(new CreateProductDto { Code = Unique("VP"), Name = "Validation Product" });
        (await Should.ThrowAsync<BusinessException>(() =>
            _sales.CreateAsync(Input(context, product.Id, 1, 100))))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.SalesBomMustBePublished);
    }

    [Fact]
    public async Task Should_Reject_Inactive_CustomerGroup_And_Warehouse()
    {
        var group = await _groups.CreateAsync(new CreateCustomerGroupDto { Code = Unique("IG"), Name = "Inactive Group" });
        var customer = await _customers.CreateAsync(new CreateCustomerDto { Code = Unique("IC"), Name = "Inactive Group Customer", CustomerGroupId = group.Id });
        var warehouse = await _warehouses.CreateAsync(new CreateWarehouseDto { Code = Unique("IW"), Name = "Inactive Warehouse" });
        var component = await _components.CreateAsync(new CreateComponentDto { Code = Unique("II"), Name = "Validation Component", Unit = "Piece" });
        var (product, _) = await CreateProductForComponentAsync(component);
        await _groups.DeactivateAsync(group.Id);
        var context = (customer, warehouse);
        var order = await _sales.CreateAsync(Input(context, product.Id, 1, 100));
        (await Should.ThrowAsync<BusinessException>(() =>
            _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") })))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.CustomerGroupInactive);

        await _warehouses.DeactivateAsync(warehouse.Id);
        (await Should.ThrowAsync<BusinessException>(() =>
            _sales.CreateAsync(Input(context, product.Id, 1, 100))))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.WarehouseInactive);
    }

    [Fact]
    public async Task Different_Order_Using_Same_Confirmation_Key_Should_Be_Rejected()
    {
        var context = await CreateBaseAsync();
        var component = await CreateComponentWithStockAsync(context.Warehouse.Id, 10, 10);
        var (product, _) = await CreateProductForComponentAsync(component);
        var first = await _sales.CreateAsync(Input(context, product.Id, 1, 20));
        var second = await _sales.CreateAsync(Input(context, product.Id, 1, 20));
        var key = Guid.NewGuid().ToString("N");
        await _sales.ConfirmAsync(first.Id, new ConfirmSalesOrderDto { IdempotencyKey = key });
        (await Should.ThrowAsync<BusinessException>(() =>
            _sales.ConfirmAsync(second.Id, new ConfirmSalesOrderDto { IdempotencyKey = key })))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.DuplicateConfirmationKey);
    }

    [Fact]
    public async Task Stale_Concurrent_Update_Should_Be_Translated_To_Sales_003()
    {
        var context = await CreateBaseAsync();
        var component = await _components.CreateAsync(new CreateComponentDto { Code = Unique("CC"), Name = "Concurrent Component", Unit = "Piece" });
        var (product, _) = await CreateProductForComponentAsync(component);
        var order = await _sales.CreateAsync(Input(context, product.Id, 1, 100));

        var exception = await Should.ThrowAsync<BusinessException>(() => WithUnitOfWorkAsync(async () =>
        {
            var repository = GetRequiredService<ISalesOrderRepository>();
            var aggregate = await repository.GetAsync(order.Id, includeDetails: true);
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            db.Entry(aggregate).Property(x => x.RowVersion).OriginalValue = [99];
            aggregate.CancelDraft(DateTime.UtcNow);
            await repository.UpdateAsync(aggregate, autoSave: true);
        }));
        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.SalesConcurrentModification);
    }

    [Fact]
    public async Task Database_Should_Translate_Duplicate_OrderNo_To_Sales_001()
    {
        var context = await CreateBaseAsync();
        var component = await _components.CreateAsync(new CreateComponentDto { Code = Unique("DO"), Name = "Duplicate Order Component", Unit = "Piece" });
        var (product, _) = await CreateProductForComponentAsync(component);
        var order = await _sales.CreateAsync(Input(context, product.Id, 1, 100));

        var exception = await Should.ThrowAsync<BusinessException>(() => WithUnitOfWorkAsync(async () =>
        {
            var duplicate = await GetRequiredService<SalesManager>().CreateAsync(context.Customer.Id, context.Warehouse.Id, DateTime.UtcNow);
            typeof(SalesOrder).GetProperty(nameof(SalesOrder.OrderNo))!.SetValue(duplicate, order.OrderNo);
            await GetRequiredService<ISalesOrderRepository>().InsertAsync(duplicate, autoSave: true);
        }));
        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.DuplicateOrderNo);
    }

    [Fact]
    public async Task Database_Should_Translate_Duplicate_ConfirmationKey_To_Sales_002()
    {
        var context = await CreateBaseAsync();
        var component = await CreateComponentWithStockAsync(context.Warehouse.Id, 10, 10);
        var (product, bom) = await CreateProductForComponentAsync(component);
        var first = await _sales.CreateAsync(Input(context, product.Id, 1, 20));
        var second = await _sales.CreateAsync(Input(context, product.Id, 1, 20));
        var key = Guid.NewGuid().ToString("N");
        await _sales.ConfirmAsync(first.Id, new ConfirmSalesOrderDto { IdempotencyKey = key });
        var firstConfirmed = await _sales.GetAsync(first.Id);

        var exception = await Should.ThrowAsync<BusinessException>(() => WithUnitOfWorkAsync(async () =>
        {
            var repository = GetRequiredService<ISalesOrderRepository>();
            var aggregate = await repository.GetAsync(second.Id, includeDetails: true);
            aggregate.ApplyCustomerSnapshot(
                context.Customer.Code, context.Customer.Name, context.Customer.CustomerGroupId,
                context.Customer.CustomerGroupCode, context.Customer.CustomerGroupName);
            aggregate.ApplyLineConfirmationSnapshot(
                aggregate.Lines.Single().Id, product.Code, product.Name, SalesConsts.DefaultProductUnit, bom.VersionNo,
                firstConfirmed.Lines.Single().InventoryTransactionId!.Value, 10);
            aggregate.Confirm(key, DateTime.UtcNow);
            await repository.UpdateAsync(aggregate, autoSave: true);
        }));
        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.DuplicateConfirmationKey);
    }

    private async Task<(CustomerDto Customer, WarehouseDto Warehouse)> CreateBaseAsync()
    {
        var group = await _groups.CreateAsync(new CreateCustomerGroupDto { Code = Unique("SG"), Name = "Sales Group" });
        var customer = await _customers.CreateAsync(new CreateCustomerDto { Code = Unique("SC"), Name = "Sales Customer", CustomerGroupId = group.Id });
        var warehouse = await _warehouses.CreateAsync(new CreateWarehouseDto { Code = Unique("SW"), Name = "Sales Warehouse" });
        return (customer, warehouse);
    }

    private async Task<VPureLux.Catalog.Components.ComponentDto> CreateComponentWithStockAsync(Guid warehouseId, decimal quantity, decimal cost)
    {
        var component = await _components.CreateAsync(new CreateComponentDto { Code = Unique("SI"), Name = "Sales Inventory Component", Unit = "Piece" });
        var stockItem = (await _stockItems.FindByCatalogItemAsync(StockItemType.Component, component.Id))!;
        await PostReceiptAsync(warehouseId, stockItem.Id, quantity, cost, Unique("LOT"));
        return component;
    }

    private async Task<StockItem> GetComponentStockItemAsync(Guid componentId) =>
        await _stockItems.FindByCatalogItemAsync(StockItemType.Component, componentId)
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

    private async Task<(ProductDto Product, BomVersionDto Bom)> CreateProductForComponentAsync(
        VPureLux.Catalog.Components.ComponentDto component)
    {
        var product = await _products.CreateAsync(new CreateProductDto { Code = Unique("SP"), Name = $"SKU {component.Code}" });
        var bom = await _boms.CreateAsync(product.Id, new CreateBomVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Items = [new CreateBomItemDto { ComponentId = component.Id, Quantity = 1 }]
        });
        await _boms.PublishAsync(bom.Id);
        return (product, bom);
    }

    private static CreateSalesOrderDto Input(
        (CustomerDto Customer, WarehouseDto Warehouse) context,
        Guid productId,
        decimal quantity,
        decimal? actual) => new()
    {
        CustomerId = context.Customer.Id,
        WarehouseId = context.Warehouse.Id,
        OrderDate = DateTime.Now.Date,
        Lines = [new CreateSalesOrderLineDto { ProductId = productId, Quantity = quantity, ActualSellingPrice = actual }]
    };

    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];
}
