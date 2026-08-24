using System;
using VPureLux.Sales;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Service;

public class ServicePayment : FullAuditedAggregateRoot<Guid>
{
    public Guid ServiceOrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime PaymentDate { get; private set; }
    public SalesPaymentMethod PaymentMethod { get; private set; }
    public string ReferenceNo { get; private set; } = string.Empty;
    public string? Note { get; private set; }
    public ServicePaymentStatus Status { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    protected ServicePayment()
    {
    }

    public ServicePayment(
        Guid id,
        Guid serviceOrderId,
        Guid customerId,
        decimal amount,
        DateTime paymentDate,
        SalesPaymentMethod paymentMethod,
        string idempotencyKey,
        string? referenceNo = null,
        string? note = null) : base(id)
    {
        if (amount <= 0 || !Enum.IsDefined(paymentMethod))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }
        ServiceOrderId = Check.NotDefaultOrNull<Guid>(serviceOrderId, nameof(serviceOrderId));
        CustomerId = Check.NotDefaultOrNull<Guid>(customerId, nameof(customerId));
        Amount = decimal.Round(amount, ServiceConsts.MoneyScale, MidpointRounding.AwayFromZero);
        PaymentDate = paymentDate;
        PaymentMethod = paymentMethod;
        IdempotencyKey = Check.NotNullOrWhiteSpace(idempotencyKey, nameof(idempotencyKey), ServiceConsts.MaxIdempotencyKeyLength);
        ReferenceNo = Check.Length(referenceNo?.Trim() ?? string.Empty, nameof(referenceNo), ServiceConsts.MaxReferenceNoLength) ?? string.Empty;
        Note = string.IsNullOrWhiteSpace(note) ? null : Check.Length(note.Trim(), nameof(note), ServiceConsts.MaxNoteLength);
        Status = ServicePaymentStatus.Posted;
    }

    public void Void() => Status = ServicePaymentStatus.Voided;
}
