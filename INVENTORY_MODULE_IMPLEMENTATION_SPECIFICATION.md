# Inventory Module Implementation Specification

## Scope

Inventory owns StockItems, Warehouses, InventoryLots, immutable posted
InventoryTransactions, FIFO allocations, inventory ledger facts, and
InventoryBalance read-model contracts.

Inventory does not own selling price, revenue, profit, margin, BOM structure,
or Pricing versions.

## Frozen Architecture

* Use Generic StockItem architecture.
* Component StockItems are inventory-enabled in phase 1.
* Product StockItems are structurally supported but inventory-disabled in
  phase 1.
* InventoryTransactions are the source of truth.
* InventoryBalance is a rebuildable read model only.
* Inventory costing is FIFO by lot.
* FIFO order is `ReceivedAt ASC, CreationTime ASC, Id ASC`.
* All posting operations are atomic and idempotent.

## Aggregate Roots

* `StockItem`
* `Warehouse`
* `InventoryLot`
* `InventoryTransaction`

`InventoryTransaction` owns `InventoryTransactionLine` and
`InventoryLotAllocation`.

## Core Rules

* Warehouse Code is immutable after creation.
* InventoryLot LotNo is immutable after receipt posting.
* Posted transactions are immutable.
* Stock and lot availability cannot become negative.
* Inactive Warehouses and StockItems cannot be used.
* Inventory-disabled StockItems cannot be used.
* Adjustments require a Reason with maximum length 500.
* Issue operations return actual FIFO issue cost and allocations.
* No phase 1 Product inventory receipt or issue is permitted.

## Workflows

### Receipt

Create and post a receipt transaction, create immutable-cost lots, generate
positive ledger facts, and update InventoryBalance projections.

### Issue

Validate available quantity, allocate oldest lots first, post a transaction,
generate negative ledger facts, update InventoryBalance projections, and
return actual issue cost.

### Adjustment

Increase creates a new lot. Decrease consumes existing lots using FIFO. Both
require a Reason.

## Module Boundaries

* Catalog creation/deactivation events synchronize StockItems.
* Sales orchestrates Product BOM expansion and calls Inventory with Component
  requirements.
* BOM remains the owner of immutable BOM versions.
* Pricing suggested selling prices are sales references only and must not be
  used for receipt data entry or inventory valuation.
* Receipt `UnitCost` is the actual lot input cost and remains the source for
  FIFO issue cost.
* Sales copies Inventory issue cost into its immutable CostPriceSnapshot.

## STEP 05 Persistence Requirements

* Quantity precision: `DECIMAL(18,4)`.
* Cost precision: `DECIMAL(18,2)`.
* Adjustment Reason: `NVARCHAR(500)`.
* Restrict delete behavior for historical references.
* Unique StockItem `(ItemType, CatalogItemId)`.
* Unique Warehouse Code.
* Unique Lot `(WarehouseId, StockItemId, LotNo)`.
* Unique InventoryBalance `(WarehouseId, StockItemId)`.
* Unique InventoryTransaction IdempotencyKey.
* Database constraints prevent negative lot quantities.
* Concurrency tokens protect InventoryLot and InventoryBalance.
