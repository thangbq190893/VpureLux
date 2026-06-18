using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace VPureLux.Sales;

public sealed record CustomerPurchaseHistoryRecord(
    Guid CustomerId,
    Guid ProductId,
    DateTime LastPurchaseDate,
    decimal LastPurchasePrice,
    decimal AveragePurchasePrice,
    decimal Revenue,
    decimal Profit);

public interface ISalesOrderRepository : IRepository<SalesOrder, Guid>
{
    Task<int> GetNextOrderSequenceAsync(string yearMonth, CancellationToken cancellationToken = default);
    Task<SalesOrder?> FindByConfirmationIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<List<SalesOrder>> GetListAsync(
        Guid? customerId = null,
        SalesOrderStatus? status = null,
        string? sorting = null,
        int maxResultCount = int.MaxValue,
        int skipCount = 0,
        CancellationToken cancellationToken = default);
    Task<long> GetCountAsync(
        Guid? customerId = null,
        SalesOrderStatus? status = null,
        CancellationToken cancellationToken = default);
    Task<List<CustomerPurchaseHistoryRecord>> GetCustomerPurchaseHistoryAsync(Guid customerId, CancellationToken cancellationToken = default);
}
