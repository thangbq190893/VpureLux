using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace VPureLux.Service;

public class ServiceCompletionTests
{
    private static ServiceOrder NewOrder(decimal? laborCost = null)
    {
        var order = new ServiceOrder(Guid.NewGuid(), "SVC-TEST", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateTime.UtcNow, "C", "Customer", "A", "Machine", null, null, null, null);
        order.AddLine(Guid.NewGuid(), ServiceOrderLineType.Labor, Guid.NewGuid(), null, "L", "Labor", "Visit", 2, 100, laborCost, null);
        return order;
    }

    private static ServiceCompletionCommand Command(ServiceOrder order, int actual = 1, string key = "key") =>
        new(order.Id, key, new DateTimeOffset(2026, 9, 7, 12, 30, 0, TimeSpan.FromHours(7)),
            order.Lines.Select(x => new ServiceCompletionLine(x.Id, actual)).ToList());

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(3)]
    public void Only_in_progress_can_complete(int transitions)
    {
        var order = NewOrder();
        if (transitions > 0) order.Confirm(DateTime.UtcNow);
        if (transitions == 3) order.Cancel(DateTime.UtcNow, "cancel");
        Should.Throw<BusinessException>(() => order.ValidateCompletion(Command(order)))
            .Code.ShouldBe(ServiceErrorCodes.OrderCannotBeModified);
    }

    [Theory]
    [InlineData(null)] [InlineData(0)] [InlineData(45)]
    public void Labor_cost_is_snapshot_and_unknown_is_not_zero(int? cost)
    {
        var order = NewOrder(cost);
        order.Confirm(DateTime.UtcNow);
        order.Start(DateTime.UtcNow);
        order.Complete(Command(order), null, new Dictionary<Guid, (decimal, Guid)>());
        order.Lines.Single().ActualCostAmount.ShouldBe(cost);
        order.ActualCostAmount.ShouldBe(cost);
        order.TotalRevenueAmount.ShouldBe(100m);
        order.InventoryTransactionId.ShouldBeNull();
        Should.Throw<BusinessException>(() => order.UpdateLine(order.Lines.Single().Id, null, 1, 1, null));
        Should.Throw<BusinessException>(() => order.Complete(Command(order), null, new Dictionary<Guid, (decimal, Guid)>()));
    }

    [Fact]
    public void Zero_is_unperformed_with_no_fake_cost_fact()
    {
        var order = NewOrder(20);
        order.Confirm(DateTime.UtcNow); order.Start(DateTime.UtcNow);
        order.Complete(Command(order, 0), null, new Dictionary<Guid, (decimal, Guid)>());
        order.Lines.Single().ActualCostAmount.ShouldBeNull();
        order.TotalRevenueAmount.ShouldBe(0);
        order.Lines.Single().PlannedQuantity.ShouldBe(2);
    }

    [Fact]
    public void Canonical_hash_is_culture_order_and_offset_independent_but_payload_sensitive()
    {
        var order = NewOrder();
        order.AddLine(Guid.NewGuid(), ServiceOrderLineType.Labor, Guid.NewGuid(), null, "L2", "Labor", "Visit", 2, 100, null, null);
        var command = Command(order);
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("vi-VN");
            var same = new ServiceCompletionCommand(order.Id, command.Key,
                new DateTimeOffset(command.CompletedAt), command.Lines.Reverse().ToList());
            same.Hash.ShouldBe(command.Hash);
            order.Confirm(DateTime.UtcNow); order.Start(DateTime.UtcNow);
            order.Complete(command, null, new Dictionary<Guid, (decimal, Guid)>());
            order.IsCompletionReplay(same).ShouldBeTrue();
            Should.Throw<BusinessException>(() => order.IsCompletionReplay(Command(order, 2))).Code.ShouldBe(ServiceErrorCodes.CompletionConflict);
            order.IsCompletionReplay(Command(order, 1, "other")).ShouldBeFalse();
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public void Duplicate_foreign_missing_negative_and_excessive_actual_lines_reject()
    {
        var order = NewOrder();
        order.Confirm(DateTime.UtcNow); order.Start(DateTime.UtcNow);
        var line = order.Lines.Single();
        Should.Throw<BusinessException>(() => new ServiceCompletionCommand(order.Id, "key", DateTimeOffset.UtcNow,
            [new(line.Id, 1), new(line.Id, 1)]));
        Should.Throw<BusinessException>(() => Command(order, -1));
        Should.Throw<BusinessException>(() => order.ValidateCompletion(Command(order, 3)));
        Should.Throw<BusinessException>(() => order.ValidateCompletion(new ServiceCompletionCommand(order.Id, "key", DateTimeOffset.UtcNow,
            [new(Guid.NewGuid(), 1)])));
        typeof(ServiceCompletionLine).GetProperty(nameof(ServiceCompletionLine.ActualQuantity))!.PropertyType.ShouldBe(typeof(int));
    }
}
