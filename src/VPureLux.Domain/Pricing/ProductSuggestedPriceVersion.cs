using System;
using VPureLux.Pricing.Events;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Pricing;

public class ProductSuggestedPriceVersion : FullAuditedAggregateRoot<Guid>
{
    public Guid ProductId { get; private set; }
    public PriceVersionNo VersionNo { get; private set; } = null!;
    public Money Price { get; private set; } = null!;
    public string Reason { get; private set; } = string.Empty;
    public EffectivePeriod EffectivePeriod { get; private set; } = null!;
    public PriceVersionStatus Status { get; private set; }

    protected ProductSuggestedPriceVersion()
    {
    }

    internal ProductSuggestedPriceVersion(
        Guid id,
        Guid productId,
        PriceVersionNo versionNo,
        Money price,
        string reason,
        DateTime effectiveFrom)
        : base(id)
    {
        if (productId == Guid.Empty)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed)
                .WithData(nameof(productId), productId);
        }

        ProductId = productId;
        VersionNo = Check.NotNull(versionNo, nameof(versionNo));
        Price = Check.NotNull(price, nameof(price));
        Reason = Check.NotNullOrWhiteSpace(reason, nameof(reason), PricingConsts.MaxReasonLength);
        EffectivePeriod = new EffectivePeriod(effectiveFrom);
        Status = PriceVersionStatus.Active;

        AddLocalEvent(new ProductSuggestedPriceVersionCreatedEvent(
            Id, ProductId, VersionNo.Value, EffectivePeriod.EffectiveFrom));
    }

    public void Close(DateTime effectiveTo)
    {
        if (Status == PriceVersionStatus.Closed)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.PriceVersionAlreadyClosed);
        }

        EffectivePeriod = EffectivePeriod.Close(effectiveTo);
        Status = PriceVersionStatus.Closed;
        AddLocalEvent(new ProductSuggestedPriceVersionClosedEvent(
            Id, ProductId, VersionNo.Value, effectiveTo));
    }
}
