using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace VPureLux.Pricing;

public interface IComponentSuggestedSellingPriceLookupService
{
    Task<IReadOnlyDictionary<Guid, ComponentSuggestedSellingPriceVersionDto>> FindCurrentMapAsync(
        IReadOnlyCollection<Guid> componentIds,
        DateTime date);
}

public class ComponentSuggestedSellingPriceLookupService : IComponentSuggestedSellingPriceLookupService, ITransientDependency
{
    private readonly IComponentSuggestedSellingPriceVersionRepository _repository;
    private readonly PricingApplicationMapper _mapper;

    public ComponentSuggestedSellingPriceLookupService(
        IComponentSuggestedSellingPriceVersionRepository repository,
        PricingApplicationMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyDictionary<Guid, ComponentSuggestedSellingPriceVersionDto>> FindCurrentMapAsync(
        IReadOnlyCollection<Guid> componentIds,
        DateTime date)
    {
        if (componentIds.Count == 0)
        {
            return new Dictionary<Guid, ComponentSuggestedSellingPriceVersionDto>();
        }

        var versions = await _repository.FindAtDateMapAsync(componentIds, date);
        return versions.ToDictionary(
            x => x.Key,
            x => _mapper.ToDto(x.Value));
    }
}
