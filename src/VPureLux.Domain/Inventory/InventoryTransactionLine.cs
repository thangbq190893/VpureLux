using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace VPureLux.Inventory;

public class InventoryTransactionLine : Entity<Guid>
{
    private readonly List<InventoryLotAllocation> _allocations = new();

    public Guid StockItemId { get; private set; }
    public InventoryMovementDirection Direction { get; private set; }
    public decimal Quantity { get; private set; }
    public string? LotNo { get; private set; }
    public DateTime? ReceivedAt { get; private set; }
    public decimal? UnitCost { get; private set; }
    public IReadOnlyCollection<InventoryLotAllocation> Allocations => _allocations.AsReadOnly();
    public decimal TotalIssueCost => _allocations.Sum(x => x.TotalCost);

    protected InventoryTransactionLine()
    {
    }

    internal InventoryTransactionLine(
        Guid id,
        Guid stockItemId,
        InventoryMovementDirection direction,
        decimal quantity,
        string? lotNo = null,
        DateTime? receivedAt = null,
        decimal? unitCost = null)
        : base(id)
    {
        quantity = decimal.Round(quantity, InventoryConsts.QuantityScale, MidpointRounding.AwayFromZero);
        if (stockItemId == Guid.Empty || quantity <= 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        StockItemId = stockItemId;
        Direction = direction;
        Quantity = quantity;
        LotNo = lotNo;
        ReceivedAt = receivedAt;
        UnitCost = unitCost.HasValue
            ? decimal.Round(unitCost.Value, InventoryConsts.CostScale, MidpointRounding.AwayFromZero)
            : null;
    }

    internal void AddAllocation(Guid id, Guid lotId, decimal quantity, decimal unitCost)
    {
        _allocations.Add(new InventoryLotAllocation(id, lotId, quantity, unitCost));
    }
}
