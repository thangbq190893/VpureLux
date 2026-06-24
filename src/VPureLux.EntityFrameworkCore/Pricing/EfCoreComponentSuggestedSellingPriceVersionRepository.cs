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

namespace VPureLux.Pricing;

public class EfCoreComponentSuggestedSellingPriceVersionRepository :
    EfCoreRepository<VPureLuxDbContext, ComponentSuggestedSellingPriceVersion, Guid>,
    IComponentSuggestedSellingPriceVersionRepository
{
    public EfCoreComponentSuggestedSellingPriceVersionRepository(
        IDbContextProvider<VPureLuxDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public async Task<int> GetNextVersionNoAsync(Guid componentId, CancellationToken cancellationToken = default)
    {
        var current = await (await GetDbSetAsync())
            .Where(x => x.ComponentId == componentId)
            .OrderByDescending(x => x.VersionNo)
            .Select(x => x.VersionNo)
            .FirstOrDefaultAsync(GetCancellationToken(cancellationToken));
        return current == null ? PricingConsts.MinimumVersionNo : checked(current.Value + 1);
    }

    public async Task<ComponentSuggestedSellingPriceVersion?> FindActiveAsync(
        Guid componentId,
        CancellationToken cancellationToken = default)
    {
        return await (await GetDbSetAsync()).FirstOrDefaultAsync(
            x => x.ComponentId == componentId && x.Status == PriceVersionStatus.Active,
            GetCancellationToken(cancellationToken));
    }

    public async Task<ComponentSuggestedSellingPriceVersion?> FindAtDateAsync(
        Guid componentId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        return await (await GetDbSetAsync()).FirstOrDefaultAsync(
            x => x.ComponentId == componentId &&
                 x.EffectivePeriod.EffectiveFrom <= date &&
                 (!x.EffectivePeriod.EffectiveTo.HasValue || date < x.EffectivePeriod.EffectiveTo.Value),
            GetCancellationToken(cancellationToken));
    }

    public async Task<IReadOnlyDictionary<Guid, ComponentSuggestedSellingPriceVersion>> FindAtDateMapAsync(
        IReadOnlyCollection<Guid> componentIds,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        if (componentIds.Count == 0)
        {
            return new Dictionary<Guid, ComponentSuggestedSellingPriceVersion>();
        }

        var idSet = componentIds.Distinct().ToHashSet();
        var versions = await (await GetDbSetAsync())
            .Where(x => idSet.Contains(x.ComponentId) &&
                        x.EffectivePeriod.EffectiveFrom <= date &&
                        (!x.EffectivePeriod.EffectiveTo.HasValue || date < x.EffectivePeriod.EffectiveTo.Value))
            .ToListAsync(GetCancellationToken(cancellationToken));

        return versions
            .GroupBy(x => x.ComponentId)
            .ToDictionary(x => x.Key, x => x.First());
    }

    public async Task<List<ComponentSuggestedSellingPriceVersion>> GetHistoryAsync(
        Guid componentId,
        CancellationToken cancellationToken = default)
    {
        return await (await GetDbSetAsync())
            .Where(x => x.ComponentId == componentId)
            .OrderByDescending(x => x.VersionNo)
            .ToListAsync(GetCancellationToken(cancellationToken));
    }

    public override async Task<ComponentSuggestedSellingPriceVersion> InsertAsync(
        ComponentSuggestedSellingPriceVersion entity,
        bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.InsertAsync(entity, autoSave, cancellationToken);
        }
        catch (DbUpdateException exception) when (IsActiveUniqueViolation(exception))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ActivePriceVersionAlreadyExists)
                .WithData(nameof(entity.ComponentId), entity.ComponentId);
        }
    }

    private static bool IsActiveUniqueViolation(Exception exception)
    {
        return ContainsIndexName(exception, ComponentSuggestedSellingPriceVersionConfiguration.ActiveComponentUniqueIndexName) ||
               ContainsIndexName(exception, ComponentSuggestedSellingPriceVersionConfiguration.ComponentVersionUniqueIndexName) ||
               ContainsIndexName(exception, "UNIQUE constraint failed: AppComponentSuggestedSellingPriceVersions.ComponentId");
    }

    private static bool ContainsIndexName(Exception exception, string indexName)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current.Message.Contains(indexName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
