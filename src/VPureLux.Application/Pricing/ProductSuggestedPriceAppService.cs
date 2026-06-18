using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace VPureLux.Pricing;

[Authorize(VPureLuxPermissions.Pricing.View)]
public class ProductSuggestedPriceAppService
    : ApplicationService, IProductSuggestedPriceAppService
{
    private readonly IProductSuggestedPriceVersionRepository _repository;
    private readonly PricingManager _pricingManager;
    private readonly PricingCatalogValidator _catalogValidator;
    private readonly PricingApplicationMapper _mapper;

    public ProductSuggestedPriceAppService(
        IProductSuggestedPriceVersionRepository repository,
        PricingManager pricingManager,
        PricingCatalogValidator catalogValidator,
        PricingApplicationMapper mapper)
    {
        _repository = repository;
        _pricingManager = pricingManager;
        _catalogValidator = catalogValidator;
        _mapper = mapper;
    }

    [Authorize(VPureLuxPermissions.Pricing.History)]
    public async Task<List<ProductSuggestedPriceVersionDto>> GetHistoryAsync(Guid productId)
    {
        await _catalogValidator.ValidateProductExistsAsync(productId);
        return (await _repository.GetHistoryAsync(productId)).Select(_mapper.ToDto).ToList();
    }

    public async Task<ProductSuggestedPriceVersionDto> GetCurrentAsync(Guid productId)
    {
        return await GetAtDateAsync(productId, Clock.Now);
    }

    public async Task<ProductSuggestedPriceVersionDto> GetAtDateAsync(Guid productId, DateTime date)
    {
        await _catalogValidator.ValidateProductExistsAsync(productId);
        var version = await _repository.FindAtDateAsync(productId, date);
        return _mapper.ToDto(EnsureFound(version, productId));
    }

    [Authorize(VPureLuxPermissions.Pricing.ProductSuggestedPrices.Create)]
    public async Task<ProductSuggestedPriceVersionDto> CreateAsync(
        Guid productId,
        CreateProductSuggestedPriceVersionDto input)
    {
        await _catalogValidator.ValidateActiveProductAsync(productId);
        var result = await _pricingManager.CreateProductSuggestedPriceVersionAsync(
            productId, input.Price, input.Reason, input.EffectiveFrom);

        if (result.ClosedVersion != null)
        {
            await _repository.UpdateAsync(result.ClosedVersion, autoSave: true);
        }

        await _repository.InsertAsync(result.NewVersion, autoSave: true);
        return _mapper.ToDto(result.NewVersion);
    }

    private static ProductSuggestedPriceVersion EnsureFound(
        ProductSuggestedPriceVersion? version,
        Guid productId)
    {
        if (version == null)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.PriceVersionNotFound)
                .WithData(nameof(productId), productId);
        }

        return version;
    }
}
