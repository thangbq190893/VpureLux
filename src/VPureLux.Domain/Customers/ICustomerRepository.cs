using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace VPureLux.Customers;

public interface ICustomerRepository : IRepository<Customer, Guid>
{
    Task<Customer?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        string code,
        Guid? excludedId = null,
        CancellationToken cancellationToken = default);

    Task<bool> HasCustomersInGroupAsync(
        Guid customerGroupId,
        CancellationToken cancellationToken = default);

    Task<List<Customer>> GetListAsync(
        string? searchText = null,
        Guid? customerGroupId = null,
        CustomerStatus? status = null,
        string? sorting = null,
        int maxResultCount = int.MaxValue,
        int skipCount = 0,
        CancellationToken cancellationToken = default);

    Task<long> GetCountAsync(
        string? searchText = null,
        Guid? customerGroupId = null,
        CustomerStatus? status = null,
        CancellationToken cancellationToken = default);
}
