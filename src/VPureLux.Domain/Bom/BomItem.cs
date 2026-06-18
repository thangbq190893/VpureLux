using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace VPureLux.Bom;

public class BomItem : Entity<Guid>
{
    public Guid ComponentId { get; private set; }
    public decimal Quantity { get; private set; }

    protected BomItem()
    {
    }

    internal BomItem(Guid id, Guid componentId, decimal quantity)
        : base(id)
    {
        if (componentId == Guid.Empty)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed)
                .WithData(nameof(ComponentId), componentId);
        }

        ComponentId = componentId;
        SetQuantity(quantity);
    }

    internal void UpdateQuantity(decimal quantity)
    {
        SetQuantity(quantity);
    }

    internal void Update(Guid componentId, decimal quantity)
    {
        if (componentId == Guid.Empty)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed)
                .WithData(nameof(ComponentId), componentId);
        }

        ComponentId = componentId;
        SetQuantity(quantity);
    }

    private void SetQuantity(decimal quantity)
    {
        if (quantity <= 0 || quantity != decimal.Truncate(quantity))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed)
                .WithData(nameof(Quantity), quantity);
        }

        Quantity = quantity;
    }
}
