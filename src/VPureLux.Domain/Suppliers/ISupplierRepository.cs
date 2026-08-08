using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace VPureLux.Suppliers;

public interface ISupplierRepository : IRepository<Supplier, Guid>
{
    Task<bool> CodeExistsAsync(string code, Guid? excludedId = null, CancellationToken cancellationToken = default);

    Task<long> GetCountAsync(string? searchText = null, CancellationToken cancellationToken = default);

    Task<List<Supplier>> GetListAsync(
        string? searchText = null,
        string? sorting = null,
        int maxResultCount = 10,
        int skipCount = 0,
        CancellationToken cancellationToken = default);
}
