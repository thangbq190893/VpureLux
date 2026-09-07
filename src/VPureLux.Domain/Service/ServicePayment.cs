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
    public string? RequestHash { get; private set; }
    public DateTime? RecordedAt { get; private set; }
    public Guid? RecordedBy { get; private set; }
    public DateTime? VoidedAt { get; private set; }
    public Guid? VoidedBy { get; private set; }
    public string? VoidReason { get; private set; }
    public string? VoidIdempotencyKey { get; private set; }
    public string? VoidRequestHash { get; private set; }

    protected ServicePayment() { }

    public ServicePayment(Guid id, Guid customerId, ServiceMoneyCommand command, Guid actor, DateTime recordedAt) : base(id)
    {
        if (customerId == Guid.Empty || actor == Guid.Empty || recordedAt == default || command.IsRefund)
            throw new BusinessException(ServiceErrorCodes.InvalidMoney);
        ServiceOrderId = command.OrderId;
        CustomerId = customerId;
        Amount = command.Amount;
        PaymentDate = command.OccurredAt;
        PaymentMethod = command.Method;
        ReferenceNo = command.Reference;
        Note = command.Note;
        IdempotencyKey = command.Key;
        RequestHash = command.Hash;
        RecordedAt = recordedAt;
        RecordedBy = actor;
        Status = ServicePaymentStatus.Posted;
    }

    public void EnsureReplay(ServiceMoneyCommand command)
    {
        // Legacy rows have no hash. Compare only their persisted raw facts; never fill a hash or reinterpret their dates.
        var sameFacts = RequestHash != null ? RequestHash == command.Hash :
            Amount == command.Amount && PaymentDate == command.OccurredAt && PaymentMethod == command.Method &&
            ReferenceNo == command.Reference && Note == command.Note && !command.IsRefund;
        if (ServiceOrderId != command.OrderId || IdempotencyKey != command.Key || !sameFacts)
            throw new BusinessException(ServiceErrorCodes.MoneyConflict).WithData("PaymentId", Id);
    }

    public bool IsVoidReplay(ServiceVoidCommand command)
    {
        if (command.PaymentId != Id) throw new BusinessException(ServiceErrorCodes.InvalidMoney);
        if (Status != ServicePaymentStatus.Voided) return false;
        if (VoidIdempotencyKey != command.Key || VoidRequestHash != command.Hash)
            throw new BusinessException(ServiceErrorCodes.MoneyConflict).WithData("PaymentId", Id);
        return true;
    }

    public void Void(ServiceVoidCommand command, Guid actor, DateTime at)
    {
        if (IsVoidReplay(command)) return;
        if (actor == Guid.Empty || at == default || Status != ServicePaymentStatus.Posted)
            throw new BusinessException(ServiceErrorCodes.InvalidMoney);
        Status = ServicePaymentStatus.Voided;
        VoidedAt = at;
        VoidedBy = actor;
        VoidReason = command.Reason;
        VoidIdempotencyKey = command.Key;
        VoidRequestHash = command.Hash;
    }
}
