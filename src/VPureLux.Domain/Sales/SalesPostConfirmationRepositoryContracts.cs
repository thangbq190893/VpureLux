using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace VPureLux.Sales;

public interface ISalesOrderRevisionRepository : IRepository<SalesOrderRevision, Guid>
{
    Task<SalesOrderRevision?> FindActiveByOrderIdAsync(Guid salesOrderId, CancellationToken cancellationToken = default);
    Task<SalesOrderRevision?> FindByApplyIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<int> GetNextRevisionNoAsync(Guid salesOrderId, CancellationToken cancellationToken = default);
    Task<List<SalesOrderRevision>> GetAppliedByOrderIdAsync(Guid salesOrderId, CancellationToken cancellationToken = default);
}

public interface ISalesOrderCancellationRepository : IRepository<SalesOrderCancellation, Guid>
{
    Task<SalesOrderCancellation?> FindByOrderIdAsync(Guid salesOrderId, CancellationToken cancellationToken = default);
}

public interface ISalesOrderRefundRepository : IRepository<SalesOrderRefund, Guid>
{
    Task<SalesOrderRefund?> FindByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<decimal> GetRefundedAmountForRevisionAsync(Guid revisionId, CancellationToken cancellationToken = default);
}

public interface ISalesPostConfirmationReadRepository
{
    Task<long> GetReturnTaskCountAsync(SalesReturnTaskFilter filter, CancellationToken cancellationToken = default);
    Task<List<SalesReturnTaskReadItem>> GetReturnTasksAsync(SalesReturnTaskFilter filter, CancellationToken cancellationToken = default);
    Task<long> GetRefundTaskCountAsync(SalesRefundTaskFilter filter, CancellationToken cancellationToken = default);
    Task<List<SalesRefundTaskReadItem>> GetRefundTasksAsync(SalesRefundTaskFilter filter, CancellationToken cancellationToken = default);
}
