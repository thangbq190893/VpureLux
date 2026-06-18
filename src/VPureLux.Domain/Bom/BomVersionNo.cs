using System.Collections.Generic;
using Volo.Abp;
using Volo.Abp.Domain.Values;

namespace VPureLux.Bom;

public class BomVersionNo : ValueObject
{
    public int Value { get; private set; }

    protected BomVersionNo()
    {
    }

    public BomVersionNo(int value)
    {
        if (value < BomConsts.MinimumVersionNo)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed)
                .WithData(nameof(Value), value);
        }

        Value = value;
    }

    public BomVersionNo Next()
    {
        return new BomVersionNo(checked(Value + 1));
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
