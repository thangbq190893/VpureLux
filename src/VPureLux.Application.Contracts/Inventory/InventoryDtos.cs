using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Inventory;

public class StockItemDto : EntityDto<Guid>
{
    public StockItemType ItemType { get; set; }
    public Guid CatalogItemId { get; set; }
    public string CodeSnapshot { get; set; } = string.Empty;
    public string NameSnapshot { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public bool IsInventoryEnabled { get; set; }
    public InventoryEntityStatus Status { get; set; }
}

public class WarehouseDto : EntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public InventoryEntityStatus Status { get; set; }
    public bool IsDefault { get; set; }
}

public class InventoryLotAllocationDto : EntityDto<Guid>
{
    public Guid InventoryLotId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
}

public class InventoryTransactionLineDto : EntityDto<Guid>
{
    public Guid StockItemId { get; set; }
    public InventoryMovementDirection Direction { get; set; }
    public decimal Quantity { get; set; }
    public string? LotNo { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public decimal? UnitCost { get; set; }
    public List<InventoryLotAllocationDto> Allocations { get; set; } = new();
}

public class InventoryTransactionDto : EntityDto<Guid>
{
    public Guid WarehouseId { get; set; }
    public InventoryTransactionType Type { get; set; }
    public InventoryTransactionStatus Status { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public Guid? BomVersionId { get; set; }
    public string? Reason { get; set; }
    public DateTime? PostedAt { get; set; }
    public decimal TotalIssueCost { get; set; }
    public List<InventoryTransactionLineDto> Lines { get; set; } = new();
}

public class InventoryBalanceDto
{
    public Guid WarehouseId { get; set; }
    public Guid StockItemId { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal InventoryValue { get; set; }
    public DateTime? LastMovementAt { get; set; }
}

public class InventoryLotDto : EntityDto<Guid>
{
    public string LotNo { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public Guid StockItemId { get; set; }
    public DateTime ReceivedAt { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public string Currency { get; set; } = string.Empty;
    public InventoryLotStatus Status { get; set; }
}

public class IssueCostResultDto
{
    public Guid InventoryTransactionId { get; set; }
    public decimal TotalIssueCost { get; set; }
    public decimal WeightedUnitCost { get; set; }
    public List<InventoryLotAllocationDto> Allocations { get; set; } = new();
}

public class GetInventoryListInput : PagedAndSortedResultRequestDto
{
    public string? SearchText { get; set; }
    public InventoryEntityStatus? Status { get; set; }
    public StockItemType? ItemType { get; set; }
    public bool? IsInventoryEnabled { get; set; }
}
