using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace VPureLux.Pricing;

public interface IProductSuggestedPriceAppService : IApplicationService
{
    Task<List<ProductSuggestedPriceVersionDto>> GetHistoryAsync(Guid productId);

    Task<ProductSuggestedPriceVersionDto> GetCurrentAsync(Guid productId);

    Task<ProductSuggestedPriceVersionDto> GetAtDateAsync(Guid productId, DateTime date);

    Task<ProductSuggestedPriceVersionDto> CreateAsync(
        Guid productId,
        CreateProductSuggestedPriceVersionDto input);
}
