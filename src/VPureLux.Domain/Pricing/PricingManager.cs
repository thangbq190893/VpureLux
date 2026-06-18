using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Services;
using Volo.Abp.Guids;
using Volo.Abp.Timing;

namespace VPureLux.Pricing;

public class PricingManager : DomainService
{
    private readonly IComponentSuggestedSellingPriceVersionRepository _componentPriceRepository;
    private readonly IProductSuggestedPriceVersionRepository _productPriceRepository;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IClock _clock;

    public PricingManager(
        IComponentSuggestedSellingPriceVersionRepository componentPriceRepository,
        IProductSuggestedPriceVersionRepository productPriceRepository,
        IGuidGenerator guidGenerator,
        IClock clock)
    {
        _componentPriceRepository = componentPriceRepository;
        _productPriceRepository = productPriceRepository;
        _guidGenerator = guidGenerator;
        _clock = clock;
    }

    public async Task<(ComponentSuggestedSellingPriceVersion NewVersion, ComponentSuggestedSellingPriceVersion? ClosedVersion)>
        CreateComponentSuggestedSellingPriceVersionAsync(
            Guid componentId,
            decimal price,
            string reason,
            DateTime effectiveFrom)
    {
        EnsureNotBackdated(effectiveFrom);
        var active = await _componentPriceRepository.FindActiveAsync(componentId);
        active?.Close(effectiveFrom);

        var versionNo = await _componentPriceRepository.GetNextVersionNoAsync(componentId);
        var newVersion = new ComponentSuggestedSellingPriceVersion(
            _guidGenerator.Create(),
            componentId,
            new PriceVersionNo(versionNo),
            new Money(price),
            reason,
            effectiveFrom);

        return (newVersion, active);
    }

    public async Task<(ProductSuggestedPriceVersion NewVersion, ProductSuggestedPriceVersion? ClosedVersion)>
        CreateProductSuggestedPriceVersionAsync(
            Guid productId,
            decimal price,
            string reason,
            DateTime effectiveFrom)
    {
        EnsureNotBackdated(effectiveFrom);
        var active = await _productPriceRepository.FindActiveAsync(productId);
        active?.Close(effectiveFrom);

        var versionNo = await _productPriceRepository.GetNextVersionNoAsync(productId);
        var newVersion = new ProductSuggestedPriceVersion(
            _guidGenerator.Create(),
            productId,
            new PriceVersionNo(versionNo),
            new Money(price),
            reason,
            effectiveFrom);

        return (newVersion, active);
    }

    private void EnsureNotBackdated(DateTime effectiveFrom)
    {
        if (effectiveFrom.Date < _clock.Now.Date)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.BackdatedPriceVersionNotAllowed)
                .WithData(nameof(effectiveFrom), effectiveFrom);
        }
    }
}
