using System;

namespace VPureLux.Pricing;

public class ProductPricingContextDto
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public bool HasPublishedBom { get; set; }
    public bool HasMissingComponentSuggestedPrices { get; set; }
    public decimal? ComponentBuildPrice { get; set; }
    public decimal? CurrentProductSuggestedPrice { get; set; }
    public decimal? Difference { get; set; }
}
