using System;

namespace VPureLux.Pricing.Events;

public sealed record ComponentSuggestedSellingPriceVersionClosedEvent(
    Guid PriceVersionId,
    Guid ComponentId,
    int VersionNo,
    DateTime EffectiveTo);
