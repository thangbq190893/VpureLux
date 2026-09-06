using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using VPureLux.Catalog;
using VPureLux.Service;
using VPureLux.Inventory;
using VPureLux.Warranty;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EntityFrameworkCore;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Service;

public partial class ServiceOrderWorkflowTests
{
    [Theory]
    [InlineData("enabled", true)]
    [InlineData("changed", true)]
    [InlineData("missing-baseline", true)]
    [InlineData("disabled", false)]
    [InlineData("absent", false)]
    [InlineData("inactive", false)]
    [InlineData("unmapped", false)]
    [InlineData("component-inactive", false)]
    [InlineData("rollback", false)]
    [InlineData("local-midnight", true)]
    public async Task Completion_care_respects_current_policy_position_and_unrelated_history(string scenario, bool successor)
    {
        var f = await CreateFixtureAsync("S003-CARE");
        var positionId = Guid.NewGuid();
        var untouchedId = Guid.NewGuid();
        var oldId = Guid.NewGuid();
        var untouchedReminderId = Guid.NewGuid();
        await WithUnitOfWorkAsync(async () =>
        {
            var repo = GetRequiredService<IRepository<CustomerAssetComponent, Guid>>();
            foreach (var id in new[] { positionId, untouchedId })
                await repo.InsertAsync(new CustomerAssetComponent(id, f.AssetId, id == positionId ? "CORE1" : "CORE9",
                    "Core", f.Component.Id, f.Component.Code, f.Component.Name, "Piece", 1,
                    new DateTime(2026, 6, 1), false));
            var reminders = GetRequiredService<IRepository<AssetReplacementReminder, Guid>>();
            foreach (var pair in new[] { (positionId, oldId), (untouchedId, untouchedReminderId) })
                await reminders.InsertAsync(new AssetReplacementReminder(pair.Item2, f.AssetId, pair.Item1,
                    f.Component.Id, null, null, f.Component.Code, f.Component.Name, "Piece", 1,
                    new DateTime(2026, 9, 1), 3, 7, ReplacementReminderTriggerSource.Manual,
                    "Fixture", null, Guid.NewGuid().ToString("N")));
        });
        await ReceiptAsync(f, 3, 25, 1);
        var material = Material(f.Component.Id);
        material.CustomerAssetComponentId = positionId;
        var order = await StartOrderAsync(f, material, Labor(f.Work.Id));
        await WithUnitOfWorkAsync(async () =>
        {
            var policies = GetRequiredService<IComponentReplacementPolicyRepository>();
            var existing = await policies.FindByComponentIdAsync(f.Component.Id);
            if (existing != null) await policies.DeleteAsync(existing);
            if (scenario != "absent")
                await policies.InsertAsync(new ComponentReplacementPolicy(Guid.NewGuid(), f.Component.Id,
                    scenario == "changed" ? 6 : 3, 14, null, scenario != "disabled"));
            var position = await GetRequiredService<IRepository<CustomerAssetComponent, Guid>>().GetAsync(positionId);
            if (scenario == "inactive") position.Deactivate("operator");
            if (scenario == "unmapped") position.ClearMapping("operator");
            if (scenario == "missing-baseline") position.ClearReplacementBaseline();
            if (scenario == "component-inactive")
            {
                var component = await GetRequiredService<IComponentRepository>().GetAsync(f.Component.Id);
                component.Deactivate();
            }
        });
        if (scenario == "rollback")
        {
            GetRequiredService<CompletionProbe>().After = () => throw new InvalidOperationException("after reminder replacement");
            await Should.ThrowAsync<InvalidOperationException>(() => Orders.CompleteAsync(order.Id, Completion(order)));
            (await Orders.GetAsync(order.Id)).Status.ShouldBe(ServiceOrderStatus.InProgress);
            await WithUnitOfWorkAsync(async () =>
            {
                var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
                (await db.Set<AssetReplacementReminder>().SingleAsync(x => x.Id == oldId)).Status.ShouldBe(AssetReplacementReminderStatus.Pending);
                (await db.Set<AssetReplacementReminder>().CountAsync(x => x.CustomerAssetId == f.AssetId)).ShouldBe(2);
                (await db.Set<AssetMaintenanceEvent>().CountAsync(x => x.CustomerAssetId == f.AssetId)).ShouldBe(0);
                (await db.InventoryTransactions.CountAsync(x => x.Type == InventoryTransactionType.ServiceIssue)).ShouldBe(0);
                (await db.InventoryBalances.SingleAsync(x => x.WarehouseId == f.WarehouseId)).QuantityOnHand.ShouldBe(3m);
                (await db.InventoryLots.SingleAsync(x => x.WarehouseId == f.WarehouseId)).AvailableQuantity.ShouldBe(3m);
                (await db.InventoryTransactions.SelectMany(x => x.Lines).SelectMany(x => x.Allocations).CountAsync()).ShouldBe(0);
                (await db.Set<CustomerAssetComponent>().SingleAsync(x => x.Id == positionId)).ReplacementBaselineDate.ShouldBe(new DateTime(2026, 6, 1));
            });
            return;
        }
        var completion = Completion(order);
        if (scenario == "local-midnight")
            completion.CompletedAt = new DateTimeOffset(2026, 9, 7, 1, 30, 0, TimeSpan.FromHours(7));
        var result = await Orders.CompleteAsync(order.Id, completion);
        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            var reminders = db.Set<AssetReplacementReminder>();
            var old = await reminders.SingleAsync(x => x.Id == oldId);
            old.Status.ShouldBe(AssetReplacementReminderStatus.Completed);
            old.CycleMonthsSnapshot.ShouldBe(3);
            old.WarningDaysBeforeDueSnapshot.ShouldBe(7);
            old.CompletionEventId.ShouldNotBeNull();
            var next = await reminders.SingleOrDefaultAsync(x => x.CustomerAssetComponentId == positionId && x.Status == AssetReplacementReminderStatus.Pending);
            (next != null).ShouldBe(successor);
            if (next != null)
            {
                next.CycleMonthsSnapshot.ShouldBe(scenario == "changed" ? 6 : 3);
                next.WarningDaysBeforeDueSnapshot.ShouldBe(14);
                next.DueDate.ShouldBe(new DateTime(2026, 9, 7).AddMonths(next.CycleMonthsSnapshot));
            }
            var position = await db.Set<CustomerAssetComponent>().SingleAsync(x => x.Id == positionId);
            if (scenario == "inactive") position.Status.ShouldBe(CustomerAssetComponentStatus.Inactive);
            if (scenario == "unmapped") position.ComponentId.ShouldBeNull();
            if (scenario == "missing-baseline") position.ReplacementBaselineDate.ShouldBe(result.CompletedAt.Date);
            if (scenario == "local-midnight")
            {
                result.CompletedAt.ShouldBe(new DateTime(2026, 9, 6, 18, 30, 0));
                position.ReplacementBaselineDate.ShouldBe(new DateTime(2026, 9, 7));
            }
            var untouched = await reminders.SingleAsync(x => x.Id == untouchedReminderId);
            untouched.Status.ShouldBe(AssetReplacementReminderStatus.Pending);
            untouched.LastModificationTime.ShouldBeNull();
            var events = await db.Set<AssetMaintenanceEvent>().Where(x => x.CustomerAssetId == f.AssetId).ToListAsync();
            events.Count.ShouldBe(2);
            var replacement = events.Single(x => x.EventType == AssetMaintenanceEventType.Replacement);
            replacement.SourceId.ShouldBe(order.Id);
            replacement.ServiceOrderLineId.ShouldBe(order.Lines[0].Id);
        });
    }
}
