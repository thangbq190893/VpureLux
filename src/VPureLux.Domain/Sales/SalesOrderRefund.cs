using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using VPureLux.Sales.Events;

namespace VPureLux.Sales;

public class SalesOrderRefund : FullAuditedAggregateRoot<Guid>
{
    public Guid SalesOrderId { get; private set; }
    public Guid? SalesOrderRevisionId { get; private set; }
    public Guid? SalesOrderCancellationId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime RefundedAt { get; private set; }
    public SalesPaymentMethod PaymentMethod { get; private set; }
    public string ReferenceNo { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;

    protected SalesOrderRefund() { }

    public SalesOrderRefund(
        Guid id,
        Guid salesOrderId,
        Guid? revisionId,
        Guid? cancellationId,
        decimal amount,
        DateTime refundedAt,
        SalesPaymentMethod paymentMethod,
        string? referenceNo,
        string reason,
        string idempotencyKey) : base(id)
    {
        if (salesOrderId == Guid.Empty || amount <= 0 || (revisionId.HasValue == cancellationId.HasValue))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }
        SalesOrderId = salesOrderId;
        SalesOrderRevisionId = revisionId;
        SalesOrderCancellationId = cancellationId;
        Amount = SalesOrderRevision.RoundMoney(amount);
        RefundedAt = refundedAt;
        PaymentMethod = paymentMethod;
        ReferenceNo = Check.Length(referenceNo?.Trim() ?? string.Empty, nameof(referenceNo), SalesConsts.MaxPaymentReferenceNoLength) ?? string.Empty;
        Reason = SalesOrderRevision.NormalizeReason(reason);
        IdempotencyKey = Check.NotNullOrWhiteSpace(idempotencyKey, nameof(idempotencyKey), SalesConsts.MaxIdempotencyKeyLength);
        AddLocalEvent(new SalesOrderRefundRecordedEvent(
            Id, SalesOrderId, SalesOrderRevisionId, SalesOrderCancellationId, Amount, Reason));
    }
}
