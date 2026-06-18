using System;
using System.ComponentModel.DataAnnotations;

namespace VPureLux.Pricing;

public class CreateComponentSuggestedSellingPriceVersionDto
{
    [Range(
        typeof(decimal),
        "0.01",
        "9999999999999999.99",
        ParseLimitsInInvariantCulture = true)]
    public decimal Price { get; set; }

    [Required]
    [StringLength(PricingConsts.MaxReasonLength)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    public DateTime EffectiveFrom { get; set; }
}
