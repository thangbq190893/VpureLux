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
public class ComponentSuggestedSellingPriceAppService
    : ApplicationService, IComponentSuggestedSellingPriceAppService
{
    private readonly IComponentSuggestedSellingPriceVersionRepository _repository;
    private readonly PricingManager _pricingManager;
    private readonly PricingCatalogValidator _catalogValidator;
    private readonly PricingApplicationMapper _mapper;

    public ComponentSuggestedSellingPriceAppService(
        IComponentSuggestedSellingPriceVersionRepository repository,
        PricingManager pricingManager,
        PricingCatalogValidator catalogValidator,
        PricingApplicationMapper mapper)
    {
        _repository = repository;
        _pricingManager = pricingManager;
        _catalogValidator = catalogValidator;
        _mapper = mapper;
    }

    [Authorize(VPureLuxPermissions.Pricing.ComponentSuggestedSellingPrices.History)]
    public async Task<List<ComponentSuggestedSellingPriceVersionDto>> GetHistoryAsync(Guid componentId)
    {
        await _catalogValidator.ValidateComponentExistsAsync(componentId);
        return (await _repository.GetHistoryAsync(componentId)).Select(_mapper.ToDto).ToList();
    }

    public async Task<ComponentSuggestedSellingPriceVersionDto> GetCurrentAsync(Guid componentId)
    {
        return await GetAtDateAsync(componentId, Clock.Now);
    }

    public async Task<ComponentSuggestedSellingPriceVersionDto> GetAtDateAsync(Guid componentId, DateTime date)
    {
        await _catalogValidator.ValidateComponentExistsAsync(componentId);
        var version = await _repository.FindAtDateAsync(componentId, date);
        return _mapper.ToDto(EnsureFound(version, componentId));
    }

    [Authorize(VPureLuxPermissions.Pricing.ComponentSuggestedSellingPrices.Create)]
    public async Task<ComponentSuggestedSellingPriceVersionDto> CreateAsync(
        Guid componentId,
        CreateComponentSuggestedSellingPriceVersionDto input)
    {
        await _catalogValidator.ValidateActiveComponentAsync(componentId);
        var result = await _pricingManager.CreateComponentSuggestedSellingPriceVersionAsync(
            componentId, input.Price, input.Reason, input.EffectiveFrom);

        if (result.ClosedVersion != null)
        {
            await _repository.UpdateAsync(result.ClosedVersion, autoSave: true);
        }

        await _repository.InsertAsync(result.NewVersion, autoSave: true);
        return _mapper.ToDto(result.NewVersion);
    }

    private static ComponentSuggestedSellingPriceVersion EnsureFound(
        ComponentSuggestedSellingPriceVersion? version,
        Guid componentId)
    {
        if (version == null)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.PriceVersionNotFound)
                .WithData(nameof(componentId), componentId);
        }

        return version;
    }
}
