using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace VPureLux.Pricing;

public interface IComponentSuggestedSellingPriceAppService : IApplicationService
{
    Task<List<ComponentSuggestedSellingPriceVersionDto>> GetHistoryAsync(Guid componentId);

    Task<ComponentSuggestedSellingPriceVersionDto> GetCurrentAsync(Guid componentId);

    Task<ComponentSuggestedSellingPriceVersionDto> GetAtDateAsync(Guid componentId, DateTime date);

    Task<ComponentSuggestedSellingPriceVersionDto> CreateAsync(
        Guid componentId,
        CreateComponentSuggestedSellingPriceVersionDto input);
}
