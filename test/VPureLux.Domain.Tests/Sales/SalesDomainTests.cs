using System;
using System.Linq;
using Shouldly;
using VPureLux.Sales.Events;
using VPureLux.Warranty;
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
        order.UpdateLine(first.Id, first.ProductId, first.BomVersionId!.Value, 3, first.SuggestedPriceVersionId, first.SuggestedPriceSnapshot, 110, null);
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
    public void Should_Cancel_Confirmed_Unpaid_Order()
    {
        var order = ConfirmedOrder();

        order.CancelConfirmedUnpaid(DateTime.UtcNow);

        order.Status.ShouldBe(SalesOrderStatus.Cancelled);
        order.CancelledAt.ShouldNotBeNull();
        order.GetLocalEvents().Select(x => x.EventData).ShouldContain(x => x is SalesOrderCancelledEvent);
        Should.Throw<BusinessException>(() => order.CancelConfirmedUnpaid(DateTime.UtcNow))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.SalesOrderAlreadyCancelled);
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

    [Fact]
    public void Payment_Summary_Should_Derive_Receivable_Status()
    {
        SalesOrderPaymentSummary.From(1_000, 0).PaymentStatus.ShouldBe(SalesOrderReceivableStatus.Unpaid);
        SalesOrderPaymentSummary.From(1_000, 250).PaymentStatus.ShouldBe(SalesOrderReceivableStatus.PartiallyPaid);
        SalesOrderPaymentSummary.From(1_000, 1_000).PaymentStatus.ShouldBe(SalesOrderReceivableStatus.Paid);
        SalesOrderPaymentSummary.From(1_000, 1_100).PaymentStatus.ShouldBe(SalesOrderReceivableStatus.Overpaid);
        SalesOrderPaymentSummary.From(1_000, 250).RemainingAmount.ShouldBe(750);
        SalesOrderPaymentSummary.From(1_000, 1_100).RemainingAmount.ShouldBe(-100);
    }

    [Fact]
    public void SalesOrderPayment_Should_Default_To_Posted_History_Row()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var payment = new SalesOrderPayment(
            Guid.NewGuid(),
            orderId,
            customerId,
            500,
            DateTime.UtcNow,
            SalesPaymentMethod.BankTransfer,
            "BANK-001",
            "First installment",
            "pay-key");

        payment.SalesOrderId.ShouldBe(orderId);
        payment.CustomerId.ShouldBe(customerId);
        payment.Amount.ShouldBe(500);
        payment.ReferenceNo.ShouldBe("BANK-001");
        payment.Note.ShouldBe("First installment");
        payment.IdempotencyKey.ShouldBe("pay-key");
        payment.Status.ShouldBe(SalesOrderPaymentStatus.Posted);
        payment.ContributesToReceivable.ShouldBeTrue();
    }

    [Fact]
    public void Revision_Should_Copy_Effective_Order_And_Require_Reason()
    {
        var order = ConfirmedOrder();
        Should.Throw<ArgumentException>(() => new SalesOrderRevision(
            Guid.NewGuid(), order.Id, 1, "", order.CustomerId, order.TotalRevenueAmount, order.EffectiveLines));

        var revision = Revision(order);
        revision.Status.ShouldBe(SalesOrderRevisionStatus.Draft);
        revision.Lines.Single().SourceSalesOrderLineId.ShouldBe(order.EffectiveLines.Single().Id);
        revision.CustomerIdSnapshot.ShouldBe(order.CustomerId);
    }

    [Fact]
    public void Revision_Decrease_And_Remove_Should_Require_Return_Confirmation()
    {
        var order = ConfirmedOrder();
        var revision = Revision(order);
        var line = revision.Lines.Single();
        revision.UpdateLine(line.Id, line.ProductId, line.BomVersionId, 0.5m,
            line.SuggestedPriceVersionId, line.SuggestedPriceSnapshot, line.ActualSellingPrice, line.OverrideReason);
        line.RequiresReturnConfirmation.ShouldBeTrue();
        revision.ConfirmReturnedGoods(line.Id, Guid.NewGuid(), DateTime.UtcNow, "Received");
        line.ReturnConfirmedAt.ShouldNotBeNull();
        revision.RemoveLine(line.Id);
        line.IsRemoved.ShouldBeTrue();
        line.RequiresReturnConfirmation.ShouldBeTrue();
    }

    [Fact]
    public void Revision_Apply_Should_Calculate_Refund_And_Be_Idempotent()
    {
        var revision = Revision(ConfirmedOrder());
        var appliedAt = DateTime.UtcNow;
        revision.Apply("apply-key", Guid.NewGuid(), appliedAt, 80, 100);
        revision.Status.ShouldBe(SalesOrderRevisionStatus.Applied);
        revision.RefundDue.ShouldBe(20);
        revision.Apply("apply-key", Guid.NewGuid(), appliedAt.AddMinutes(1), 999, 0);
        revision.AppliedTotal.ShouldBe(80);
        Should.Throw<BusinessException>(() => revision.Apply("other-key", null, appliedAt, 80, 100))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.SalesRevisionIdempotencyConflict);
    }

    [Fact]
    public void Cancellation_Should_Close_Only_After_Stock_And_Refund_Are_Complete()
    {
        var cancellation = new SalesOrderCancellation(
            Guid.NewGuid(), Guid.NewGuid(), "Customer", "Changed mind", Guid.NewGuid(),
            DateTime.UtcNow, requiresStockReturn: true, refundDue: 100);
        cancellation.ClosedAt.ShouldBeNull();
        cancellation.CompleteStockReturn(Guid.NewGuid(), DateTime.UtcNow);
        cancellation.ClosedAt.ShouldBeNull();
        cancellation.RecordRefund(40, DateTime.UtcNow);
        cancellation.PaymentStatus.ShouldBe(SalesOrderCancellationPaymentStatus.PendingRefund);
        cancellation.RecordRefund(60, DateTime.UtcNow);
        cancellation.ClosedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Cancellation_Stock_Exception_Should_Remain_Open()
    {
        var cancellation = new SalesOrderCancellation(
            Guid.NewGuid(), Guid.NewGuid(), "Damage", "Damaged return", null,
            DateTime.UtcNow, requiresStockReturn: true, refundDue: 0);
        cancellation.MarkStockException("Needs manager adjustment");
        cancellation.StockStatus.ShouldBe(SalesOrderCancellationStockStatus.Exception);
        cancellation.ClosedAt.ShouldBeNull();
    }

    [Fact]
    public void Cancellation_Should_Reject_Refund_Above_Due()
    {
        var cancellation = new SalesOrderCancellation(
            Guid.NewGuid(), Guid.NewGuid(), "Customer", "Changed mind", null,
            DateTime.UtcNow, requiresStockReturn: false, refundDue: 100);
        Should.Throw<BusinessException>(() => cancellation.RecordRefund(101, DateTime.UtcNow))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.SalesRefundExceedsAmountDue);
    }

    [Fact]
    public void Payment_Void_Should_Preserve_Row_And_Stop_Receivable_Contribution()
    {
        var payment = new SalesOrderPayment(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100, DateTime.UtcNow,
            SalesPaymentMethod.Cash, idempotencyKey: "void-test");
        payment.Void(Guid.NewGuid(), DateTime.UtcNow, "Wrong entry, cash not received");
        payment.Status.ShouldBe(SalesOrderPaymentStatus.Voided);
        payment.ContributesToReceivable.ShouldBeFalse();
        payment.VoidReason.ShouldBe("Wrong entry, cash not received");
    }

    [Fact]
    public void Payment_Summary_Should_Expose_Refund_Due_Without_Resetting_Paid()
    {
        var summary = SalesOrderPaymentSummary.From(80, 100);
        summary.PaidAmount.ShouldBe(100);
        summary.RemainingAmount.ShouldBe(-20);
        summary.RefundDue.ShouldBe(20);
    }

    [Fact]
    public void Cancelling_Pending_Asset_Should_Keep_A_Bounded_Note()
    {
        var asset = CustomerAsset.CreateSoldMachine(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            1, 1, "ASSET-1", "SO-1", "C-1", "Customer", "P-1", "Machine", DateTime.UtcNow);

        asset.CancelBeforeInstallation(new string('x', SalesConsts.MaxReasonLength));

        asset.Status.ShouldBe(CustomerAssetStatus.Cancelled);
        asset.Note!.Length.ShouldBe(WarrantyConsts.MaxNoteLength);
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

    private static SalesOrderRevision Revision(SalesOrder order) =>
        new(Guid.NewGuid(), order.Id, 1, "Manager correction", order.CustomerId,
            order.TotalRevenueAmount, order.EffectiveLines);

    private static SalesOrder Order() =>
        new(Guid.NewGuid(), "SO-202606-000001", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

    private static SalesOrderLine AddProductLine(
        SalesOrder order,
        decimal quantity = 1,
        decimal actualSellingPrice = 1) =>
        order.AddLine(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), quantity, null, null, actualSellingPrice, null);
}
