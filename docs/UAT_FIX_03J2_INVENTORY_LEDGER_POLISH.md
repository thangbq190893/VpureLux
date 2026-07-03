# UAT Fix 03J.2 - Inventory Ledger UI/Read-Model Polish

## Reason

03J.1 found that `Sổ kho` was a simple posted transaction list. It did not show enough trace detail for users to answer which Vật tư moved, why it moved, what source reference caused it, and how much quantity/value moved in or out.

## Scope

- Improve `/Inventory/Ledger` only.
- Keep the change UI/read-model focused.
- Do not change Inventory posting, FIFO allocation, cost calculation, Domain rules, database schema, migrations, or indexes.
- Do not redesign Inventory Adjustment in this batch.
- Keep backend identifiers such as `Component` and `StockItemType.Component` unchanged.

## Filters Added

- Warehouse.
- Stock item.
- Transaction type.
- Source/reference text.
- From date.
- To date.

Warehouse and stock item filtering still use the existing read service parameters. Transaction type, source/reference, and date range are applied in the Ledger PageModel against the existing posted transaction DTOs returned by the current query.

## Columns Added

The Ledger table now renders line-level trace rows instead of one row per transaction. Columns now include:

- Date/time.
- Warehouse.
- Vật tư.
- Transaction type.
- Source/reference.
- Quantity in.
- Quantity out.
- Unit cost.
- Amount.
- Reason.

Increase rows calculate amount from quantity times unit cost. Decrease rows calculate amount from existing FIFO allocation totals. No posting, FIFO, or stored cost behavior changed.

## Read-Model Changes

No Application contract or repository change was required in this batch. `LedgerModel` now flattens existing `InventoryTransactionDto.Lines` into a Web-layer `LedgerTraceRow` for display.

The current DTOs already provide:

- transaction posted time, warehouse, type, source reference fields, and reason;
- line stock item, direction, quantity, unit cost for increase rows;
- FIFO allocation unit cost and total cost for decrease rows.

## Deferred Fields

- Balance after: deferred because there is no persisted balance-after snapshot in the current read model/schema, and reconstructing it safely needs a separate design decision.
- User display: deferred because the current Inventory DTO/page does not expose a user label.
- Lot display for decrease/FIFO rows: deferred because allocations expose lot ids and cost data, but not user-friendly lot labels in the current Ledger row model.
- Separate note field: deferred because only `Reason` exists today.

## Tests Run

- `dotnet build VPureLux.slnx --no-restore -m:2` - passed with the existing Microsoft.NET.Test.Sdk generated entry-point warning.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Inventory" -m:1` - passed, 41/41.
- Repository grep for the legacy Vietnamese component wording - passed, no matches.

## Manual Smoke Checklist Deferred

- Open `/Inventory/Ledger`.
- Filter by warehouse and Vật tư.
- Filter by transaction type.
- Filter by date range.
- Filter by source/reference text.
- Verify receipt/adjustment increase rows show quantity in, unit cost, and amount.
- Verify issue/adjustment decrease rows show quantity out and FIFO-derived amount.
- Verify no balance-after or user column appears until those fields are designed.
