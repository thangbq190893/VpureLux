using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using VPureLux.Inventory;
using VPureLux.Service;
using VPureLux.Warranty;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Uow;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Service;

internal sealed class ServiceTestLock : IAbpDistributedLock
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    public Action<string>? Waiting { get; set; }
    public async Task<IAbpDistributedLockHandle?> TryAcquireAsync(string name, TimeSpan timeout = default,
        CancellationToken cancellationToken = default)
    {
        var gate = _locks.GetOrAdd(name, _ => new SemaphoreSlim(1, 1));
        if (gate.CurrentCount == 0) Waiting?.Invoke(name);
        return await gate.WaitAsync(timeout, cancellationToken) ? new Handle(gate) : null;
    }
    private sealed class Handle(SemaphoreSlim gate) : IAbpDistributedLockHandle
    {
        public void Dispose() => gate.Release();
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }
}

public partial class ServiceOrderWorkflowTests
{
    [Theory]
    [InlineData(1)] [InlineData(2)]
    public async Task Completion_two_services_same_position_serialize_stock_and_successor(int stockQuantity)
    {
        var f = await CreateFixtureAsync("S003-ASSET-RACE");
        var positionId = Guid.NewGuid();
        await WithUnitOfWorkAsync(async () =>
        {
            await GetRequiredService<IRepository<CustomerAssetComponent, Guid>>().InsertAsync(new CustomerAssetComponent(
                positionId, f.AssetId, "CORE1", "Core 1", f.Component.Id, f.Component.Code, f.Component.Name,
                "Piece", 1, new DateTime(2026, 6, 1), false));
            await GetRequiredService<IComponentReplacementPolicyRepository>().InsertAsync(new ComponentReplacementPolicy(
                Guid.NewGuid(), f.Component.Id, 3, 7, null));
        });
        await ReceiptAsync(f, stockQuantity, 20, 1);
        var material = Material(f.Component.Id);
        material.CustomerAssetComponentId = positionId;
        var a = await StartOrderAsync(f, material);
        var b = await StartOrderAsync(f, material);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        GetRequiredService<CompletionProbe>().Before = async () =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                entered.TrySetResult();
                await release.Task.WaitAsync(TimeSpan.FromSeconds(15));
            }
        };
        GetRequiredService<ServiceTestLock>().Waiting = key =>
        {
            if (key == $"VPureLux:CustomerAsset:{f.AssetId:N}") waiting.TrySetResult();
        };
        var first = Task.Run(() => Orders.CompleteAsync(a.Id, Completion(a)));
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
        var second = Task.Run(() => Orders.CompleteAsync(b.Id, Completion(b)));
        try { await waiting.Task.WaitAsync(TimeSpan.FromSeconds(15)); }
        finally { release.TrySetResult(); }
        await first;
        if (stockQuantity == 1)
            (await Should.ThrowAsync<BusinessException>(() => second)).Code.ShouldBe(ServiceErrorCodes.StockShortage);
        else await second;
        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            (await db.Set<AssetReplacementReminder>().CountAsync(x => x.CustomerAssetComponentId == positionId &&
                x.Status == AssetReplacementReminderStatus.Pending)).ShouldBe(1);
            (await db.InventoryLots.Where(x => x.WarehouseId == f.WarehouseId).SumAsync(x => (double)x.AvailableQuantity)).ShouldBe(0);
            (await db.InventoryTransactions.CountAsync(x => x.WarehouseId == f.WarehouseId &&
                x.Type == InventoryTransactionType.ServiceIssue)).ShouldBe(stockQuantity);
        });
    }

    [Theory]
    [InlineData("same-key")] [InlineData("different-key")] [InlineData("cancel")]
    public async Task Completion_race_order_lock_holds_until_transaction_finishes(string competitor)
    {
        var f = await CreateFixtureAsync("S003-RACE");
        var order = await StartOrderAsync(f, Labor(f.Work.Id));
        var firstInput = Completion(order);
        var secondInput = Completion(order);
        if (competitor == "same-key") secondInput.IdempotencyKey = firstInput.IdempotencyKey;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        GetRequiredService<CompletionProbe>().Before = async () => { entered.TrySetResult(); await release.Task.WaitAsync(TimeSpan.FromSeconds(15)); };
        GetRequiredService<ServiceTestLock>().Waiting = key =>
        {
            if (key == $"VPureLux:ServiceOrder:{order.Id:N}") waiting.TrySetResult();
        };
        var first = Task.Run(() => Orders.CompleteAsync(order.Id, firstInput));
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
        var second = Task.Run(async () =>
        {
            if (competitor == "cancel")
                await Orders.CancelAsync(order.Id, new() { ConcurrencyStamp = order.ConcurrencyStamp, Reason = "cancel race" });
            else await Orders.CompleteAsync(order.Id, secondInput);
        });
        try
        {
            await waiting.Task.WaitAsync(TimeSpan.FromSeconds(15));
            second.IsCompleted.ShouldBeFalse();
        }
        finally { release.TrySetResult(); }
        await first;
        if (competitor == "same-key") await second;
        else await Should.ThrowAsync<BusinessException>(() => second);
        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            (await db.Set<AssetMaintenanceEvent>().CountAsync(x => x.CustomerAssetId == f.AssetId)).ShouldBe(1);
        });
    }

    [Fact]
    public async Task Completion_vs_shared_inventory_stale_lot_writer_cannot_double_allocate()
    {
        var f = await CreateFixtureAsync("S003-STOCK");
        var lotId = await ReceiptAsync(f, 1, 10, 1);
        var order = await StartOrderAsync(f, Material(f.Component.Id));
        var uowManager = GetRequiredService<IUnitOfWorkManager>();
        using (var stale = uowManager.Begin(requiresNew: true, isTransactional: false))
        {
            var repo = GetRequiredService<IInventoryLotRepository>();
            var lot = await repo.GetAsync(lotId);
            await Orders.CompleteAsync(order.Id, Completion(order));
            lot.Allocate(1);
            await Should.ThrowAsync<AbpDbConcurrencyException>(() => repo.UpdateAsync(lot, autoSave: true));
        }
        await WithUnitOfWorkAsync(async () =>
            (await GetRequiredService<IInventoryLotRepository>().GetAsync(lotId)).AvailableQuantity.ShouldBe(0m));
    }

    [Fact]
    public async Task Completion_all_material_zero_writes_machine_fact_without_inventory()
    {
        var f = await CreateFixtureAsync("S003-ZERO");
        var order = await StartOrderAsync(f, Material(f.Component.Id));
        var command = Completion(order);
        command.Lines[0].ActualQuantity = 0;
        var result = await Orders.CompleteAsync(order.Id, command);
        result.InventoryTransactionId.ShouldBeNull();
        result.Revenue.ShouldBe(0);
        (await Orders.GetAsync(order.Id)).Lines.Single().ActualCostAmount.ShouldBeNull();
    }
}
