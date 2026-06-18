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

public class EfCoreProductSuggestedPriceVersionRepository :
    EfCoreRepository<VPureLuxDbContext, ProductSuggestedPriceVersion, Guid>,
    IProductSuggestedPriceVersionRepository
{
    public EfCoreProductSuggestedPriceVersionRepository(
        IDbContextProvider<VPureLuxDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public async Task<int> GetNextVersionNoAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var current = await (await GetDbSetAsync())
            .Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.VersionNo)
            .Select(x => x.VersionNo)
            .FirstOrDefaultAsync(GetCancellationToken(cancellationToken));
        return current == null ? PricingConsts.MinimumVersionNo : checked(current.Value + 1);
    }

    public async Task<ProductSuggestedPriceVersion?> FindActiveAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await (await GetDbSetAsync()).FirstOrDefaultAsync(
            x => x.ProductId == productId && x.Status == PriceVersionStatus.Active,
            GetCancellationToken(cancellationToken));
    }

    public async Task<ProductSuggestedPriceVersion?> FindAtDateAsync(
        Guid productId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        return await (await GetDbSetAsync()).FirstOrDefaultAsync(
            x => x.ProductId == productId &&
                 x.EffectivePeriod.EffectiveFrom <= date &&
                 (!x.EffectivePeriod.EffectiveTo.HasValue || date < x.EffectivePeriod.EffectiveTo.Value),
            GetCancellationToken(cancellationToken));
    }

    public async Task<List<ProductSuggestedPriceVersion>> GetHistoryAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await (await GetDbSetAsync())
            .Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.VersionNo)
            .ToListAsync(GetCancellationToken(cancellationToken));
    }

    public override async Task<ProductSuggestedPriceVersion> InsertAsync(
        ProductSuggestedPriceVersion entity,
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
                .WithData(nameof(entity.ProductId), entity.ProductId);
        }
    }

    private static bool IsActiveUniqueViolation(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current.Message.Contains(
                    ProductSuggestedPriceVersionConfiguration.ActiveProductUniqueIndexName,
                    StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains(
                    ProductSuggestedPriceVersionConfiguration.ProductVersionUniqueIndexName,
                    StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains(
                    "UNIQUE constraint failed: AppProductSuggestedPriceVersions.ProductId",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
