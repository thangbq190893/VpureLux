using System;
using VPureLux.Sales;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Service;

// Each row is an actual cash return. There is deliberately no edit/void/delete operation.
public class ServiceRefund : CreationAuditedAggregateRoot<Guid>
{
    public Guid ServiceOrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime RefundDate { get; private set; }
    public SalesPaymentMethod Method { get; private set; }
    public string ReferenceNo { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string RequestHash { get; private set; } = string.Empty;
    public Guid RecordedBy { get; private set; }
    public DateTime RecordedAt { get; private set; }
    protected ServiceRefund() { }

    public ServiceRefund(Guid id, Guid customerId, ServiceMoneyCommand command, Guid actor, DateTime at) : base(id)
    {
        if (customerId == Guid.Empty || actor == Guid.Empty || at == default || !command.IsRefund || string.IsNullOrWhiteSpace(command.Note))
            throw new BusinessException(ServiceErrorCodes.InvalidMoney);
        ServiceOrderId = command.OrderId;
        CustomerId = customerId;
        Amount = command.Amount;
        RefundDate = command.OccurredAt;
        Method = command.Method;
        ReferenceNo = command.Reference;
        Reason = command.Note;
        IdempotencyKey = command.Key;
        RequestHash = command.Hash;
        RecordedBy = actor;
        RecordedAt = at;
    }

    public void EnsureReplay(ServiceMoneyCommand command)
    {
        if (ServiceOrderId != command.OrderId || IdempotencyKey != command.Key || RequestHash != command.Hash)
            throw new BusinessException(ServiceErrorCodes.MoneyConflict).WithData("RefundId", Id);
    }
}
