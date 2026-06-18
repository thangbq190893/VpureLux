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

public class EfCoreCustomerGroupRepository :
    EfCoreRepository<VPureLuxDbContext, CustomerGroup, Guid>,
    ICustomerGroupRepository
{
    public EfCoreCustomerGroupRepository(IDbContextProvider<VPureLuxDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public async Task<CustomerGroup?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
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

    public async Task<List<CustomerGroup>> GetListAsync(
        string? searchText = null,
        CustomerGroupStatus? status = null,
        string? sorting = null,
        int maxResultCount = int.MaxValue,
        int skipCount = 0,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(await GetQueryableAsync(), searchText, status);
        query = sorting?.Trim().ToLowerInvariant() switch
        {
            "name" => query.OrderBy(x => x.Name),
            "name desc" => query.OrderByDescending(x => x.Name),
            "code" => query.OrderBy(x => x.Code),
            "code desc" => query.OrderByDescending(x => x.Code),
            "sortorder desc" => query.OrderByDescending(x => x.SortOrder).ThenBy(x => x.Code),
            _ => query.OrderBy(x => x.SortOrder).ThenBy(x => x.Code)
        };

        return await query.Skip(skipCount).Take(maxResultCount)
            .ToListAsync(GetCancellationToken(cancellationToken));
    }

    public async Task<long> GetCountAsync(
        string? searchText = null,
        CustomerGroupStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        return await ApplyFilters(await GetQueryableAsync(), searchText, status)
            .LongCountAsync(GetCancellationToken(cancellationToken));
    }

    private static IQueryable<CustomerGroup> ApplyFilters(
        IQueryable<CustomerGroup> query,
        string? searchText,
        CustomerGroupStatus? status)
    {
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var value = searchText.Trim();
            query = query.Where(x => x.Code.Contains(value) || x.Name.Contains(value));
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
