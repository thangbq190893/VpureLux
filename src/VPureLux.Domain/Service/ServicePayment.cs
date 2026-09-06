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

    // Persistence-only compatibility shell. Operational commands belong to later Service phases.
    protected ServicePayment() { }
}
