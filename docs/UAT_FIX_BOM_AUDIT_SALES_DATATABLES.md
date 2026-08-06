# UAT Fix - BOM, Audit, Sales Server-side DataTables

## Issue

User review found the BOM index, business audit log, and sales order list still behaved like fixed-size Razor-rendered lists instead of paginated DataTables.

## Root Cause

- BOM index loaded product summaries into `Model.Rows` and rendered all available rows in Razor.
- Audit index loaded `Model.Logs` and rendered the visible list in Razor.
- Sales index loaded `Model.Orders` and rendered the visible list in Razor.
- Sales history also loaded confirmed orders as one Razor-rendered list.
- These pages did not expose DataTables list handlers, so paging depended on PageModel list loading rather than DataTables `skipCount` / `maxResultCount`.

## Fix

- Added server-side list handlers for:
  - `/Bom?handler=List`
  - `/Audit?handler=List`
  - `/Sales?handler=List`
  - `/Sales/History?handler=List`
- Added page scripts using ABP DataTables with `serverSide: true`.
- Kept existing filters and routed them through DataTables AJAX extra parameters.
- Kept existing permissions for create/history/export actions.
- Moved display formatting needed by DataTables into compact row DTOs returned by the PageModels.

## Validation Behavior

- BOM index now pages products through the product application service and computes BOM summary data only for the current DataTables page.
- Audit index now pages audit logs through the audit application service and returns localized action/severity/status labels.
- Sales index now pages sales orders through the sales application service and returns localized statuses and Vietnamese date/money formatting.
- Sales history now pages confirmed sales orders through the sales application service and preserves the profit-column permission check.

## Intentionally Not Changed

- No domain rules changed.
- No application service business behavior changed.
- No database schema or migration changed.
- No BOM versioning/publish behavior changed.
- No sales posting/payment behavior changed.
- No audit persistence behavior changed.
- Existing form/dropdown lookup pages were not converted to remote autocomplete in this patch.
- `Vật tư` terminology preserved; no `Linh kiện` wording introduced.

## Tests Run

- `dotnet build VPureLux.slnx --no-restore -m:2`
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Bom|FullyQualifiedName~Audit|FullyQualifiedName~Sales" -m:1`

## Manual Smoke Checklist

Deferred/not run manually:

- Open `/Bom`, confirm DataTables pager is visible and can move beyond the first page.
- Search BOM products and confirm row actions still open history/create/current version.
- Open `/Audit`, filter by module/entity/severity/correlation id, and page through results.
- Open `/Sales`, filter payment status and page through orders.
- Confirm no duplicate select controls appear on the affected pages.
