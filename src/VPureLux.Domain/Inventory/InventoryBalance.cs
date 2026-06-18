using System;
using System.Collections.Generic;

namespace VPureLux.Inventory;

public class InventoryBalance
{
    public Guid WarehouseId { get; private set; }
    public Guid StockItemId { get; private set; }
    public decimal QuantityOnHand { get; private set; }
    public decimal InventoryValue { get; private set; }
    public DateTime? LastMovementAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    protected InventoryBalance()
    {
    }

    public InventoryBalance(Guid warehouseId, Guid stockItemId)
    {
        WarehouseId = warehouseId;
        StockItemId = stockItemId;
    }

    public void ApplyMovement(decimal quantityDelta, decimal valueDelta, DateTime movementAt)
    {
        var newQuantity = QuantityOnHand + quantityDelta;
        var newValue = InventoryValue + valueDelta;
        if (newQuantity < 0 || newValue < 0)
        {
            throw new Volo.Abp.BusinessException(VPureLuxDomainErrorCodes.InsufficientInventory);
        }

        QuantityOnHand = decimal.Round(newQuantity, InventoryConsts.QuantityScale, MidpointRounding.AwayFromZero);
        InventoryValue = decimal.Round(newValue, InventoryConsts.CostScale, MidpointRounding.AwayFromZero);
        LastMovementAt = movementAt;
    }
}

public sealed record FifoAllocation(Guid InventoryLotId, decimal Quantity, decimal UnitCost)
{
    public decimal TotalCost => Quantity * UnitCost;
}

public sealed record IssueCostResult(
    Guid InventoryTransactionId,
    decimal TotalIssueCost,
    decimal WeightedUnitCost,
    IReadOnlyList<FifoAllocation> Allocations);
