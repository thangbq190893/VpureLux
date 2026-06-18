using System;
using System.Collections.Generic;
using Volo.Abp;
using Volo.Abp.Domain.Values;

namespace VPureLux.Pricing;

public class EffectivePeriod : ValueObject
{
    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }

    protected EffectivePeriod()
    {
    }

    public EffectivePeriod(DateTime effectiveFrom, DateTime? effectiveTo = null)
    {
        if (effectiveTo.HasValue && effectiveTo.Value <= effectiveFrom)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.InvalidPriceEffectivePeriod)
                .WithData(nameof(effectiveFrom), effectiveFrom)
                .WithData(nameof(effectiveTo), effectiveTo);
        }

        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
    }

    public bool Contains(DateTime date)
    {
        return EffectiveFrom <= date && (!EffectiveTo.HasValue || date < EffectiveTo.Value);
    }

    public EffectivePeriod Close(DateTime effectiveTo)
    {
        return new EffectivePeriod(EffectiveFrom, effectiveTo);
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return EffectiveFrom;
        yield return EffectiveTo!;
    }
}
