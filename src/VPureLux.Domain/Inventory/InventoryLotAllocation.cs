using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace VPureLux.Inventory;

public class InventoryLotAllocation : Entity<Guid>
{
    public Guid InventoryLotId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal TotalCost => Quantity * UnitCost;

    protected InventoryLotAllocation()
    {
    }

    internal InventoryLotAllocation(Guid id, Guid inventoryLotId, decimal quantity, decimal unitCost)
        : base(id)
    {
        InventoryLotId = inventoryLotId;
        Quantity = EnsurePositive(quantity, InventoryConsts.QuantityScale);
        UnitCost = EnsurePositive(unitCost, InventoryConsts.CostScale);
    }

    private static decimal EnsurePositive(decimal value, int scale)
    {
        value = decimal.Round(value, scale, MidpointRounding.AwayFromZero);
        if (value <= 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        return value;
    }
}
