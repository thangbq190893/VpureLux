using System;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Pricing;

public class ComponentSuggestedSellingPriceVersionDto : EntityDto<Guid>
{
    public Guid ComponentId { get; set; }
    public int VersionNo { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = PricingConsts.Currency;
    public string Reason { get; set; } = string.Empty;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public PriceVersionStatus Status { get; set; }
}
