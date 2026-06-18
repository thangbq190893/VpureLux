using System;

namespace VPureLux.Inventory.Events;

public sealed record StockItemCreatedEvent(Guid StockItemId, StockItemType ItemType, Guid CatalogItemId);
public sealed record StockItemDeactivatedEvent(Guid StockItemId, StockItemType ItemType, Guid CatalogItemId);
public sealed record WarehouseCreatedEvent(Guid WarehouseId, string Code);
public sealed record InventoryReceiptPostedEvent(Guid TransactionId, Guid WarehouseId, DateTime PostedAt);
public sealed record InventoryIssuePostedEvent(Guid TransactionId, Guid WarehouseId, decimal TotalIssueCost, DateTime PostedAt);
public sealed record InventoryAdjustedEvent(Guid TransactionId, Guid WarehouseId, InventoryTransactionType Type, string Reason);
public sealed record InventoryLotDepletedEvent(Guid LotId, Guid WarehouseId, Guid StockItemId);
