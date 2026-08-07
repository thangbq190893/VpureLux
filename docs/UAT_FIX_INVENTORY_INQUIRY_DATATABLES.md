# UAT Fix - Inventory Inquiry DataTables

## Issue

Inventory inquiry pages still rendered full Razor lists instead of paginated DataTables:

- Sổ kho: `/Inventory/Ledger`
- Tổng kho hiện tại: `/Inventory/Balances`
- Lô hàng: `/Inventory/Lots`

With many inventory transactions, balances, or lots, these pages became hard to use and could render too much data into the initial HTML.

## Root Cause

- PageModels loaded `Items` / `Rows` during `OnGetAsync`.
- Razor pages rendered table bodies using `foreach`.
- Filtering and formatting were tied to server-rendered HTML instead of a paginated list handler.

## Fix

- Added DataTables list handlers:
  - `/Inventory/Ledger?handler=List`
  - `/Inventory/Balances?handler=List`
  - `/Inventory/Lots?handler=List`
- Added page scripts:
  - `/Pages/Inventory/Ledger.js`
  - `/Pages/Inventory/Balances.js`
  - `/Pages/Inventory/Lots.js`
- Kept existing warehouse/material filters.
- Kept Ledger type/date/source-reference filters.
- Moved row formatting into PageModel row DTOs returned as `PagedResultDto`.
- Kept Vietnamese money/date/quantity formatting.

## Intentionally Not Changed

- No inventory posting behavior changed.
- No FIFO/costing behavior changed.
- No database schema or migration changed.
- No LotNo generation changed.
- Dropdown lookup behavior was not converted to remote autocomplete in this patch.
- `Vật tư` terminology preserved; no `Linh kiện` wording introduced.

## Tests Run

- `dotnet build VPureLux.slnx --no-restore -m:2`
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Inventory" -m:1`

## Manual Smoke Checklist

Deferred/not run manually:

- Open `/Inventory/Balances`, apply warehouse/material filters, and page through results.
- Open `/Inventory/Lots`, apply warehouse/material filters, and page through results.
- Open `/Inventory/Ledger`, apply warehouse/material/type/date/source filters, and page through results.
- Confirm Ledger BOM source links still open BOM details.
