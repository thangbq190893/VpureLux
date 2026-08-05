# UAT Fix - Review List Pages Similar To Components

## Issue

The Components page was fixed to avoid the old bounded Razor list pattern that loaded a small fixed page and rendered row actions directly in the view. After confirming Components works, similar CRUD list pages were reviewed for the same design risk.

## Root Cause

Products, Customers, and Customer Groups still used page-load list data with Razor-rendered rows and a fixed initial result size. This kept paging/search/action rendering inconsistent with the ABP DataTables pattern now used by Components.

## Fix

- Converted Products to ABP DataTables with a Razor `List` handler.
- Converted Customers to ABP DataTables with a Razor `List` handler.
- Converted Customer Groups to ABP DataTables with a Razor `List` handler.
- Added row-action record normalization so ABP row action callbacks work whether the callback receives the record directly or a wrapped `{ record }` payload.
- Added server-side sorting support and default newest-first sorting for the converted lists.
- Preserved modal create/edit/details flows and status actions.

## Intentionally Not Changed

- No database schema or migration changes.
- No domain rule changes.
- No pricing, inventory, BOM, sales, or audit behavior changes.
- Create/edit form select controls were not refactored because they are not the same list-table pattern.

## Validation Behavior

The converted pages now load table rows through server-side DataTables handlers and reload the current table page after modal saves or status changes.

## Tests

Focused Web tests were updated to assert the DataTables shell, handler-based loading, row-action normalization, and no old bounded Razor row rendering for these pages.

- `dotnet build VPureLux.slnx --no-restore -m:2` - passed.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Catalog|FullyQualifiedName~Customer|FullyQualifiedName~Product|FullyQualifiedName~Component" -m:1` - passed, 101 tests.
- `dotnet test test/VPureLux.EntityFrameworkCore.Tests/VPureLux.EntityFrameworkCore.Tests.csproj --no-build --filter "FullyQualifiedName~Catalog|FullyQualifiedName~Customer" -m:1` - passed, 41 tests.
- `dotnet test test/VPureLux.Application.Tests/VPureLux.Application.Tests.csproj --no-build --filter "FullyQualifiedName~Catalog|FullyQualifiedName~Customer" -m:1` - passed, 18 tests.
- `git diff --check` - passed.
- `git grep -n -i "linh kiện" -- src test docs BUSINESS_ARCHITECTURE_DECISIONS_V2.md UI_IMPLEMENTATION_DECISION_MATRIX.md UI_REFACTOR_SOURCE_OF_TRUTH.md UI_UX_ABP_GUIDE_V2.md` - returned existing audit/evidence references and prior fix-doc terminology-check text only.

Manual browser smoke for the converted pages is deferred.
