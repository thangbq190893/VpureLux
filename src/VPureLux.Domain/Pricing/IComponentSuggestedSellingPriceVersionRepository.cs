using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace VPureLux.Pricing;

public interface IComponentSuggestedSellingPriceVersionRepository
    : IRepository<ComponentSuggestedSellingPriceVersion, Guid>
{
    Task<int> GetNextVersionNoAsync(Guid componentId, CancellationToken cancellationToken = default);

    Task<ComponentSuggestedSellingPriceVersion?> FindActiveAsync(
        Guid componentId,
        CancellationToken cancellationToken = default);

    Task<ComponentSuggestedSellingPriceVersion?> FindAtDateAsync(
        Guid componentId,
        DateTime date,
        CancellationToken cancellationToken = default);

    Task<List<ComponentSuggestedSellingPriceVersion>> GetHistoryAsync(
        Guid componentId,
        CancellationToken cancellationToken = default);
}
