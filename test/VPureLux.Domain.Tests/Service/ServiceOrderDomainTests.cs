using System;
using System.Linq;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace VPureLux.Service;

public class ServiceOrderDomainTests
{
    [Fact]
    public void Should_require_confirm_then_start_and_forbid_individual_line_completion()
    {
        var order = CreateOrder();
        order.AddLine(Guid.NewGuid(), ServiceOrderLineType.Labor, Guid.NewGuid(), null,
            "WORK-01", "Inspection", "Visit", 1, 100, null, null);

        order.Confirm(DateTime.UtcNow);
        order.Status.ShouldBe(ServiceOrderStatus.Confirmed);
        order.Start(DateTime.UtcNow);
        order.Status.ShouldBe(ServiceOrderStatus.InProgress);
        typeof(ServiceOrder).GetMethod("Complete").ShouldNotBeNull();
        typeof(ServiceOrder).GetMethod("CompleteLine").ShouldBeNull();
        Should.Throw<BusinessException>(() => order.Confirm(DateTime.UtcNow))
            .Code.ShouldBe(ServiceErrorCodes.OrderCannotBeModified);
    }

    [Fact]
    public void Should_keep_line_identity_and_snapshot_when_only_mutable_fields_change()
    {
        var order = CreateOrder();
        var line = order.AddLine(Guid.NewGuid(), ServiceOrderLineType.Labor, Guid.NewGuid(), null,
            "WORK-A", "Original labor", "Visit", 1, 100.125m, null, "first");

        var lineId = line.Id;

        order.UpdateLine(lineId, null, 2, 120.555m, "changed");

        line.Id.ShouldBe(lineId);
        line.ItemCodeSnapshot.ShouldBe("WORK-A");
        line.ItemNameSnapshot.ShouldBe("Original labor");
        line.UnitSnapshot.ShouldBe("Visit");
        line.PlannedQuantity.ShouldBe(2);
        line.UnitPrice.ShouldBe(120.56m);
        line.StandardCostSnapshot.ShouldBeNull();
    }

    [Fact]
    public void Should_enforce_material_labor_exclusivity_and_nullable_cost_semantics()
    {
        var order = CreateOrder();
        var componentId = Guid.NewGuid();
        var positionId = Guid.NewGuid();
        var material = order.AddLine(Guid.NewGuid(), ServiceOrderLineType.Material, componentId, positionId,
            "MAT-01", "Core", "Piece", 1, 10, 99, null);
        var laborUnknown = order.AddLine(Guid.NewGuid(), ServiceOrderLineType.Labor, Guid.NewGuid(), positionId,
            "WORK-01", "Labor", "Visit", 1, 20, null, null);
        var laborZero = order.AddLine(Guid.NewGuid(), ServiceOrderLineType.Labor, Guid.NewGuid(), null,
            "WORK-02", "Free labor", "Visit", 1, 0, 0, null);

        material.ComponentId.ShouldBe(componentId);
        material.ServiceWorkId.ShouldBeNull();
        material.StandardCostSnapshot.ShouldBeNull();
        laborUnknown.ComponentId.ShouldBeNull();
        laborUnknown.CustomerAssetComponentId.ShouldBeNull();
        laborUnknown.StandardCostSnapshot.ShouldBeNull();
        laborZero.StandardCostSnapshot.ShouldBe(0);
    }

    [Fact]
    public void Cancel_should_require_reason_and_make_order_terminal()
    {
        var order = CreateOrder();
        order.AddLine(Guid.NewGuid(), ServiceOrderLineType.Labor, Guid.NewGuid(), null,
            "WORK-01", "Labor", "Visit", 1, 20, null, null);

        Should.Throw<BusinessException>(() => order.Cancel(DateTime.UtcNow, " "))
            .Code.ShouldBe(ServiceErrorCodes.CancellationReasonRequired);
        order.Cancel(DateTime.UtcNow, "Customer changed schedule");
        order.Status.ShouldBe(ServiceOrderStatus.Cancelled);
        order.CancellationReason.ShouldBe("Customer changed schedule");
        Should.Throw<BusinessException>(() => order.UpdateDraft(DateTime.UtcNow, null, null, null, null))
            .Code.ShouldBe(ServiceErrorCodes.OrderCannotBeModified);
        Should.Throw<BusinessException>(() => order.Cancel(DateTime.UtcNow, "again"))
            .Code.ShouldBe(ServiceErrorCodes.OrderCannotBeModified);
    }

    [Fact]
    public void Confirm_should_require_a_line_and_line_quantities_must_be_positive_integers()
    {
        var order = CreateOrder();
        Should.Throw<BusinessException>(() => order.Confirm(DateTime.UtcNow))
            .Code.ShouldBe(ServiceErrorCodes.OrderCannotBeModified);
        Should.Throw<BusinessException>(() => order.AddLine(Guid.NewGuid(), ServiceOrderLineType.Material,
            Guid.NewGuid(), null, "MAT", "Core", "Piece", 0, 1, null, null))
            .Code.ShouldBe(ServiceErrorCodes.InvalidLine);
        typeof(ServiceOrderLine).GetProperty(nameof(ServiceOrderLine.PlannedQuantity))!.PropertyType.ShouldBe(typeof(int));
    }

    [Fact]
    public void Removing_one_line_should_not_recreate_unrelated_lines()
    {
        var order = CreateOrder();
        var first = order.AddLine(Guid.NewGuid(), ServiceOrderLineType.Labor, Guid.NewGuid(), null,
            "W1", "One", "Visit", 1, 1, null, null);
        var second = order.AddLine(Guid.NewGuid(), ServiceOrderLineType.Labor, Guid.NewGuid(), null,
            "W2", "Two", "Visit", 1, 1, null, null);

        order.RemoveLine(first.Id);

        order.Lines.ShouldHaveSingleItem().Id.ShouldBe(second.Id);
        order.Lines.Single().LineNo.ShouldBe(1);
    }

    private static ServiceOrder CreateOrder() => new(
        Guid.NewGuid(), "SVC-202609070001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        new DateTime(2026, 9, 7), "CUS-01", "Customer", "ASSET-01", "Machine",
        null, null, null, null);
}
