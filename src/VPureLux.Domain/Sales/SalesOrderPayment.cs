using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Sales;

public class SalesOrderPayment : FullAuditedAggregateRoot<Guid>
{
    public Guid SalesOrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime PaymentDate { get; private set; }
    public SalesPaymentMethod PaymentMethod { get; private set; }
    public string ReferenceNo { get; private set; } = string.Empty;
    public string? Note { get; private set; }
    public SalesOrderPaymentStatus Status { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    protected SalesOrderPayment() { }

    public SalesOrderPayment(
        Guid id,
        Guid salesOrderId,
        Guid customerId,
        decimal amount,
        DateTime paymentDate,
        SalesPaymentMethod paymentMethod,
        string? referenceNo = null,
        string? note = null,
        string? idempotencyKey = null) : base(id)
    {
        SalesOrderId = Check.NotDefaultOrNull<Guid>(salesOrderId, nameof(salesOrderId));
        CustomerId = Check.NotDefaultOrNull<Guid>(customerId, nameof(customerId));
        if (amount <= 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }
        Amount = amount;
        PaymentDate = paymentDate;
        PaymentMethod = paymentMethod;
        ReferenceNo = Check.Length(referenceNo?.Trim() ?? string.Empty, nameof(referenceNo), SalesConsts.MaxPaymentReferenceNoLength);
        Note = string.IsNullOrWhiteSpace(note)
            ? null
            : Check.Length(note.Trim(), nameof(note), SalesConsts.MaxPaymentNoteLength);
        IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey)
            ? null
            : Check.Length(idempotencyKey.Trim(), nameof(idempotencyKey), SalesConsts.MaxIdempotencyKeyLength);
        Status = SalesOrderPaymentStatus.Posted;
    }

    public bool ContributesToReceivable => Status == SalesOrderPaymentStatus.Posted;
}
