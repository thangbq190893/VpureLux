using System;
using VPureLux.Inventory.Events;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Inventory;

public class InventoryLot : FullAuditedAggregateRoot<Guid>
{
    public string LotNo { get; private set; } = string.Empty;
    public Guid WarehouseId { get; private set; }
    public Guid StockItemId { get; private set; }
    public Guid ReceiptTransactionLineId { get; private set; }
    public DateTime ReceivedAt { get; private set; }
    public decimal ReceivedQuantity { get; private set; }
    public decimal AvailableQuantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public string Currency { get; private set; } = InventoryConsts.Currency;
    public InventoryLotStatus Status { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    protected InventoryLot()
    {
    }

    internal InventoryLot(
        Guid id,
        string lotNo,
        Guid warehouseId,
        Guid stockItemId,
        Guid receiptTransactionLineId,
        DateTime receivedAt,
        decimal quantity,
        decimal unitCost)
        : base(id)
    {
        LotNo = Check.NotNullOrWhiteSpace(lotNo, nameof(lotNo), InventoryConsts.MaxLotNoLength);
        WarehouseId = warehouseId;
        StockItemId = stockItemId;
        ReceiptTransactionLineId = receiptTransactionLineId;
        ReceivedAt = receivedAt;
        ReceivedQuantity = EnsurePositive(quantity, nameof(quantity), InventoryConsts.QuantityScale);
        AvailableQuantity = ReceivedQuantity;
        UnitCost = EnsurePositive(unitCost, nameof(unitCost), InventoryConsts.CostScale);
        Status = InventoryLotStatus.Available;
    }

    public void Allocate(decimal quantity)
    {
        quantity = EnsurePositive(quantity, nameof(quantity), InventoryConsts.QuantityScale);
        if (quantity > AvailableQuantity)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.InsufficientInventory)
                .WithData(nameof(Id), Id)
                .WithData(nameof(AvailableQuantity), AvailableQuantity);
        }

        AvailableQuantity -= quantity;
        if (AvailableQuantity == 0)
        {
            Status = InventoryLotStatus.Depleted;
            AddLocalEvent(new InventoryLotDepletedEvent(Id, WarehouseId, StockItemId));
        }
    }

    private static decimal EnsurePositive(decimal value, string name, int scale)
    {
        value = decimal.Round(value, scale, MidpointRounding.AwayFromZero);
        if (value <= 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed).WithData(name, value);
        }

        return value;
    }
}
