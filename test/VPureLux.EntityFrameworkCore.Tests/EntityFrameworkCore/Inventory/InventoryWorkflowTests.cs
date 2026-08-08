using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Shouldly;
using VPureLux.Catalog;
using VPureLux.Catalog.Components;
using VPureLux.Inventory;
using VPureLux.Suppliers;
using Volo.Abp;
using Volo.Abp.Validation;
using Volo.Abp.Timing;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Inventory;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class InventoryWorkflowTests : VPureLuxEntityFrameworkCoreTestBase
{
    private readonly IComponentAppService _components;
    private readonly IStockItemRepository _stockItems;
    private readonly IComponentRepository _componentRepository;
    private readonly IWarehouseAppService _warehouses;
    private readonly IInventoryTransactionAppService _transactions;
    private readonly IInventoryQueryAppService _queries;
    private readonly ISupplierAppService _suppliers;
    private readonly IInventoryLotSupplierRepository _lotSuppliers;
    private readonly IDistributedCache _cache;
    private readonly IClock _clock;

    public InventoryWorkflowTests()
    {
        _components = GetRequiredService<IComponentAppService>();
        _stockItems = GetRequiredService<IStockItemRepository>();
        _componentRepository = GetRequiredService<IComponentRepository>();
        _warehouses = GetRequiredService<IWarehouseAppService>();
        _transactions = GetRequiredService<IInventoryTransactionAppService>();
        _queries = GetRequiredService<IInventoryQueryAppService>();
        _suppliers = GetRequiredService<ISupplierAppService>();
        _lotSuppliers = GetRequiredService<IInventoryLotSupplierRepository>();
        _cache = GetRequiredService<IDistributedCache>();
        _clock = GetRequiredService<IClock>();
    }

    [Fact]
    public async Task Should_Synchronize_Component_StockItem_And_Status()
    {
        var component = await CreateComponentAsync();
        var item = (await _stockItems.FindByCatalogItemAsync(StockItemType.Component, component.Id))!;
        item.IsInventoryEnabled.ShouldBeTrue();
        await _components.DeactivateAsync(component.Id);
        (await _stockItems.FindByCatalogItemAsync(StockItemType.Component, component.Id))!.Status.ShouldBe(InventoryEntityStatus.Inactive);
        var aggregate = await _componentRepository.GetAsync(component.Id);
        aggregate.Activate();
        await _componentRepository.UpdateAsync(aggregate, autoSave: true);
        (await _stockItems.FindByCatalogItemAsync(StockItemType.Component, component.Id))!.Status.ShouldBe(InventoryEntityStatus.Active);
    }

    [Fact]
    public async Task Should_Deactivate_StockItem_When_Component_Is_Soft_Deleted()
    {
        var component = await CreateComponentAsync();
        (await _stockItems.FindByCatalogItemAsync(StockItemType.Component, component.Id)).ShouldNotBeNull();
        await _componentRepository.DeleteAsync(component.Id, autoSave: true);
        (await _stockItems.FindByCatalogItemAsync(StockItemType.Component, component.Id))!.Status
            .ShouldBe(InventoryEntityStatus.Inactive);
    }

    [Fact]
    public async Task Should_Post_Receipt_Issue_Adjustment_And_Reconcile_Balance()
    {
        var context = await CreateContextAsync();
        await ReceiptAsync(context.WarehouseId, context.StockItemId, 100, 30000, "LOT-100");
        var issue = await IssueAsync(context.WarehouseId, context.StockItemId, 30);
        await AdjustmentIncreaseAsync(context.WarehouseId, context.StockItemId, 10, 25000, "LOT-ADJ");

        issue.TotalIssueCost.ShouldBe(900000);
        var balance = (await _queries.GetBalancesAsync(context.WarehouseId, context.StockItemId)).Single();
        balance.QuantityOnHand.ShouldBe(80);
        var rebuilt = (await _queries.GetLedgerAsync(context.WarehouseId, context.StockItemId))
            .SelectMany(x => x.Lines).Sum(x => x.Direction == InventoryMovementDirection.Increase ? x.Quantity : -x.Quantity);
        rebuilt.ShouldBe(80);
    }

    [Fact]
    public async Task Should_Allocate_Multiple_Lots_In_Deterministic_FIFO_Order()
    {
        var context = await CreateContextAsync();
        await ReceiptAsync(context.WarehouseId, context.StockItemId, 10, 30000, "LOT-1", DateTime.UtcNow.AddDays(-3));
        await ReceiptAsync(context.WarehouseId, context.StockItemId, 10, 25000, "LOT-2", DateTime.UtcNow.AddDays(-2));
        await ReceiptAsync(context.WarehouseId, context.StockItemId, 10, 20000, "LOT-3", DateTime.UtcNow.AddDays(-1));

        var issue = await IssueAsync(context.WarehouseId, context.StockItemId, 25);

        issue.TotalIssueCost.ShouldBe(650000);
        issue.Allocations.Select(x => x.Quantity).ShouldBe(new[] { 10m, 10m, 5m });
        issue.Allocations.Select(x => x.UnitCost).ShouldBe(new[] { 30000m, 25000m, 20000m });
    }

    [Fact]
    public async Task Should_Reject_Insufficient_Stock_Without_Negative_Balance()
    {
        var context = await CreateContextAsync();
        await ReceiptAsync(context.WarehouseId, context.StockItemId, 5, 100, "LOT-SHORT");
        (await Should.ThrowAsync<BusinessException>(() => IssueAsync(context.WarehouseId, context.StockItemId, 6)))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.InsufficientInventory);
        (await _queries.GetBalancesAsync(context.WarehouseId, context.StockItemId)).Single().QuantityOnHand.ShouldBe(5);
    }

    [Fact]
    public async Task Should_Return_Same_Result_For_Same_Idempotency_Request_And_Reject_Conflict()
    {
        var context = await CreateContextAsync();
        var key = Guid.NewGuid().ToString("N");
        var input = ReceiptInput(context.WarehouseId, context.StockItemId, 10, 100, "LOT-IDEM", key);
        var first = await _transactions.PostReceiptAsync(input);
        var second = await _transactions.PostReceiptAsync(input);
        second.Id.ShouldBe(first.Id);
        input.Lines[0].Quantity = 11;
        (await Should.ThrowAsync<BusinessException>(() => _transactions.PostReceiptAsync(input)))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.InventoryIdempotencyConflict);
    }

    [Fact]
    public async Task Should_Generate_Receipt_LotNo_When_Blank()
    {
        await ResetInventoryLotSequenceAsync();
        var context = await CreateContextAsync();

        var receipt = await _transactions.PostReceiptAsync(new PostReceiptDto
        {
            WarehouseId = context.WarehouseId,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines =
            [
                new ReceiptLineInput
                {
                    StockItemId = context.StockItemId,
                    Quantity = 3,
                    UnitCost = 100,
                    ReceivedAt = DateTime.UtcNow
                }
            ]
        });

        receipt.Lines.Single().LotNo.ShouldBe($"LOT-{DatePart()}0001");
    }

    [Fact]
    public async Task Receipt_With_Multiple_Items_Should_Use_One_Shared_LotNo_And_ReceivedAt()
    {
        await ResetInventoryLotSequenceAsync();
        var warehouse = await CreateWarehouseAsync();
        var firstComponent = await CreateComponentAsync();
        var secondComponent = await CreateComponentAsync();
        var firstItem = (await _stockItems.FindByCatalogItemAsync(StockItemType.Component, firstComponent.Id))!;
        var secondItem = (await _stockItems.FindByCatalogItemAsync(StockItemType.Component, secondComponent.Id))!;
        var receivedAt = new DateTime(2026, 6, 18);

        var receipt = await _transactions.PostReceiptAsync(new PostReceiptDto
        {
            WarehouseId = warehouse.Id,
            ReceivedAt = receivedAt,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines =
            [
                new ReceiptLineInput { StockItemId = firstItem.Id, Quantity = 3, UnitCost = 100 },
                new ReceiptLineInput { StockItemId = secondItem.Id, Quantity = 4, UnitCost = 200 }
            ]
        });

        receipt.Lines.Select(x => x.LotNo).Distinct().Single().ShouldBe($"LOT-{DatePart()}0001");
        receipt.Lines.ShouldAllBe(x => x.ReceivedAt!.Value.Date == receivedAt);
        (await _queries.GetLotsAsync(warehouse.Id, firstItem.Id)).Single().LotNo.ShouldBe($"LOT-{DatePart()}0001");
        (await _queries.GetLotsAsync(warehouse.Id, secondItem.Id)).Single().LotNo.ShouldBe($"LOT-{DatePart()}0001");
    }

    [Fact]
    public async Task Receipt_With_Supplier_Should_Create_Lot_Supplier_Link_And_Query_Snapshot()
    {
        var context = await CreateContextAsync();
        var supplier = await _suppliers.CreateAsync(new CreateSupplierDto
        {
            Code = Unique("SUP"),
            Name = "Receipt Supplier"
        });

        await _transactions.PostReceiptAsync(new PostReceiptDto
        {
            WarehouseId = context.WarehouseId,
            SupplierId = supplier.Id,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines =
            [
                new ReceiptLineInput
                {
                    StockItemId = context.StockItemId,
                    Quantity = 3,
                    UnitCost = 100,
                    LotNo = Unique("SUP-LOT"),
                    ReceivedAt = DateTime.UtcNow
                }
            ]
        });

        var lot = (await _queries.GetLotsAsync(context.WarehouseId, context.StockItemId)).Single();
        lot.SupplierId.ShouldBe(supplier.Id);
        lot.SupplierCode.ShouldBe(supplier.Code);
        lot.SupplierName.ShouldBe(supplier.Name);

        var links = await _lotSuppliers.GetListByLotIdsAsync([lot.Id]);
        links.Single().SupplierId.ShouldBe(supplier.Id);
        links.Single().SupplierCodeSnapshot.ShouldBe(supplier.Code);
        links.Single().SupplierNameSnapshot.ShouldBe(supplier.Name);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Should_Reject_Non_Positive_Receipt_Quantity_Before_Posting_And_LotNo_Generation(int quantity)
    {
        await ResetInventoryLotSequenceAsync();
        var context = await CreateContextAsync();

        await Should.ThrowAsync<AbpValidationException>(() => _transactions.PostReceiptAsync(new PostReceiptDto
        {
            WarehouseId = context.WarehouseId,
            SupplierId = null,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines =
            [
                new ReceiptLineInput
                {
                    StockItemId = context.StockItemId,
                    Quantity = quantity,
                    UnitCost = 100,
                    ReceivedAt = DateTime.UtcNow
                }
            ]
        }));

        (await _queries.GetLedgerAsync(context.WarehouseId, context.StockItemId)).ShouldBeEmpty();
        (await _queries.GetLotsAsync(context.WarehouseId, context.StockItemId)).ShouldBeEmpty();
        (await _cache.GetStringAsync($"Sequence:InventoryLot:{DatePart()}")).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Seed_Receipt_LotNo_From_Existing_Max_Suffix()
    {
        await ResetInventoryLotSequenceAsync();
        var context = await CreateContextAsync();
        var datePart = DatePart();
        await ReceiptAsync(context.WarehouseId, context.StockItemId, 1, 100, $"LOT-{datePart}0003");
        await ReceiptAsync(context.WarehouseId, context.StockItemId, 1, 100, $"LOT-{datePart}0009");

        var receipt = await _transactions.PostReceiptAsync(new PostReceiptDto
        {
            WarehouseId = context.WarehouseId,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines =
            [
                new ReceiptLineInput
                {
                    StockItemId = context.StockItemId,
                    Quantity = 1,
                    UnitCost = 100,
                    ReceivedAt = DateTime.UtcNow
                }
            ]
        });

        receipt.Lines.Single().LotNo.ShouldBe($"LOT-{datePart}0010");
    }

    [Fact]
    public async Task Should_Retry_Receipt_LotNo_Collision_From_Cache()
    {
        await ResetInventoryLotSequenceAsync();
        var context = await CreateContextAsync();
        var datePart = DatePart();
        await ReceiptAsync(context.WarehouseId, context.StockItemId, 1, 100, $"LOT-{datePart}0001");
        await _cache.SetStringAsync($"Sequence:InventoryLot:{datePart}", "0");

        var receipt = await _transactions.PostReceiptAsync(new PostReceiptDto
        {
            WarehouseId = context.WarehouseId,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines =
            [
                new ReceiptLineInput
                {
                    StockItemId = context.StockItemId,
                    Quantity = 1,
                    UnitCost = 100,
                    ReceivedAt = DateTime.UtcNow
                }
            ]
        });

        receipt.Lines.Single().LotNo.ShouldBe($"LOT-{datePart}0002");
    }

    [Fact]
    public async Task Should_Keep_Explicit_Receipt_LotNo()
    {
        var context = await CreateContextAsync();
        var explicitLotNo = Unique("MANUAL-LOT");

        var receipt = await ReceiptAsync(context.WarehouseId, context.StockItemId, 2, 100, explicitLotNo);

        receipt.Lines.Single().LotNo.ShouldBe(explicitLotNo);
    }

    [Fact]
    public async Task Should_Generate_Adjustment_Increase_LotNo_When_Blank()
    {
        await ResetInventoryLotSequenceAsync();
        var context = await CreateContextAsync();

        var adjustment = await _transactions.PostAdjustmentAsync(new PostAdjustmentDto
        {
            WarehouseId = context.WarehouseId,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Type = InventoryTransactionType.AdjustmentIncrease,
            Reason = "Count correction",
            IncreaseLines =
            [
                new ReceiptLineInput
                {
                    StockItemId = context.StockItemId,
                    Quantity = 2,
                    UnitCost = 100,
                    ReceivedAt = DateTime.UtcNow
                }
            ]
        });

        adjustment.Lines.Single().LotNo.ShouldBe($"LOT-{DatePart()}0001");
    }

    [Fact]
    public async Task Should_Not_Generate_LotNo_For_Adjustment_Decrease()
    {
        await ResetInventoryLotSequenceAsync();
        var context = await CreateContextAsync();
        await ReceiptAsync(context.WarehouseId, context.StockItemId, 5, 100, Unique("LOT-NEG-SEED"));

        var adjustment = await _transactions.PostAdjustmentAsync(new PostAdjustmentDto
        {
            WarehouseId = context.WarehouseId,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Type = InventoryTransactionType.AdjustmentDecrease,
            Reason = "Count decrease",
            DecreaseLines =
            [
                new IssueLineInput
                {
                    StockItemId = context.StockItemId,
                    Quantity = 2
                }
            ]
        });

        adjustment.Lines.Single().LotNo.ShouldBeNull();
        (await _cache.GetStringAsync($"Sequence:InventoryLot:{DatePart()}")).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Return_Same_Result_For_Idempotent_Blank_LotNo_Receipt()
    {
        await ResetInventoryLotSequenceAsync();
        var context = await CreateContextAsync();
        var key = Guid.NewGuid().ToString("N");
        var input = new PostReceiptDto
        {
            WarehouseId = context.WarehouseId,
            IdempotencyKey = key,
            Lines =
            [
                new ReceiptLineInput
                {
                    StockItemId = context.StockItemId,
                    Quantity = 1,
                    UnitCost = 100,
                    ReceivedAt = DateTime.UtcNow
                }
            ]
        };

        var first = await _transactions.PostReceiptAsync(input);
        var second = await _transactions.PostReceiptAsync(input);

        second.Id.ShouldBe(first.Id);
        second.Lines.Single().LotNo.ShouldBe(first.Lines.Single().LotNo);
    }

    [Fact]
    public async Task Should_Reject_Product_Inventory_Operations()
    {
        var productService = GetRequiredService<VPureLux.Catalog.Products.IProductAppService>();
        var product = await productService.CreateAsync(new VPureLux.Catalog.Products.CreateProductDto { Code = Unique("INV-P"), Name = "Product" });
        var item = (await _stockItems.FindByCatalogItemAsync(StockItemType.Product, product.Id))!;
        item.IsInventoryEnabled.ShouldBeFalse();
        var warehouse = await CreateWarehouseAsync();
        (await Should.ThrowAsync<BusinessException>(() => ReceiptAsync(warehouse.Id, item.Id, 1, 100, "P-LOT")))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.StockItemInventoryDisabled);
    }

    [Fact]
    public async Task StockItem_List_Should_Filter_Component_Inventory_Enabled_Items_Before_Paging()
    {
        var marker = Unique("INV-SEL");
        var productService = GetRequiredService<VPureLux.Catalog.Products.IProductAppService>();
        var product = await productService.CreateAsync(new VPureLux.Catalog.Products.CreateProductDto
        {
            Code = $"{marker}-A",
            Name = "Selector Product"
        });
        var component = await _components.CreateAsync(new CreateComponentDto
        {
            Code = $"{marker}-Z",
            Name = "Selector Component",
            Unit = "Piece"
        });
        var productItem = (await _stockItems.FindByCatalogItemAsync(StockItemType.Product, product.Id))!;
        var componentItem = (await _stockItems.FindByCatalogItemAsync(StockItemType.Component, component.Id))!;

        var result = await GetRequiredService<IStockItemAppService>().GetListAsync(new GetInventoryListInput
        {
            SearchText = marker,
            Status = InventoryEntityStatus.Active,
            ItemType = StockItemType.Component,
            IsInventoryEnabled = true,
            MaxResultCount = 1
        });

        result.TotalCount.ShouldBe(1);
        result.Items.Select(x => x.Id).ShouldContain(componentItem.Id);
        result.Items.Select(x => x.Id).ShouldNotContain(productItem.Id);
        result.Items.ShouldAllBe(x =>
            x.ItemType == StockItemType.Component &&
            x.IsInventoryEnabled &&
            x.Status == InventoryEntityStatus.Active);
    }

    private async Task<(Guid WarehouseId, Guid StockItemId)> CreateContextAsync()
    {
        var component = await CreateComponentAsync();
        var item = (await _stockItems.FindByCatalogItemAsync(StockItemType.Component, component.Id))!;
        return ((await CreateWarehouseAsync()).Id, item.Id);
    }

    private Task<ComponentDto> CreateComponentAsync() => _components.CreateAsync(new CreateComponentDto { Code = Unique("INV-C"), Name = "Inventory Component", Unit = "Piece" });
    private Task<WarehouseDto> CreateWarehouseAsync() => _warehouses.CreateAsync(new CreateWarehouseDto { Code = Unique("WH"), Name = "Warehouse" });
    private Task<InventoryTransactionDto> ReceiptAsync(Guid warehouseId, Guid itemId, decimal qty, decimal cost, string lot, DateTime? receivedAt = null) =>
        _transactions.PostReceiptAsync(ReceiptInput(warehouseId, itemId, qty, cost, lot, Guid.NewGuid().ToString("N"), receivedAt));
    private Task<IssueCostResultDto> IssueAsync(Guid warehouseId, Guid itemId, decimal qty) =>
        _transactions.PostIssueAsync(new PostIssueDto { WarehouseId = warehouseId, IdempotencyKey = Guid.NewGuid().ToString("N"), Lines = [new IssueLineInput { StockItemId = itemId, Quantity = qty }] });
    private Task<InventoryTransactionDto> AdjustmentIncreaseAsync(Guid warehouseId, Guid itemId, decimal qty, decimal cost, string lot) =>
        _transactions.PostAdjustmentAsync(new PostAdjustmentDto { WarehouseId = warehouseId, IdempotencyKey = Guid.NewGuid().ToString("N"), Type = InventoryTransactionType.AdjustmentIncrease, Reason = "Count correction", IncreaseLines = [new ReceiptLineInput { StockItemId = itemId, Quantity = qty, UnitCost = cost, LotNo = lot, ReceivedAt = DateTime.UtcNow }] });
    private static PostReceiptDto ReceiptInput(Guid warehouseId, Guid itemId, decimal qty, decimal cost, string lot, string key, DateTime? receivedAt = null) =>
        new() { WarehouseId = warehouseId, IdempotencyKey = key, LotNo = lot, ReceivedAt = receivedAt ?? DateTime.UtcNow, Lines = [new ReceiptLineInput { StockItemId = itemId, Quantity = qty, UnitCost = cost }] };
    private string DatePart() => _clock.Now.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
    private Task ResetInventoryLotSequenceAsync() => _cache.RemoveAsync($"Sequence:InventoryLot:{DatePart()}");
    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];
}
