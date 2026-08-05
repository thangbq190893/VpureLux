# UAT Fix - PageModel Lookup MaxResultCount Review

## Issue

Several Razor PageModels still used fixed low `MaxResultCount` values for preloaded lists and dropdown/select options. This meant users could lose access to records once master data exceeded the hard-coded cap.

## Root Cause

The list/table refactor removed the old `100` cap from converted DataTables pages, but other PageModels still had magic limits such as `100`, `500`, and `1000` for dropdown or preload data.

## Fix

- Replaced fixed PageModel caps with `LimitedResultRequestDto.MaxMaxResultCount`.
- Covered Customer Group dropdowns in Customer create/edit pages and modals.
- Covered BOM product/component option loading.
- Covered Sales customer, warehouse, product, history, and index preload queries.
- Covered Pricing, Audit, and Inventory Warehouse preload queries.
- Added a Web source guard test to fail if PageModels reintroduce `MaxResultCount = 100`, `199`, `500`, or `1000`.

## Intentionally Not Changed

- No database/schema/migration changes.
- No domain/application business rule changes.
- No dropdown UI framework changes in this patch.
- Very large datasets beyond ABP's max request guard should move to remote searchable dropdowns as a follow-up.

## Tests Run

- `dotnet build VPureLux.slnx --no-restore -m:2` - passed.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~PageModelSourceGuard|FullyQualifiedName~Catalog|FullyQualifiedName~Customer|FullyQualifiedName~Bom|FullyQualifiedName~Sales|FullyQualifiedName~Inventory|FullyQualifiedName~Pricing|FullyQualifiedName~Audit" -m:1` - passed, 248 tests.
- `git diff --check` - passed.
- Terminology grep returned existing audit/evidence references only.
