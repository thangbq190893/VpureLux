# UAT Fix 03H - Inventory Issue/Adjustment Line Editor Polish

## Reason

UAT Fix 03G made BOM Edit and Inventory Receipt line editors compact and prevented duplicated dynamic-row select controls. Inventory Issue and Inventory Adjustment still used the older bordered row-card layout, which made the posting pages visually inconsistent and left the same duplicate-select risk harder to guard during add/remove/reindex flows.

## Scope

- Polish `/Inventory/Issue` line rows.
- Polish `/Inventory/Adjustment` decrease and increase line rows.
- Reuse `LineEditors.css`, `DynamicRowSelects.js`, and the existing inventory `Posting.js` dynamic row behavior.
- Add focused Web regression coverage for compact layout, one stock item select per live row, hidden template safeguards, add/remove/reindex hooks, and terminology.

## Files Changed

- `src/VPureLux.Web/Pages/Inventory/Issue.cshtml`
- `src/VPureLux.Web/Pages/Inventory/Adjustment.cshtml`
- `src/VPureLux.Web/Pages/Shared/LineEditors.css`
- `test/VPureLux.Web.Tests/Pages/InventoryPagesTests.cs`
- `docs/UAT_FIX_03H_INVENTORY_ISSUE_ADJUSTMENT_LINE_EDITOR_POLISH.md`

## Issue UI Behavior

- Issue lines now render in a compact table using the shared line-editor styles.
- Each live row has one stock item select, one quantity input, and one aligned icon remove action.
- Add/remove/reindex continues to use `Posting.js`.
- Hidden dynamic row templates are created by `DynamicRowSelects.js`, hidden, and disabled so template controls are excluded from posting.
- Cloned rows are cleaned and re-enabled before use.

## Adjustment UI Behavior

- Decrease lines now render in a compact table with stock item, quantity, and remove action columns.
- Increase lines now render in a compact table with stock item, quantity, lot, received date, unit cost, and remove action columns.
- Existing adjustment type switching remains responsible for enabling the active section and disabling the inactive section.
- Add/remove/reindex continues to use the existing inventory posting script and data-template attributes.
- Dynamic row templates remain hidden and disabled, while cloned rows are re-enabled.

## Intentionally Not Changed

- No Inventory business rules changed.
- No FIFO, lot allocation, or posting behavior changed.
- No Domain, Application, database schema, migrations, or indexes changed.
- No backend identifiers were renamed, including `Component` and `StockItemType.Component`.
- No Sales or BOM behavior changed.
- Vietnamese terminology remains `Vật tư`; the legacy component wording was not introduced.

## Tests Run

- `dotnet build VPureLux.slnx --no-restore -m:2` - passed.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Inventory" -m:1` - passed, 37 tests.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build -m:1` - passed, 177 tests.
- Repository-wide grep for the legacy Vietnamese component wording - no matches.

## Manual Smoke Checklist

Deferred/not run:

- Issue: add 3 rows, verify compact layout, no duplicate select, submit valid issue if stock exists.
- Adjustment: add increase/decrease rows, verify compact layout, no duplicate select, submit valid adjustment if data exists.
