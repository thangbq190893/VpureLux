# UAT Fix - Inventory Lot Receipt History

## Issue

Users need a clear history of receipt quantity and receipt price for each lot of each stock item.

## Root Cause

The inventory lot page already showed lot number, stock item, receipt date, available quantity, and unit cost, but it did not clearly show the original received quantity or total receipt value per lot.

## Fix

- Renamed the lot page heading to `Lịch sử nhập theo lô`.
- Added receipt-history columns to the lot list:
  - `Số lượng nhập`
  - `Số lượng khả dụng`
  - `Đơn giá nhập thực tế`
  - `Giá trị nhập`
- Added a `Xem lịch sử nhập` action from current inventory balances to open `/Inventory/Lots` filtered by the selected warehouse and stock item.

## Data Source

The UI reads existing `InventoryLot` data:

- `ReceivedQuantity`
- `AvailableQuantity`
- `UnitCost`
- `ReceivedAt`
- `WarehouseId`
- `StockItemId`

No new table, migration, or posting behavior was introduced.

## Intentionally Not Changed

- No FIFO/costing changes.
- No inventory posting changes.
- No schema/migration changes.
- No changes to lot number auto-generation.
- No price rollup into BOM or pricing management yet.

## Tests Run

- `dotnet build VPureLux.slnx --no-restore -m:2` - passed.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Inventory" -m:1` - passed, 65/65.
- `dotnet test test/VPureLux.EntityFrameworkCore.Tests/VPureLux.EntityFrameworkCore.Tests.csproj --no-build --filter "FullyQualifiedName~Inventory" -m:1` - passed, 24/24.
- `git diff --check` - passed.
- Terminology grep for the legacy material wording across `src`, `test`, and `docs` - no new user-facing wording introduced.

## Manual Smoke Checklist

- Open `/Inventory/Balances`.
- Click `Xem lịch sử nhập` for an item.
- Confirm `/Inventory/Lots` is filtered by the selected warehouse and stock item.
- Confirm each lot shows receipt quantity, available quantity, unit cost, and receipt value.
