using System.Collections.Generic;
using Volo.Abp;
using Volo.Abp.Domain.Values;

namespace VPureLux.Pricing;

public class PriceVersionNo : ValueObject
{
    public int Value { get; private set; }

    protected PriceVersionNo()
    {
    }

    public PriceVersionNo(int value)
    {
        if (value < PricingConsts.MinimumVersionNo)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed)
                .WithData(nameof(Value), value);
        }

        Value = value;
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }
}
