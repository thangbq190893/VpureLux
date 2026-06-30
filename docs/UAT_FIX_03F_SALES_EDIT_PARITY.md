# UAT Fix 03F - Sales Edit Parity

## Reason

Sales Create already had multi-line guardrails for product context, suggested price display, row validation, and BOM-backed stock preview. Sales Edit still used a split table/card experience, which made existing lines and add-line behavior feel different from Create. This batch brings Edit closer to Create while keeping the existing Sales application and domain rules unchanged.

## Scope

- Web/UI/PageModel changes only.
- Sales Edit now uses a compact table-like line editor aligned with the Sales Create layout.
- Shared Sales product-context JavaScript now supports both Create and Edit pages.
- Sales Edit PageModel adds Web-layer validation and a stock availability JSON handler using the same BOM component availability shape as Create.
- Regression tests were added to `SalesPagesTests`.

## Files Changed

- `src/VPureLux.Web/Pages/Sales/Edit.cshtml`
- `src/VPureLux.Web/Pages/Sales/Edit.cshtml.cs`
- `src/VPureLux.Web/Pages/Sales/SalesProductContext.js`
- `src/VPureLux.Web/Pages/Sales/Create.css`
- `test/VPureLux.Web.Tests/Pages/SalesPagesTests.cs`
- `docs/UAT_FIX_03F_SALES_EDIT_PARITY.md`

## UI Behavior

- Existing Sales Edit lines render in a compact table with product, status/suggested price, stock availability target, quantity, actual price, override reason, save, and remove actions.
- Add-line now appears as a table row using the same row hooks as Create instead of a separate card.
- Existing Update/Add/Remove handlers remain postback-based.
- Edit reuses the Sales Create table CSS with small Edit-specific action-column adjustments.

## Validation Behavior

- Add-line no-BOM products receive a field-level `NewLine.ProductId` error with the existing product-stock limitation message.
- Add-line override reason validation maps to `NewLine.OverrideReason`.
- Update-line override reason validation maps to `UpdateLine.OverrideReason`.
- Missing suggested price with a manual actual price remains allowed when the product otherwise has a published BOM.
- Global stock validation messages are preserved when Web-layer availability validation blocks a line change.

## Stock Availability Behavior

- Edit exposes a stock availability preview endpoint for the current order warehouse.
- The client uses the same availability request/response shape as Create.
- BOM-backed products preview availability from BOM component stock.
- No-BOM products show the existing limitation and do not fake product-stock availability.
- Aggregate multi-line shortage checks use the same component-demand logic as Create.

## Intentionally Not Changed

- No Sales business rules were changed.
- No no-BOM product-stock selling was added.
- Product stock inventory remains disabled.
- No stock reservation was added.
- Confirm/FIFO/inventory posting was not changed.
- Domain, Application, EF, DB schema, migrations, and indexes were not changed.
- Backend identifiers such as `Component`, `ComponentId`, and `StockItemType.Component` were not renamed.

## Tests Run

- `dotnet build VPureLux.slnx --no-restore -m:2` - passed.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Sales" -m:1` - passed, 56 tests.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build -m:1` - passed, 169 tests.
- Requested legacy Vietnamese component-term grep across `src`, `test`, `docs`, and UI decision docs - no matches.

## Deferred Items

- Browser/E2E automation.
- Any future fully client-side Sales Edit dynamic-row flow beyond the current safe postback handlers.
- No-BOM product-stock selling.
- Product stock inventory enablement.
- Stock reservation.
