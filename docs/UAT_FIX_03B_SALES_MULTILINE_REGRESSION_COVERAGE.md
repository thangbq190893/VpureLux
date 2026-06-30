# UAT Fix 03B - Sales Multi-Line Regression Coverage

## Reason

Sales Create now supports multiple order lines with per-line product context, pricing override validation, and stock availability preview. This batch adds regression coverage for that existing behavior before Sales Edit parity work, so later UI changes have a clear safety net.

## Scope

- Test-only coverage for Sales Create multi-line UI binding, dynamic row hooks, validation, stock preview, Details/Confirm behavior, and current Edit baseline behavior.
- No Sales business rule, persistence, inventory posting, schema, migration, or UI parity changes.

## Tests Added Or Updated

- Rendered Sales Create first-row binding names for `Input.Lines[0].*` and required row/product/price/override/stock hooks.
- Sales Create dynamic add/remove/reindex JavaScript source guards.
- Multi-line Sales Create POST persistence for multiple valid lines.
- Per-line override reason validation binding to the correct line index.
- Stock availability handler no-BOM response guard.
- Aggregate multi-line stock shortage blocking at Sales Create save.
- Client-side stock availability refresh guards for page load, warehouse change, product change, quantity change, add row, and remove row.
- Vietnamese Sales stock terminology guard.
- Sales Edit baseline rendering for multiple existing lines plus current Add/Update/Remove handlers.
- Sales Details draft estimated revenue across multiple lines.

## Protected Behaviors

- Sales Create remains a compact table/editor with a hidden dynamic row template.
- Create row fields bind as indexed `Input.Lines[i].*` values.
- Product context, stock availability, actual price, override reason, and remove-row hooks remain available per row.
- Multiple valid Sales Create lines are posted and persisted together.
- Suggested-price override reason validation remains per line.
- Missing suggested price plus manual actual price remains allowed when the line is otherwise valid.
- No-BOM products remain blocked for Sales Create and do not fake product-stock availability.
- Known component stock shortages block save, including aggregate multi-line shortages against shared components.
- Draft Details totals show estimated revenue while cost/profit remain clearly deferred until confirmation.
- Draft confirmation action and friendly error paths remain guarded by existing tests.
- Sales Edit can display multiple existing lines and keeps its current Add/Update/Remove baseline.

## Production Code Changed?

No.

## Terminology Note

Display text uses `Vật tư` where component terminology is needed. Backend identifiers remain unchanged, including `Component`, `ComponentId`, and `StockItemType.Component`.

## Tests Run

- `dotnet build VPureLux.slnx --no-restore -m:2` - passed.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Sales" -m:1` - passed, 51 tests.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build -m:1` - passed, 164 tests.
- Requested legacy Vietnamese component-term grep across `src`, `test`, `docs`, and the UI decision docs - no matches.

## Deferred

- Sales Edit parity with Create-like dynamic multi-line editing.
- No-BOM product-stock selling.
- Product stock inventory enablement.
- Stock reservation.
- Browser or E2E automation.
