using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using VPureLux.Sales.Events;

namespace VPureLux.Sales;

public class SalesOrderCancellation : FullAuditedAggregateRoot<Guid>
{
    public Guid SalesOrderId { get; private set; }
    public string ReasonGroup { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public Guid? ApprovedBy { get; private set; }
    public DateTime EffectiveAt { get; private set; }
    public SalesOrderCancellationStockStatus StockStatus { get; private set; }
    public SalesOrderCancellationPaymentStatus PaymentStatus { get; private set; }
    public decimal RefundDue { get; private set; }
    public decimal RefundedAmount { get; private set; }
    public Guid? StockReversalTransactionId { get; private set; }
    public string? StockExceptionReason { get; private set; }
    public DateTime? StockCompletedAt { get; private set; }
    public DateTime? PaymentCompletedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    protected SalesOrderCancellation() { }

    public SalesOrderCancellation(
        Guid id,
        Guid salesOrderId,
        string reasonGroup,
        string reason,
        Guid? approvedBy,
        DateTime effectiveAt,
        bool requiresStockReturn,
        decimal refundDue) : base(id)
    {
        SalesOrderId = Check.NotDefaultOrNull<Guid>(salesOrderId, nameof(salesOrderId));
        ReasonGroup = SalesOrderRevision.NormalizeReason(reasonGroup);
        Reason = SalesOrderRevision.NormalizeReason(reason);
        ApprovedBy = approvedBy;
        EffectiveAt = effectiveAt;
        RefundDue = SalesOrderRevision.RoundMoney(Math.Max(refundDue, 0));
        StockStatus = requiresStockReturn
            ? SalesOrderCancellationStockStatus.PendingReturn
            : SalesOrderCancellationStockStatus.NotRequired;
        PaymentStatus = RefundDue > 0
            ? SalesOrderCancellationPaymentStatus.PendingRefund
            : SalesOrderCancellationPaymentStatus.NotRequired;
        TryClose(effectiveAt);
        AddLocalEvent(new SalesOrderCancellationApprovedEvent(
            Id, SalesOrderId, ReasonGroup, Reason, RefundDue));
    }

    public void CompleteStockReturn(Guid reversalTransactionId, DateTime completedAt)
    {
        if (StockStatus is not (SalesOrderCancellationStockStatus.PendingReturn or SalesOrderCancellationStockStatus.Exception))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.SalesCancellationNotPendingReturn);
        }
        StockReversalTransactionId = Check.NotDefaultOrNull<Guid>(reversalTransactionId, nameof(reversalTransactionId));
        StockStatus = SalesOrderCancellationStockStatus.Completed;
        StockExceptionReason = null;
        StockCompletedAt = completedAt;
        TryClose(completedAt);
    }

    public void MarkStockException(string reason)
    {
        if (StockStatus != SalesOrderCancellationStockStatus.PendingReturn)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.SalesCancellationNotPendingReturn);
        }
        StockStatus = SalesOrderCancellationStockStatus.Exception;
        StockExceptionReason = SalesOrderRevision.NormalizeReason(reason);
    }

    public void RecordRefund(decimal amount, DateTime refundedAt)
    {
        amount = SalesOrderRevision.RoundMoney(amount);
        if (PaymentStatus != SalesOrderCancellationPaymentStatus.PendingRefund || amount <= 0 || RefundedAmount + amount > RefundDue)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.SalesRefundExceedsAmountDue);
        }
        RefundedAmount += amount;
        if (RefundedAmount == RefundDue)
        {
            PaymentStatus = SalesOrderCancellationPaymentStatus.Completed;
            PaymentCompletedAt = refundedAt;
            TryClose(refundedAt);
        }
    }

    private void TryClose(DateTime at)
    {
        var stockDone = StockStatus is SalesOrderCancellationStockStatus.NotRequired or SalesOrderCancellationStockStatus.Completed;
        var paymentDone = PaymentStatus is SalesOrderCancellationPaymentStatus.NotRequired or SalesOrderCancellationPaymentStatus.Completed;
        if (stockDone && paymentDone)
        {
            ClosedAt = at;
        }
    }
}
