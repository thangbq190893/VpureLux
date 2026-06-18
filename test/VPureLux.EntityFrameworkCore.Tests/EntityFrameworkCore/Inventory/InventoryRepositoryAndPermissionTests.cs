using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Shouldly;
using VPureLux.Catalog.Components;
using VPureLux.Inventory;
using VPureLux.Permissions;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.EntityFrameworkCore;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Inventory;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class InventoryRepositoryAndPermissionTests : VPureLuxEntityFrameworkCoreTestBase
{
    [Fact]
    public async Task Should_Define_All_Inventory_Permissions()
    {
        var manager = GetRequiredService<IPermissionDefinitionManager>();
        foreach (var permission in new[]
                 {
                     VPureLuxPermissions.Inventory.View, VPureLuxPermissions.Inventory.Receive,
                     VPureLuxPermissions.Inventory.Issue, VPureLuxPermissions.Inventory.Adjust,
                     VPureLuxPermissions.Inventory.ManageWarehouses, VPureLuxPermissions.Inventory.ViewLedger
                 })
        {
            (await manager.GetAsync(permission)).ShouldNotBeNull();
        }
    }

    [Fact]
    public void Should_Protect_Inventory_Application_Operations()
    {
        Permission(typeof(InventoryTransactionAppService)).ShouldBe(VPureLuxPermissions.Inventory.View);
        Permission(nameof(InventoryTransactionAppService.PostReceiptAsync)).ShouldBe(VPureLuxPermissions.Inventory.Receive);
        Permission(nameof(InventoryTransactionAppService.PostIssueAsync)).ShouldBe(VPureLuxPermissions.Inventory.Issue);
        Permission(nameof(InventoryTransactionAppService.PostAdjustmentAsync)).ShouldBe(VPureLuxPermissions.Inventory.Adjust);
        Permission(typeof(InventoryQueryAppService).GetMethod(nameof(InventoryQueryAppService.GetLedgerAsync))!).ShouldBe(VPureLuxPermissions.Inventory.ViewLedger);
    }

    [Fact]
    public async Task Should_Have_Required_Indexes_Precision_Checks_And_Concurrency_Tokens()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            UniqueIndex<StockItem>(db, "UX_StockItems_ItemType_CatalogItemId");
            UniqueIndex<Warehouse>(db, "UX_Warehouses_Code");
            UniqueIndex<InventoryLot>(db, "UX_InventoryLots_WarehouseId_StockItemId_LotNo");
            UniqueIndex<InventoryTransaction>(db, "UX_InventoryTransactions_IdempotencyKey");
            UniqueIndex<InventoryBalance>(db, "UX_InventoryBalances_WarehouseId_StockItemId");

            var lot = db.Model.FindEntityType(typeof(InventoryLot))!;
            lot.FindProperty(nameof(InventoryLot.AvailableQuantity))!.GetPrecision().ShouldBe(18);
            lot.FindProperty(nameof(InventoryLot.AvailableQuantity))!.GetScale().ShouldBe(4);
            lot.FindProperty(nameof(InventoryLot.UnitCost))!.GetScale().ShouldBe(2);
            lot.FindProperty(nameof(InventoryLot.RowVersion))!.IsConcurrencyToken.ShouldBeTrue();
            db.Model.FindEntityType(typeof(InventoryBalance))!.FindProperty(nameof(InventoryBalance.RowVersion))!.IsConcurrencyToken.ShouldBeTrue();
            db.Model.FindEntityType(typeof(InventoryTransaction))!.FindProperty(nameof(InventoryTransaction.Reason))!.GetMaxLength().ShouldBe(500);
        });
    }

    [Fact]
    public async Task Should_Enforce_Unique_Warehouse_Code_And_Foreign_Key()
    {
        var warehouseService = GetRequiredService<IWarehouseAppService>();
        var code = Unique("WH");
        await warehouseService.CreateAsync(new CreateWarehouseDto { Code = code, Name = "First" });
        await Should.ThrowAsync<Exception>(() => warehouseService.CreateAsync(new CreateWarehouseDto { Code = code, Name = "Second" }));

        await Should.ThrowAsync<DbUpdateException>(() => WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            db.InventoryBalances.Add(new InventoryBalance(Guid.NewGuid(), Guid.NewGuid()));
            await db.SaveChangesAsync();
        }));
    }

    [Fact]
    public async Task Should_Enforce_Database_Idempotency_Unique_Index()
    {
        var components = GetRequiredService<IComponentAppService>();
        var itemRepository = GetRequiredService<IStockItemRepository>();
        var warehouse = await GetRequiredService<IWarehouseAppService>().CreateAsync(new CreateWarehouseDto { Code = Unique("WH"), Name = "Warehouse" });
        var component = await components.CreateAsync(new CreateComponentDto { Code = Unique("C"), Name = "Component", Unit = "Piece" });
        var item = (await itemRepository.FindByCatalogItemAsync(StockItemType.Component, component.Id))!;
        var service = GetRequiredService<IInventoryTransactionAppService>();
        var key = Guid.NewGuid().ToString("N");
        await service.PostReceiptAsync(Input(warehouse.Id, item.Id, key, "LOT-A"));

        var exception = await Should.ThrowAsync<Volo.Abp.BusinessException>(() =>
            service.PostReceiptAsync(Input(warehouse.Id, item.Id, key, "LOT-B")));
        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.InventoryIdempotencyConflict);
    }

    private static PostReceiptDto Input(Guid warehouseId, Guid itemId, string key, string lot) =>
        new() { WarehouseId = warehouseId, IdempotencyKey = key, Lines = [new ReceiptLineInput { StockItemId = itemId, Quantity = 1, LotNo = lot, UnitCost = 100, ReceivedAt = DateTime.UtcNow }] };
    private static void UniqueIndex<TEntity>(VPureLuxDbContext db, string name) where TEntity : class =>
        db.Model.FindEntityType(typeof(TEntity))!.GetIndexes().Single(x => x.GetDatabaseName() == name).IsUnique.ShouldBeTrue();
    private static string? Permission(MemberInfo member) => member.GetCustomAttribute<AuthorizeAttribute>()?.Policy;
    private static string? Permission(string method) => Permission(typeof(InventoryTransactionAppService).GetMethod(method)!);
    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];
}
