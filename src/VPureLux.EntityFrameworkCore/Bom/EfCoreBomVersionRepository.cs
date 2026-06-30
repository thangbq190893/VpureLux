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

namespace VPureLux.Bom;

public class EfCoreBomVersionRepository :
    EfCoreRepository<VPureLuxDbContext, BomVersion, Guid>,
    IBomVersionRepository
{
    public EfCoreBomVersionRepository(IDbContextProvider<VPureLuxDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public override async Task<IQueryable<BomVersion>> WithDetailsAsync()
    {
        return (await GetQueryableAsync()).Include(x => x.Items);
    }

    public override async Task<BomVersion> UpdateAsync(
        BomVersion entity,
        bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dbContext = await GetDbContextAsync();
            if (dbContext.Entry(entity).State == EntityState.Detached)
            {
                dbContext.BomVersions.Update(entity);
            }

            dbContext.ChangeTracker.DetectChanges();
            var persistedItemIds = await dbContext.BomVersions
                .Where(x => x.Id == entity.Id)
                .SelectMany(x => x.Items)
                .Select(x => x.Id)
                .ToListAsync(GetCancellationToken(cancellationToken));

            foreach (var item in dbContext.ChangeTracker.Entries<BomItem>()
                         .Where(x => x.State == EntityState.Modified && !persistedItemIds.Contains(x.Entity.Id)))
            {
                item.State = EntityState.Added;
            }

            if (autoSave)
            {
                await dbContext.SaveChangesAsync(GetCancellationToken(cancellationToken));
            }

            return entity;
        }
        catch (DbUpdateException exception) when (IsPublishedProductUniqueViolation(exception))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.OnlyOneActiveBomAllowed)
                .WithData(nameof(entity.ProductId), entity.ProductId);
        }
    }

    public async Task<int> GetNextVersionNoAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var currentVersion = await (await GetDbSetAsync())
            .Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.VersionNo)
            .Select(x => x.VersionNo)
            .FirstOrDefaultAsync(GetCancellationToken(cancellationToken));

        return currentVersion != null
            ? checked(currentVersion.Value + 1)
            : BomConsts.MinimumVersionNo;
    }

    public async Task<bool> HasPublishedVersionAsync(
        Guid productId,
        Guid? excludedId = null,
        CancellationToken cancellationToken = default)
    {
        return await (await GetDbSetAsync())
            .AnyAsync(
                x => x.ProductId == productId &&
                     x.Status == BomStatus.Published &&
                     (!excludedId.HasValue || x.Id != excludedId.Value),
                GetCancellationToken(cancellationToken));
    }

    public async Task<List<BomVersion>> GetListByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await (await WithDetailsAsync())
            .Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.VersionNo)
            .ToListAsync(GetCancellationToken(cancellationToken));
    }

    public async Task<IReadOnlyDictionary<Guid, BomVersion>> GetPublishedMapByProductIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<Guid, BomVersion>();
        }

        var idSet = productIds.Distinct().ToHashSet();
        var versions = await (await WithDetailsAsync())
            .Where(x => idSet.Contains(x.ProductId) && x.Status == BomStatus.Published)
            .OrderByDescending(x => x.VersionNo)
            .ToListAsync(GetCancellationToken(cancellationToken));

        return versions
            .GroupBy(x => x.ProductId)
            .ToDictionary(x => x.Key, x => x.First());
    }

    private static bool IsPublishedProductUniqueViolation(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current.Message.Contains(
                    BomVersionConfiguration.PublishedProductUniqueIndexName,
                    StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains(
                    "UNIQUE constraint failed: AppBomVersions.ProductId",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
