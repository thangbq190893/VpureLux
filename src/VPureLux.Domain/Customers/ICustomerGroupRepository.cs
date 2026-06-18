using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace VPureLux.Customers;

public interface ICustomerGroupRepository : IRepository<CustomerGroup, Guid>
{
    Task<CustomerGroup?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        string code,
        Guid? excludedId = null,
        CancellationToken cancellationToken = default);

    Task<List<CustomerGroup>> GetListAsync(
        string? searchText = null,
        CustomerGroupStatus? status = null,
        string? sorting = null,
        int maxResultCount = int.MaxValue,
        int skipCount = 0,
        CancellationToken cancellationToken = default);

    Task<long> GetCountAsync(
        string? searchText = null,
        CustomerGroupStatus? status = null,
        CancellationToken cancellationToken = default);
}
