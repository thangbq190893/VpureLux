using Riok.Mapperly.Abstractions;
using Volo.Abp.DependencyInjection;

namespace VPureLux.Pricing;

[Mapper]
public partial class PricingApplicationMapper : ITransientDependency
{
    public ComponentSuggestedSellingPriceVersionDto ToDto(ComponentSuggestedSellingPriceVersion source)
    {
        return new ComponentSuggestedSellingPriceVersionDto
        {
            Id = source.Id,
            ComponentId = source.ComponentId,
            VersionNo = source.VersionNo.Value,
            Price = source.Price.Amount,
            Currency = source.Price.Currency,
            Reason = source.Reason,
            EffectiveFrom = source.EffectivePeriod.EffectiveFrom,
            EffectiveTo = source.EffectivePeriod.EffectiveTo,
            Status = source.Status
        };
    }

    public ProductSuggestedPriceVersionDto ToDto(ProductSuggestedPriceVersion source)
    {
        return new ProductSuggestedPriceVersionDto
        {
            Id = source.Id,
            ProductId = source.ProductId,
            VersionNo = source.VersionNo.Value,
            Price = source.Price.Amount,
            Currency = source.Price.Currency,
            Reason = source.Reason,
            EffectiveFrom = source.EffectivePeriod.EffectiveFrom,
            EffectiveTo = source.EffectivePeriod.EffectiveTo,
            Status = source.Status
        };
    }
}
