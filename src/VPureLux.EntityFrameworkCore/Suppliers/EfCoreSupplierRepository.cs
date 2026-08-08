using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VPureLux.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace VPureLux.Suppliers;

public class EfCoreSupplierRepository : EfCoreRepository<VPureLuxDbContext, Supplier, Guid>, ISupplierRepository
{
    public EfCoreSupplierRepository(IDbContextProvider<VPureLuxDbContext> provider) : base(provider)
    {
    }

    public async Task<bool> CodeExistsAsync(string code, Guid? excludedId = null, CancellationToken cancellationToken = default)
    {
        code = Supplier.NormalizeCode(code);
        return await (await GetDbSetAsync())
            .AnyAsync(
                x => x.Code == code && (!excludedId.HasValue || x.Id != excludedId.Value),
                GetCancellationToken(cancellationToken));
    }

    public async Task<long> GetCountAsync(string? searchText = null, CancellationToken cancellationToken = default)
    {
        return await ApplySearch(await GetDbSetAsync(), searchText)
            .LongCountAsync(GetCancellationToken(cancellationToken));
    }

    public async Task<List<Supplier>> GetListAsync(
        string? searchText = null,
        string? sorting = null,
        int maxResultCount = 10,
        int skipCount = 0,
        CancellationToken cancellationToken = default)
    {
        return await ApplySorting(ApplySearch(await GetDbSetAsync(), searchText), sorting)
            .Skip(skipCount)
            .Take(maxResultCount)
            .ToListAsync(GetCancellationToken(cancellationToken));
    }

    private static IQueryable<Supplier> ApplySearch(IQueryable<Supplier> query, string? searchText)
    {
        if (searchText.IsNullOrWhiteSpace())
        {
            return query;
        }

        return query.Where(x =>
            x.Code.Contains(searchText!) ||
            x.Name.Contains(searchText!) ||
            (x.Phone != null && x.Phone.Contains(searchText!)) ||
            (x.TaxCode != null && x.TaxCode.Contains(searchText!)));
    }

    private static IQueryable<Supplier> ApplySorting(IQueryable<Supplier> query, string? sorting)
    {
        if (sorting.IsNullOrWhiteSpace())
        {
            return query.OrderByDescending(x => x.CreationTime);
        }

        var parts = sorting.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var field = parts[0].Split('.').Last().ToLowerInvariant();
        var desc = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

        return field switch
        {
            "code" => desc ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
            "name" => desc ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            "phone" => desc ? query.OrderByDescending(x => x.Phone) : query.OrderBy(x => x.Phone),
            "taxcode" => desc ? query.OrderByDescending(x => x.TaxCode) : query.OrderBy(x => x.TaxCode),
            "creationtime" => desc ? query.OrderByDescending(x => x.CreationTime) : query.OrderBy(x => x.CreationTime),
            _ => query.OrderByDescending(x => x.CreationTime)
        };
    }
}
