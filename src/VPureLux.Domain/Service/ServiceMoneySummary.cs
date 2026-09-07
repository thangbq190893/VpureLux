using System;
using System.Linq.Expressions;
using Volo.Abp;

namespace VPureLux.Service;

public class ServiceMoneyFacts
{
    public Guid ServiceOrderId { get; set; }
    public Guid CustomerId { get; set; }
    public ServiceOrderStatus Status { get; set; }
    public decimal PlannedTotal { get; set; }
    public decimal CompletedTotal { get; set; }
    public decimal GrossPosted { get; set; }
    public decimal GrossRefunded { get; set; }
}

public class ServiceMoneySummary
{
    public Guid ServiceOrderId { get; set; }
    public Guid CustomerId { get; set; }
    public ServiceOrderStatus Status { get; set; }
    public decimal PlannedTotal { get; set; }
    public decimal? ActualTotal { get; set; }
    public decimal Revenue { get; set; }
    public decimal GrossPosted { get; set; }
    public decimal GrossRefunded { get; set; }
    public decimal NetPaid { get; set; }
    public decimal AdvancePaid { get; set; }
    public decimal PlannedRemaining { get; set; }
    public decimal AdvanceExcess { get; set; }
    public decimal Receivable { get; set; }
    public decimal CustomerCredit { get; set; }
    public decimal RefundDue { get; set; }
    public bool HasInconsistentLedger { get; set; }

    // This same expression runs in SQL and in domain tests; no second balance formula in the UI.
    public static Expression<Func<ServiceMoneyFacts, ServiceMoneySummary>> Projection => f => new ServiceMoneySummary
    {
        ServiceOrderId = f.ServiceOrderId, CustomerId = f.CustomerId, Status = f.Status,
        PlannedTotal = f.PlannedTotal,
        ActualTotal = f.Status == ServiceOrderStatus.Completed ? f.CompletedTotal : null,
        Revenue = f.Status == ServiceOrderStatus.Completed ? f.CompletedTotal : 0,
        GrossPosted = f.GrossPosted, GrossRefunded = f.GrossRefunded,
        NetPaid = f.GrossPosted > f.GrossRefunded ? f.GrossPosted - f.GrossRefunded : 0,
        HasInconsistentLedger = f.GrossPosted < f.GrossRefunded || f.GrossPosted < 0 || f.GrossRefunded < 0,
        AdvancePaid = f.Status < ServiceOrderStatus.Completed && f.GrossPosted > f.GrossRefunded ? f.GrossPosted - f.GrossRefunded : 0,
        PlannedRemaining = f.Status < ServiceOrderStatus.Completed && f.PlannedTotal > f.GrossPosted - f.GrossRefunded
            ? f.PlannedTotal - (f.GrossPosted - f.GrossRefunded) : 0,
        AdvanceExcess = f.Status < ServiceOrderStatus.Completed && f.GrossPosted - f.GrossRefunded > f.PlannedTotal
            ? f.GrossPosted - f.GrossRefunded - f.PlannedTotal : 0,
        Receivable = f.Status == ServiceOrderStatus.Completed && f.CompletedTotal > f.GrossPosted - f.GrossRefunded
            ? f.CompletedTotal - (f.GrossPosted - f.GrossRefunded) : 0,
        CustomerCredit = f.GrossPosted - f.GrossRefunded >
            (f.Status == ServiceOrderStatus.Completed ? f.CompletedTotal : f.Status == ServiceOrderStatus.Cancelled ? 0 : f.PlannedTotal)
            ? f.GrossPosted - f.GrossRefunded - (f.Status == ServiceOrderStatus.Completed ? f.CompletedTotal : f.Status == ServiceOrderStatus.Cancelled ? 0 : f.PlannedTotal) : 0,
        RefundDue = f.GrossPosted - f.GrossRefunded >
            (f.Status == ServiceOrderStatus.Completed ? f.CompletedTotal : f.Status == ServiceOrderStatus.Cancelled ? 0 : f.PlannedTotal)
            ? f.GrossPosted - f.GrossRefunded - (f.Status == ServiceOrderStatus.Completed ? f.CompletedTotal : f.Status == ServiceOrderStatus.Cancelled ? 0 : f.PlannedTotal) : 0
    };

    private static readonly Func<ServiceMoneyFacts, ServiceMoneySummary> Calculate = Projection.Compile();
    public static ServiceMoneySummary From(ServiceMoneyFacts facts) => Calculate(facts);

    public void EnsurePaymentAllowed(decimal amount)
    {
        EnsureConsistent();
        var room = Status == ServiceOrderStatus.Completed ? Receivable : PlannedRemaining;
        if (amount <= 0 || Status == ServiceOrderStatus.Cancelled || amount > room)
            throw new BusinessException(ServiceErrorCodes.PaymentExceedsObligation)
                .WithData("ServiceOrderId", ServiceOrderId).WithData("Amount", amount).WithData("AvailableAmount", room);
    }

    public void EnsureRefundAllowed(decimal amount)
    {
        EnsureConsistent();
        if (amount <= 0 || amount > RefundDue)
            throw new BusinessException(ServiceErrorCodes.RefundExceedsCredit)
                .WithData("ServiceOrderId", ServiceOrderId).WithData("Amount", amount).WithData("AvailableAmount", RefundDue);
    }

    public void EnsureVoidAllowed(decimal amount)
    {
        EnsureConsistent();
        if (GrossPosted - amount < GrossRefunded)
            throw new BusinessException(ServiceErrorCodes.VoidWouldInvalidateRefund).WithData("ServiceOrderId", ServiceOrderId);
    }

    private void EnsureConsistent()
    {
        if (HasInconsistentLedger) throw new BusinessException(ServiceErrorCodes.InvalidMoneyLedger)
            .WithData("ServiceOrderId", ServiceOrderId);
    }
}

public class ServiceCustomerMoneySummary
{
    public decimal Receivable { get; set; }
    public decimal AdvancePaid { get; set; }
    public decimal CustomerCredit { get; set; }
    public decimal RefundDue { get; set; }
    public long InconsistentOrderCount { get; set; }
}
