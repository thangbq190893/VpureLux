using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VPureLux.Service;

public class ServiceMoneyHistoryFilter
{
    public Guid ServiceOrderId { get; set; }
    public string? SearchText { get; set; }
    public ServicePaymentStatus? Status { get; set; }
    public string? Sorting { get; set; }
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; } = 10;
}

public interface IServiceMoneyReadRepository
{
    Task<ServiceMoneySummary> GetSummaryAsync(Guid orderId);
    Task<ServiceCustomerMoneySummary> GetCustomerSummaryAsync(Guid customerId);
    Task<(long Count, List<ServicePayment> Items)> GetPaymentsAsync(ServiceMoneyHistoryFilter filter);
    Task<(long Count, List<ServiceRefund> Items)> GetRefundsAsync(ServiceMoneyHistoryFilter filter);
}
