# UAT Fix - Pricing Index DataTables Pagination

## Issue

`/Pricing` still rendered pricing management lists directly in Razor with `foreach`, so large material/product lists were not paged server-side.

## Root Cause

The pricing index loaded active components with `MaxMaxResultCount`, then loaded all product pricing contexts before rendering rows in the page.

## Fix

- Converted `/Pricing` to two ABP DataTables:
  - `PricingComponentsTable`
  - `PricingProductsTable`
- Added server-side handlers:
  - `OnGetComponentListAsync`
  - `OnGetProductListAsync`
- Component pricing now pages active materials through `IComponentAppService.GetListAsync`.
- Product pricing now pages products through `IProductAppService.GetListAsync` and only calculates pricing context for the products on the current page.
- Added keyword search and clear buttons for both tabs.
- Preserved existing history links and BOM/current-price display behavior.

## Intentionally Not Changed

- No pricing business rule changes.
- No BOM costing changes.
- No sales/inventory behavior changes.
- No DB schema/migration changes.
- Product and component history pages remain unchanged.

## Tests Run

- `dotnet build VPureLux.slnx --no-restore -m:2` - passed.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Pricing" -m:1` - passed, 21/21.
- `dotnet test test/VPureLux.EntityFrameworkCore.Tests/VPureLux.EntityFrameworkCore.Tests.csproj --no-build --filter "FullyQualifiedName~Pricing" -m:1` - passed, 24/24.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Catalog" -m:1` - passed, 28/28.
- `dotnet test test/VPureLux.EntityFrameworkCore.Tests/VPureLux.EntityFrameworkCore.Tests.csproj --no-build --filter "FullyQualifiedName~Catalog" -m:1` - passed, 27/27.
- `git diff --check` - passed.
- Terminology grep for the legacy material wording across `src`, `test`, and `docs` - no new user-facing wording introduced.

## Manual Smoke Checklist

- Open `/Pricing`.
- Verify both tabs page with DataTables controls.
- Search component price rows by material code/name.
- Search product price rows by product code/name.
- Open history from a row.
