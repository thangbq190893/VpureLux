using System;
using System.Linq;
using Shouldly;
using VPureLux.Sales.Events;
using Volo.Abp;
using Xunit;

namespace VPureLux.Sales;

public class SalesDomainTests
{
    [Fact]
    public void Should_Create_Draft_Edit_And_Renumber_Lines()
    {
        var order = Order();
        var first = AddProductLine(order, quantity: 2, actualSellingPrice: 100);
        var second = AddProductLine(order, quantity: 1, actualSellingPrice: 200);
        order.UpdateLine(first.Id, 3, 110, null);
        order.RemoveLine(first.Id);
        second.LineNo.ShouldBe(1);
        second.Quantity.ShouldBe(1);
        order.GetLocalEvents().Select(x => x.EventData).ShouldContain(x => x is SalesOrderCreatedEvent);
    }

    [Fact]
    public void Should_Require_Override_Reason_Only_When_Price_Differs()
    {
        var order = Order();
        Should.Throw<BusinessException>(() =>
            order.AddLine(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(), 100, 90, null))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.SalesOverrideReasonRequired);
        order.AddLine(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(), 100, 90, "Negotiated")
            .OverrideReason.ShouldBe("Negotiated");
    }

    [Fact]
    public void Should_Calculate_Profit_And_Margin_From_Snapshots()
    {
        var order = Order();
        var line = AddProductLine(order, actualSellingPrice: 1_000_000);
        order.ApplyCustomerSnapshot("C1", "Customer", Guid.NewGuid(), "G1", "Group");
        order.ApplyLineConfirmationSnapshot(line.Id, "I1", "Item", "Piece", null, Guid.NewGuid(), 650_000);
        order.Confirm("key", DateTime.UtcNow);
        line.RevenueAmount.ShouldBe(1_000_000);
        line.CostAmountSnapshot.ShouldBe(650_000);
        line.ProfitAmount.ShouldBe(350_000);
        line.MarginPercent.ShouldBe(35);
        order.TotalProfitAmount.ShouldBe(350_000);
    }

    [Fact]
    public void Should_Enforce_State_Transitions_And_Snapshot_Immutability()
    {
        var order = ConfirmedOrder();
        Should.Throw<BusinessException>(() => AddProductLine(order))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.SalesOrderAlreadyConfirmed);
        Should.Throw<BusinessException>(() => order.CancelDraft(DateTime.UtcNow))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.SalesOrderAlreadyConfirmed);
        order.GetLocalEvents().Select(x => x.EventData).ShouldContain(x => x is SalesOrderConfirmedEvent);
    }

    [Fact]
    public void Should_Cancel_Draft_And_Reject_Further_Editing()
    {
        var order = Order();
        order.CancelDraft(DateTime.UtcNow);
        order.Status.ShouldBe(SalesOrderStatus.Cancelled);
        Should.Throw<BusinessException>(() => AddProductLine(order))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.SalesOrderAlreadyCancelled);
        order.GetLocalEvents().Select(x => x.EventData).ShouldContain(x => x is SalesOrderCancelledEvent);
    }

    [Fact]
    public void Should_Replay_Same_Confirmation_Key_And_Reject_Different_Key()
    {
        var order = ConfirmedOrder("same-key");
        order.Confirm("same-key", DateTime.UtcNow);
        Should.Throw<BusinessException>(() => order.Confirm("different-key", DateTime.UtcNow))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.DuplicateConfirmationKey);
    }

    [Fact]
    public void Should_Reject_Invalid_Line_Invariants()
    {
        var order = Order();
        Should.Throw<BusinessException>(() => order.AddLine(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0, null, null, 1, null));
        Should.Throw<BusinessException>(() => order.AddLine(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), 1, null, null, 1, null));
        Should.Throw<BusinessException>(() => order.AddLine(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, 1, null, null, 1, null));
    }

    private static SalesOrder ConfirmedOrder(string key = "key")
    {
        var order = Order();
        var line = AddProductLine(order, actualSellingPrice: 100);
        order.ApplyCustomerSnapshot("C1", "Customer", Guid.NewGuid(), "G1", "Group");
        order.ApplyLineConfirmationSnapshot(line.Id, "I1", "Item", "Piece", null, Guid.NewGuid(), 60);
        order.Confirm(key, DateTime.UtcNow);
        return order;
    }

    private static SalesOrder Order() =>
        new(Guid.NewGuid(), "SO-202606-000001", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

    private static SalesOrderLine AddProductLine(
        SalesOrder order,
        decimal quantity = 1,
        decimal actualSellingPrice = 1) =>
        order.AddLine(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), quantity, null, null, actualSellingPrice, null);
}
