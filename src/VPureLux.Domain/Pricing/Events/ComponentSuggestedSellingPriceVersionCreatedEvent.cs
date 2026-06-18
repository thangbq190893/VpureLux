using System;

namespace VPureLux.Pricing.Events;

public sealed record ComponentSuggestedSellingPriceVersionCreatedEvent(
    Guid PriceVersionId,
    Guid ComponentId,
    int VersionNo,
    DateTime EffectiveFrom);
