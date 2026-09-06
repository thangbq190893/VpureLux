using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Shouldly;
using VPureLux.Inventory;
using VPureLux.Permissions;
using VPureLux.Service;
using VPureLux.Warranty;
using VPureLux.EntityFrameworkCore.Warranty;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Uow;
using Volo.Abp.DistributedLocking;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Service;

public partial class ServiceOrderWorkflowTests
{
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        // These tests must exercise real rollback; the shared legacy test module disables ALL transactions.
        services.Replace(ServiceDescriptor.Transient<IUnitOfWorkManager, UnitOfWorkManager>());
        services.AddSingleton<ServiceTestLock>();
        services.Replace(ServiceDescriptor.Singleton<IAbpDistributedLock>(provider => provider.GetRequiredService<ServiceTestLock>()));
        services.AddSingleton<CompletionProbe>();
        services.Replace(ServiceDescriptor.Transient<ICustomerCareServiceCompletion>(provider =>
            new ProbedCare(ActivatorUtilities.CreateInstance<CustomerCareServiceCompletion>(provider),
                provider.GetRequiredService<CompletionProbe>())));
    }

    private sealed class CompletionProbe
    {
        public Func<Task>? Before { get; set; }
        public Func<Task>? After { get; set; }
    }

    private sealed class ProbedCare(CustomerCareServiceCompletion inner, CompletionProbe probe) : ICustomerCareServiceCompletion
    {
        public async Task ApplyAsync(CustomerCareCompletion facts)
        {
            if (probe.Before != null) await probe.Before();
            await inner.ApplyAsync(facts);
            if (probe.After != null) await probe.After();
        }
    }

    private async Task<ServiceOrderDto> StartOrderAsync(Fixture fixture, params ServiceOrderLineInput[] lines)
    {
        var order = await Orders.CreateAsync(new CreateServiceOrderDto
        {
            CustomerAssetId = fixture.AssetId, WarehouseId = fixture.WarehouseId, Lines = lines.ToList()
        });
        order = await Orders.ConfirmAsync(order.Id, new() { ConcurrencyStamp = order.ConcurrencyStamp });
        return await Orders.StartAsync(order.Id, new() { ConcurrencyStamp = order.ConcurrencyStamp });
    }

    private static CompleteServiceOrderDto Completion(ServiceOrderDto order) => new()
    {
        ConcurrencyStamp = order.ConcurrencyStamp, IdempotencyKey = Guid.NewGuid().ToString("N"),
        CompletedAt = new DateTimeOffset(2026, 9, 7, 12, 0, 0, TimeSpan.FromHours(7)),
        Lines = order.Lines.Select(x => new CompleteServiceLineDto { LineId = x.Id, ActualQuantity = x.PlannedQuantity }).ToList()
    };

    private async Task<Guid> ReceiptAsync(Fixture fixture, int quantity, decimal cost, int day)
    {
        Guid lotId = default;
        await WithUnitOfWorkAsync(async () =>
        {
            var stock = await GetRequiredService<IStockItemRepository>().FindByCatalogItemAsync(StockItemType.Component, fixture.Component.Id);
            var manager = GetRequiredService<InventoryManager>();
            var transaction = manager.CreateTransaction(fixture.WarehouseId, InventoryTransactionType.PurchaseReceipt,
                Guid.NewGuid().ToString("N"), new string('a', 64));
            var line = transaction.AddReceiptLine(Guid.NewGuid(), stock!.Id, quantity, Unique("LOT"),
                new DateTime(2026, 9, day), cost);
            var lot = manager.CreateLot(transaction.WarehouseId, line);
            lotId = lot.Id;
            transaction.Post(new DateTime(2026, 9, day));
            await GetRequiredService<IInventoryTransactionRepository>().InsertAsync(transaction);
            await GetRequiredService<IInventoryLotRepository>().InsertAsync(lot);
            await GetRequiredService<IInventoryBalanceRepository>().ApplyMovementAsync(fixture.WarehouseId, stock.Id,
                quantity, quantity * cost, new DateTime(2026, 9, day));
        });
        return lotId;
    }

    [Theory]
    [InlineData(null)] [InlineData(0)] [InlineData(30)]
    public async Task Completion_labor_only_preserves_cost_and_replay(int? cost)
    {
        var f = await CreateFixtureAsync("S003-L");
        var work = await CreateWorkAsync(Unique("LAB"), "Labor", "Visit", 100, cost);
        var order = await StartOrderAsync(f, Labor(work.Id));
        var input = Completion(order);
        var first = await Orders.CompleteAsync(order.Id, input);
        var replay = await Orders.CompleteAsync(order.Id, input);
        first.ActualCost.ShouldBe(cost);
        first.InventoryTransactionId.ShouldBeNull();
        replay.CompletedAt.ShouldBe(first.CompletedAt);
        replay.Revenue.ShouldBe(first.Revenue);
        input.Lines[0].ActualQuantity = 0;
        (await Should.ThrowAsync<BusinessException>(() => Orders.CompleteAsync(order.Id, input)))
            .Code.ShouldBe(ServiceErrorCodes.CompletionConflict);
        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            (await db.InventoryTransactions.CountAsync(x => x.Type == InventoryTransactionType.ServiceIssue)).ShouldBe(0);
            (await db.Set<AssetMaintenanceEvent>().CountAsync(x => x.CustomerAssetId == f.AssetId)).ShouldBe(1);
        });
    }

    [Fact]
    public async Task Completion_multi_lot_repeated_component_uses_actual_FIFO_cost_and_skips_depleted()
    {
        var f = await CreateFixtureAsync("S003-FIFO");
        var depletedId = await ReceiptAsync(f, 1, 5, 1);
        var first = await StartOrderAsync(f, Material(f.Component.Id));
        await Orders.CompleteAsync(first.Id, Completion(first));
        var old = await ReceiptAsync(f, 2, 10, 2);
        var newer = await ReceiptAsync(f, 3, 20, 3);
        var order = await StartOrderAsync(f, Material(f.Component.Id, 1), Material(f.Component.Id, 3), Labor(f.Work.Id));
        var input = Completion(order);
        input.Lines[2].ActualQuantity = 0;
        var result = await Orders.CompleteAsync(order.Id, input);
        result.ActualCost.ShouldBe(60m);
        result.Revenue.ShouldBe(400m);
        await WithUnitOfWorkAsync(async () =>
        {
            var tx = await GetRequiredService<IInventoryTransactionRepository>().GetAsync(result.InventoryTransactionId!.Value);
            tx.Type.ShouldBe(InventoryTransactionType.ServiceIssue);
            tx.ReferenceId.ShouldBe(order.Id);
            tx.Lines.Sum(x => x.Allocations.Sum(a => a.Quantity)).ShouldBe(4m);
            tx.Lines.SelectMany(x => x.Allocations).ShouldNotContain(x => x.InventoryLotId == depletedId);
            tx.Lines.SelectMany(x => x.Allocations).Where(x => x.InventoryLotId == old).Sum(x => x.Quantity).ShouldBe(2m);
            (await GetRequiredService<IInventoryLotRepository>().GetAsync(newer)).AvailableQuantity.ShouldBe(1m);
        });
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Completion_shortage_and_failure_after_stock_roll_back_everything(bool injected)
    {
        var f = await CreateFixtureAsync("S003-ROLLBACK");
        var lotId = await ReceiptAsync(f, 2, 17, 1);
        var order = await StartOrderAsync(f, Material(f.Component.Id, 1), Material(f.Component.Id, 2));
        var input = Completion(order);
        if (injected)
        {
            input.Lines[1].ActualQuantity = 1;
            GetRequiredService<CompletionProbe>().After = () => throw new InvalidOperationException("S003 injected after care write");
            await Should.ThrowAsync<InvalidOperationException>(() => Orders.CompleteAsync(order.Id, input));
        }
        else (await Should.ThrowAsync<BusinessException>(() => Orders.CompleteAsync(order.Id, input)))
            .Code.ShouldBe(ServiceErrorCodes.StockShortage);
        (await Orders.GetAsync(order.Id)).Status.ShouldBe(ServiceOrderStatus.InProgress);
        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            (await db.InventoryTransactions.CountAsync(x => x.Type == InventoryTransactionType.ServiceIssue)).ShouldBe(0);
            (await db.InventoryLots.SingleAsync(x => x.Id == lotId)).AvailableQuantity.ShouldBe(2m);
            (await db.InventoryTransactions.SelectMany(x => x.Lines).SelectMany(x => x.Allocations).CountAsync()).ShouldBe(0);
            (await db.InventoryBalances.SingleAsync(x => x.WarehouseId == f.WarehouseId)).QuantityOnHand.ShouldBe(2m);
            (await db.Set<AssetMaintenanceEvent>().CountAsync(x => x.CustomerAssetId == f.AssetId)).ShouldBe(0);
        });
    }

    [Fact]
    public async Task Completion_permission_feature_and_manual_replacement_guards_are_server_side()
    {
        var f = await CreateFixtureAsync("S003-PERM");
        var order = await StartOrderAsync(f, Labor(f.Work.Id));
        var command = Completion(order);
        using (WarrantyMatrixAuthorizationService.Deny(VPureLuxPermissions.Service.Complete))
            await Should.ThrowAsync<AbpAuthorizationException>(() => Orders.CompleteAsync(order.Id, command));
        var warranty = GetRequiredService<IWarrantyAppService>();
        (await Should.ThrowAsync<BusinessException>(() => warranty.CompleteReminderAsync(Guid.NewGuid(),
            new() { IdempotencyKey = "guard", Note = "Actual replacement" }))).Code.ShouldBe(ServiceErrorCodes.ReplacementRequiresService);
        GetRequiredService<IOptions<ServiceOptions>>().Value.IsEnabled = false;
        (await Should.ThrowAsync<BusinessException>(() => Orders.CompleteAsync(order.Id, command))).Code.ShouldBe(ServiceErrorCodes.Disabled);
        GetRequiredService<IOptions<ServiceOptions>>().Value.IsEnabled = true;
        await Orders.CompleteAsync(order.Id, command);
    }
}
