using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace VPureLux.Pricing;

public interface IProductSuggestedPriceVersionRepository
    : IRepository<ProductSuggestedPriceVersion, Guid>
{
    Task<int> GetNextVersionNoAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<ProductSuggestedPriceVersion?> FindActiveAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<ProductSuggestedPriceVersion?> FindAtDateAsync(
        Guid productId,
        DateTime date,
        CancellationToken cancellationToken = default);

    Task<List<ProductSuggestedPriceVersion>> GetHistoryAsync(
        Guid productId,
        CancellationToken cancellationToken = default);
}
