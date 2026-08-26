using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VPureLux.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;

namespace VPureLux.Sales;

public class EfCoreSalesOrderRevisionRepository : EfCoreRepository<VPureLuxDbContext, SalesOrderRevision, Guid>, ISalesOrderRevisionRepository
{
    public EfCoreSalesOrderRevisionRepository(IDbContextProvider<VPureLuxDbContext> provider) : base(provider) { }

    public async Task<SalesOrderRevision?> FindActiveByOrderIdAsync(Guid salesOrderId, CancellationToken cancellationToken = default) =>
        await (await WithDetailsAsync(x => x.Lines)).FirstOrDefaultAsync(
            x => x.SalesOrderId == salesOrderId && x.Status == SalesOrderRevisionStatus.Draft,
            GetCancellationToken(cancellationToken));

    public async Task<SalesOrderRevision?> FindByApplyIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
        await (await WithDetailsAsync(x => x.Lines)).FirstOrDefaultAsync(
            x => x.ApplyIdempotencyKey == idempotencyKey,
            GetCancellationToken(cancellationToken));

    public async Task<int> GetNextRevisionNoAsync(Guid salesOrderId, CancellationToken cancellationToken = default) =>
        (await (await GetQueryableAsync()).Where(x => x.SalesOrderId == salesOrderId)
            .Select(x => (int?)x.RevisionNo).MaxAsync(GetCancellationToken(cancellationToken)) ?? 0) + 1;

    public async Task<List<SalesOrderRevision>> GetAppliedByOrderIdAsync(Guid salesOrderId, CancellationToken cancellationToken = default) =>
        await (await WithDetailsAsync(x => x.Lines)).Where(
            x => x.SalesOrderId == salesOrderId && x.Status == SalesOrderRevisionStatus.Applied)
            .OrderBy(x => x.RevisionNo).ToListAsync(GetCancellationToken(cancellationToken));

    public override async Task<SalesOrderRevision> UpdateAsync(
        SalesOrderRevision entity,
        bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        var db = await GetDbContextAsync();
        if (db.Entry(entity).State == EntityState.Detached)
        {
            db.SalesOrderRevisions.Update(entity);
        }
        db.ChangeTracker.DetectChanges();
        var persistedLineIds = await db.SalesOrderRevisions.Where(x => x.Id == entity.Id)
            .SelectMany(x => x.Lines).Select(x => x.Id)
            .ToListAsync(GetCancellationToken(cancellationToken));
        var persistedAllocationIds = await db.SalesOrderRevisions.Where(x => x.Id == entity.Id)
            .SelectMany(x => x.Lines).SelectMany(x => x.ReversedAllocations).Select(x => x.Id)
            .ToListAsync(GetCancellationToken(cancellationToken));
        foreach (var entry in db.ChangeTracker.Entries<SalesOrderRevisionLine>()
                     .Where(x => x.State == EntityState.Modified && !persistedLineIds.Contains(x.Entity.Id)))
        {
            entry.State = EntityState.Added;
        }
        foreach (var entry in db.ChangeTracker.Entries<SalesOrderRevisionAllocation>()
                     .Where(x => x.State == EntityState.Modified && !persistedAllocationIds.Contains(x.Entity.Id)))
        {
            entry.State = EntityState.Added;
        }
        if (autoSave)
        {
            await db.SaveChangesAsync(GetCancellationToken(cancellationToken));
        }
        return entity;
    }
}

public class EfCoreSalesOrderCancellationRepository : EfCoreRepository<VPureLuxDbContext, SalesOrderCancellation, Guid>, ISalesOrderCancellationRepository
{
    public EfCoreSalesOrderCancellationRepository(IDbContextProvider<VPureLuxDbContext> provider) : base(provider) { }
    public async Task<SalesOrderCancellation?> FindByOrderIdAsync(Guid salesOrderId, CancellationToken cancellationToken = default) =>
        await (await GetQueryableAsync()).FirstOrDefaultAsync(x => x.SalesOrderId == salesOrderId, GetCancellationToken(cancellationToken));
}

public class EfCoreSalesOrderRefundRepository : EfCoreRepository<VPureLuxDbContext, SalesOrderRefund, Guid>, ISalesOrderRefundRepository
{
    public EfCoreSalesOrderRefundRepository(IDbContextProvider<VPureLuxDbContext> provider) : base(provider) { }
    public async Task<SalesOrderRefund?> FindByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
        await (await GetQueryableAsync()).FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, GetCancellationToken(cancellationToken));
    public async Task<decimal> GetRefundedAmountForRevisionAsync(Guid revisionId, CancellationToken cancellationToken = default) =>
        await (await GetQueryableAsync()).Where(x => x.SalesOrderRevisionId == revisionId)
            .SumAsync(x => x.Amount, GetCancellationToken(cancellationToken));
}
