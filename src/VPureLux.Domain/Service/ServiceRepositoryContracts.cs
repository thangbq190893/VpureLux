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
}

public interface IServiceReadRepository
{
    Task<long> GetOrderCountAsync(ServiceOrderFilter filter, CancellationToken cancellationToken = default);
    Task<List<ServiceOrderListItem>> GetOrderListAsync(ServiceOrderFilter filter, CancellationToken cancellationToken = default);
    Task<long> GetAssetCountAsync(Guid? customerId, string? searchText, CancellationToken cancellationToken = default);
    Task<List<ServiceAssetOption>> GetAssetOptionsAsync(Guid? customerId, string? searchText, int skipCount, int maxResultCount, CancellationToken cancellationToken = default);
    Task<long> GetMaterialCountAsync(string? searchText, CancellationToken cancellationToken = default);
    Task<List<ServiceMaterialOption>> GetMaterialOptionsAsync(string? searchText, int skipCount, int maxResultCount, CancellationToken cancellationToken = default);
    Task<long> GetWorkCountAsync(string? searchText, CancellationToken cancellationToken = default);
    Task<List<ServiceWorkOption>> GetWorkOptionsAsync(string? searchText, int skipCount, int maxResultCount, CancellationToken cancellationToken = default);
    Task<long> GetTechnicianCountAsync(string? searchText, CancellationToken cancellationToken = default);
    Task<List<ServiceTechnicianOption>> GetTechnicianOptionsAsync(string? searchText, int skipCount, int maxResultCount, CancellationToken cancellationToken = default);
}
