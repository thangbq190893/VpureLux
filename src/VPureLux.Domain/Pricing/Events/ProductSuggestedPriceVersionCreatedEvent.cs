using System;

namespace VPureLux.Pricing.Events;

public sealed record ProductSuggestedPriceVersionCreatedEvent(
    Guid PriceVersionId,
    Guid ProductId,
    int VersionNo,
    DateTime EffectiveFrom);
