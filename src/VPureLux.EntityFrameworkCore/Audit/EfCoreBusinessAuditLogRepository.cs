using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VPureLux.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;

namespace VPureLux.Audit;

public class EfCoreBusinessAuditLogRepository : IBusinessAuditLogRepository, ITransientDependency
{
    private readonly IDbContextProvider<VPureLuxDbContext> _provider;
    public EfCoreBusinessAuditLogRepository(IDbContextProvider<VPureLuxDbContext> provider) => _provider = provider;

    public async Task InsertAsync(BusinessAuditLog log, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        var db = await _provider.GetDbContextAsync();
        await db.Set<BusinessAuditLog>().AddAsync(log, cancellationToken);
        if (autoSave) await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<BusinessAuditLog?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        await (await QueryAsync()).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<long> GetCountAsync(AuditSearchFilter filter, CancellationToken cancellationToken = default) =>
        await Apply(await QueryAsync(), filter).LongCountAsync(cancellationToken);

    public async Task<List<BusinessAuditLog>> GetListAsync(AuditSearchFilter filter, CancellationToken cancellationToken = default) =>
        await Apply(await QueryAsync(), filter).OrderByDescending(x => x.EventTime).ThenByDescending(x => x.Id)
            .Skip(filter.SkipCount).Take(filter.MaxResultCount).ToListAsync(cancellationToken);

    private async Task<IQueryable<BusinessAuditLog>> QueryAsync() =>
        (await _provider.GetDbContextAsync()).Set<BusinessAuditLog>().AsNoTracking();

    private static IQueryable<BusinessAuditLog> Apply(IQueryable<BusinessAuditLog> q, AuditSearchFilter f) =>
        q.Where(x => !f.From.HasValue || x.EventTime >= f.From)
            .Where(x => !f.To.HasValue || x.EventTime <= f.To)
            .Where(x => !f.UserId.HasValue || x.UserId == f.UserId)
            .Where(x => f.Module == null || x.Module == f.Module)
            .Where(x => f.EntityType == null || x.EntityType == f.EntityType)
            .Where(x => !f.EntityId.HasValue || x.EntityId == f.EntityId)
            .Where(x => !f.Severity.HasValue || x.Severity == f.Severity)
            .Where(x => f.CorrelationId == null || x.CorrelationId == f.CorrelationId);
}
