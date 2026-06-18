using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VPureLux.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace VPureLux.Customers;

public class EfCoreCustomerRepository :
    EfCoreRepository<VPureLuxDbContext, Customer, Guid>,
    ICustomerRepository
{
    public EfCoreCustomerRepository(IDbContextProvider<VPureLuxDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public async Task<Customer?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        code = NormalizeCode(code);
        return await (await GetDbSetAsync())
            .FirstOrDefaultAsync(x => x.Code == code, GetCancellationToken(cancellationToken));
    }

    public async Task<bool> CodeExistsAsync(
        string code,
        Guid? excludedId = null,
        CancellationToken cancellationToken = default)
    {
        code = NormalizeCode(code);
        return await (await GetDbSetAsync()).AnyAsync(
            x => x.Code == code && (!excludedId.HasValue || x.Id != excludedId.Value),
            GetCancellationToken(cancellationToken));
    }

    public async Task<bool> HasCustomersInGroupAsync(
        Guid customerGroupId,
        CancellationToken cancellationToken = default)
    {
        return await (await GetDbSetAsync()).AnyAsync(
            x => x.CustomerGroupId == customerGroupId,
            GetCancellationToken(cancellationToken));
    }

    public async Task<List<Customer>> GetListAsync(
        string? searchText = null,
        Guid? customerGroupId = null,
        CustomerStatus? status = null,
        string? sorting = null,
        int maxResultCount = int.MaxValue,
        int skipCount = 0,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(await GetQueryableAsync(), searchText, customerGroupId, status);
        query = sorting?.Trim().ToLowerInvariant() switch
        {
            "name" => query.OrderBy(x => x.Name),
            "name desc" => query.OrderByDescending(x => x.Name),
            "code desc" => query.OrderByDescending(x => x.Code),
            _ => query.OrderBy(x => x.Code)
        };

        return await query.Skip(skipCount).Take(maxResultCount)
            .ToListAsync(GetCancellationToken(cancellationToken));
    }

    public async Task<long> GetCountAsync(
        string? searchText = null,
        Guid? customerGroupId = null,
        CustomerStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        return await ApplyFilters(await GetQueryableAsync(), searchText, customerGroupId, status)
            .LongCountAsync(GetCancellationToken(cancellationToken));
    }

    private static IQueryable<Customer> ApplyFilters(
        IQueryable<Customer> query,
        string? searchText,
        Guid? customerGroupId,
        CustomerStatus? status)
    {
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var value = searchText.Trim();
            query = query.Where(x =>
                x.Code.Contains(value) ||
                x.Name.Contains(value) ||
                (x.PhoneNumber != null && x.PhoneNumber.Contains(value)) ||
                (x.Email != null && x.Email.Contains(value)));
        }

        if (customerGroupId.HasValue)
        {
            query = query.Where(x => x.CustomerGroupId == customerGroupId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        return query;
    }

    private static string NormalizeCode(string code)
    {
        return code.Trim().ToUpperInvariant();
    }
}
