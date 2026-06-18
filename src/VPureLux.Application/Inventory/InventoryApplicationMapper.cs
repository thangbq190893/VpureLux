using System.Linq;
using Volo.Abp.DependencyInjection;

namespace VPureLux.Inventory;

public class InventoryApplicationMapper : ITransientDependency
{
    public StockItemDto ToDto(StockItem item) => new()
    {
        Id = item.Id,
        ItemType = item.ItemType,
        CatalogItemId = item.CatalogItemId,
        CodeSnapshot = item.CodeSnapshot,
        NameSnapshot = item.NameSnapshot,
        Unit = item.Unit,
        IsInventoryEnabled = item.IsInventoryEnabled,
        Status = item.Status
    };

    public WarehouseDto ToDto(Warehouse warehouse) => new()
    {
        Id = warehouse.Id,
        Code = warehouse.Code,
        Name = warehouse.Name,
        Address = warehouse.Address,
        Status = warehouse.Status,
        IsDefault = warehouse.IsDefault
    };

    public InventoryTransactionDto ToDto(InventoryTransaction transaction) => new()
    {
        Id = transaction.Id,
        WarehouseId = transaction.WarehouseId,
        Type = transaction.Type,
        Status = transaction.Status,
        IdempotencyKey = transaction.IdempotencyKey,
        RequestHash = transaction.RequestHash,
        ReferenceType = transaction.ReferenceType,
        ReferenceId = transaction.ReferenceId,
        BomVersionId = transaction.BomVersionId,
        Reason = transaction.Reason,
        PostedAt = transaction.PostedAt,
        TotalIssueCost = transaction.TotalIssueCost,
        Lines = transaction.Lines.Select(line => new InventoryTransactionLineDto
        {
            Id = line.Id,
            StockItemId = line.StockItemId,
            Direction = line.Direction,
            Quantity = line.Quantity,
            LotNo = line.LotNo,
            ReceivedAt = line.ReceivedAt,
            UnitCost = line.UnitCost,
            Allocations = line.Allocations.Select(ToDto).ToList()
        }).ToList()
    };

    public InventoryLotAllocationDto ToDto(InventoryLotAllocation allocation) => new()
    {
        Id = allocation.Id,
        InventoryLotId = allocation.InventoryLotId,
        Quantity = allocation.Quantity,
        UnitCost = allocation.UnitCost,
        TotalCost = allocation.TotalCost
    };

    public InventoryBalanceDto ToDto(InventoryBalance balance) => new()
    {
        WarehouseId = balance.WarehouseId,
        StockItemId = balance.StockItemId,
        QuantityOnHand = balance.QuantityOnHand,
        InventoryValue = balance.InventoryValue,
        LastMovementAt = balance.LastMovementAt
    };

    public InventoryLotDto ToDto(InventoryLot lot) => new()
    {
        Id = lot.Id,
        LotNo = lot.LotNo,
        WarehouseId = lot.WarehouseId,
        StockItemId = lot.StockItemId,
        ReceivedAt = lot.ReceivedAt,
        ReceivedQuantity = lot.ReceivedQuantity,
        AvailableQuantity = lot.AvailableQuantity,
        UnitCost = lot.UnitCost,
        Currency = lot.Currency,
        Status = lot.Status
    };
}
