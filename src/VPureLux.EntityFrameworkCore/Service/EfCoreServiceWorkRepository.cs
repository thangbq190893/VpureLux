using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VPureLux.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;

namespace VPureLux.Service;

public class EfCoreServiceWorkRepository : EfCoreRepository<VPureLuxDbContext, ServiceWork, Guid>, IServiceWorkRepository
{
    public EfCoreServiceWorkRepository(IDbContextProvider<VPureLuxDbContext> provider) : base(provider) { }

    public async Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default) =>
        await (await GetDbSetAsync()).IgnoreQueryFilters().AnyAsync(x => x.Code == code, cancellationToken);

    public async Task<long> GetCountAsync(string? search, ServiceWorkStatus? status, CancellationToken cancellationToken = default) =>
        await Filter(await GetQueryableAsync(), search, status).LongCountAsync(cancellationToken);

    public async Task<List<ServiceWork>> GetListAsync(string? search, ServiceWorkStatus? status, string? sorting,
        int skipCount, int maxResultCount, CancellationToken cancellationToken = default)
    {
        var query = Filter((await GetQueryableAsync()).AsNoTracking(), search, status);
        var parts = (sorting ?? "code asc").Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var descending = parts.Length > 1 && parts[1] == "desc";
        var ordered = (parts.FirstOrDefault(), descending) switch
        {
            ("name", false) => query.OrderBy(x => x.Name),
            ("name", true) => query.OrderByDescending(x => x.Name),
            ("unit", false) => query.OrderBy(x => x.Unit),
            ("unit", true) => query.OrderByDescending(x => x.Unit),
            ("status", false) => query.OrderBy(x => x.Status),
            ("status", true) => query.OrderByDescending(x => x.Status),
            (_, true) => query.OrderByDescending(x => x.Code),
            _ => query.OrderBy(x => x.Code)
        };
        return await ordered.ThenBy(x => x.Id).Skip(skipCount).Take(maxResultCount).ToListAsync(cancellationToken);
    }

    private static IQueryable<ServiceWork> Filter(IQueryable<ServiceWork> query, string? search, ServiceWorkStatus? status)
    {
        search = search?.Trim();
        if (!string.IsNullOrEmpty(search)) query = query.Where(x => x.Code.Contains(search) || x.Name.Contains(search));
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        return query;
    }
}
