using System;

namespace VPureLux.Pricing.Events;

public sealed record ProductSuggestedPriceVersionClosedEvent(
    Guid PriceVersionId,
    Guid ProductId,
    int VersionNo,
    DateTime EffectiveTo);
