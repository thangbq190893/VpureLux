using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace VPureLux.Service;

public interface IServiceWorkRepository : IRepository<ServiceWork, Guid>
{
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);
    Task<long> GetCountAsync(string? search, ServiceWorkStatus? status, CancellationToken cancellationToken = default);
    Task<List<ServiceWork>> GetListAsync(string? search, ServiceWorkStatus? status, string? sorting,
        int skipCount, int maxResultCount, CancellationToken cancellationToken = default);
}
