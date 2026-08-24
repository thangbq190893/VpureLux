using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace VPureLux.Service;

public interface IServiceOrderRepository : IRepository<ServiceOrder, Guid>
{
    Task<bool> OrderNoExistsAsync(string orderNo, CancellationToken cancellationToken = default);
    Task<int> GetMaxOrderNoSequenceAsync(string orderNoPrefix, CancellationToken cancellationToken = default);
    Task<ServiceOrder?> FindByCompletionKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
}

public interface IServiceWorkRepository : IRepository<ServiceWork, Guid>
{
    Task<bool> CodeExistsAsync(string code, Guid? excludedId = null, CancellationToken cancellationToken = default);
}

public interface IServicePaymentRepository : IRepository<ServicePayment, Guid>
{
    Task<ServicePayment?> FindByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<List<ServicePayment>> GetByOrderIdAsync(Guid serviceOrderId, CancellationToken cancellationToken = default);
}

public interface IServiceReadRepository
{
    Task<long> GetOrderCountAsync(ServiceOrderFilter filter, CancellationToken cancellationToken = default);
    Task<List<ServiceOrderListItem>> GetOrderListAsync(ServiceOrderFilter filter, CancellationToken cancellationToken = default);
    Task<List<ServiceAssetOption>> GetAssetOptionsAsync(Guid? customerId, string? searchText, int maxResultCount, CancellationToken cancellationToken = default);
    Task<List<ServiceMaterialOption>> GetMaterialOptionsAsync(string? searchText, int maxResultCount, CancellationToken cancellationToken = default);
    Task<List<ServiceWorkOption>> GetWorkOptionsAsync(string? searchText, int maxResultCount, CancellationToken cancellationToken = default);
}
