# Sales Module Implementation Specification

## Scope

Sales owns SalesOrders, transactional selling prices, immutable confirmation
snapshots, revenue, cost, profit, margin, and customer purchase history.

Sales does not own Customer master data, Pricing versions, BOM versions,
Inventory lots, FIFO allocation policy, or reusable Catalog bundles.

## Frozen Architecture

* `SalesOrder` is the aggregate root and owns `SalesOrderLine` and
  `SalesOrderBomSnapshotItem` children.
* Currency is VND only.
* Order numbers use `SO-YYYYMM-XXXXXX`.
* Order-number counters are stored in `AppNumberSequences` and incremented
  atomically. Implementations must not derive the next number using
  `MAX(OrderNo)`.
* Product lines reference an explicit Published BOM Version.
* Component lines issue the selected Component directly.
* Full component sets use Product plus Published BOM Version.
* Arbitrary reusable Bundles are deferred to a future Catalog extension.
* Draft orders may be edited. Confirmed and Cancelled orders are immutable.
* Phase 1 allows `Draft -> Confirmed` and `Draft -> Cancelled` only.
* Confirmed cancellation requires a future Inventory reversal workflow.

## Snapshot Policy

Confirmation creates immutable Customer, Catalog, Price, BOM, Inventory cost,
revenue, profit, and margin snapshots.

`SuggestedPriceSnapshot` is a default only. `ActualSellingPrice` is the source
of truth for revenue. When Suggested and Actual prices differ,
`OverrideReason` is required and stored with maximum length 500.

`CostAmountSnapshot` is the authoritative total FIFO issue cost.
`CostPriceSnapshot` is its weighted unit-cost representation.

## Confirmation Workflow

Sales validates active Customer, CustomerGroup, Warehouse, Catalog items, and
Published BOM Versions. Product lines expand into BOM Components. Inventory
performs FIFO allocation and returns actual issue cost and allocations. Sales
then records immutable snapshots and transitions to Confirmed atomically.

## Customer History

Sales reporting owns:

* Last Purchase Date
* Last Purchase Price
* Average Purchase Price
* Revenue
* Profit

Reports use Confirmed SalesOrderLine snapshots only.

## STEP 05 Persistence Requirements

* Prices and totals: `DECIMAL(18,2)`.
* Quantity: `DECIMAL(18,4)`.
* Margin: `DECIMAL(9,4)`.
* OverrideReason: `NVARCHAR(500)`.
* Unique OrderNo.
* Unique filtered ConfirmationIdempotencyKey.
* Restrict delete behavior for historical references.
* Concurrency protection for SalesOrder.

Infrastructure translates database exceptions into:

* `SALES_001` DuplicateOrderNo.
* `SALES_002` DuplicateConfirmationKey.
* `SALES_003` ConcurrentModification.
