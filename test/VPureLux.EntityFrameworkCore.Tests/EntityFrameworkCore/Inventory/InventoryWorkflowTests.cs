using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using VPureLux.Catalog;
using VPureLux.Catalog.Components;
using VPureLux.Inventory;
using Volo.Abp;
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

    public InventoryWorkflowTests()
    {
        _components = GetRequiredService<IComponentAppService>();
        _stockItems = GetRequiredService<IStockItemRepository>();
        _componentRepository = GetRequiredService<IComponentRepository>();
        _warehouses = GetRequiredService<IWarehouseAppService>();
        _transactions = GetRequiredService<IInventoryTransactionAppService>();
        _queries = GetRequiredService<IInventoryQueryAppService>();
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
        new() { WarehouseId = warehouseId, IdempotencyKey = key, Lines = [new ReceiptLineInput { StockItemId = itemId, Quantity = qty, UnitCost = cost, LotNo = lot, ReceivedAt = receivedAt ?? DateTime.UtcNow }] };
    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];
}
