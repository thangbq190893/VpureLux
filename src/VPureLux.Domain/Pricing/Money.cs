using System;
using System.Collections.Generic;
using Volo.Abp;
using Volo.Abp.Domain.Values;

namespace VPureLux.Pricing;

public class Money : ValueObject
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = PricingConsts.Currency;

    protected Money()
    {
    }

    public Money(decimal amount)
    {
        amount = decimal.Round(amount, PricingConsts.PriceScale, MidpointRounding.AwayFromZero);
        if (amount <= 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.PriceMustBeGreaterThanZero)
                .WithData(nameof(Amount), amount);
        }

        Amount = amount;
        Currency = PricingConsts.Currency;
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Amount;
        yield return Currency;
    }
}
