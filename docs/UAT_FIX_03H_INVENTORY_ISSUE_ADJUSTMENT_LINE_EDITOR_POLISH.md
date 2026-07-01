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

## 03H.1 Shared Dropdown Clipping Follow-up

### Bug

Real browser UAT still showed compact line editor dropdowns clipped inside the table area. Confirmed affected examples were `/Inventory/Receipt` stock item rows and BOM create/edit material rows. The action column could also look too narrow and create an internal scroll/overflow feel.

### CSS/root cause

The line editor wrapper still inherited Bootstrap `table-responsive` overflow strongly enough to act like a clipping scroll container in the browser. Inventory live row selects were reinitialized through the shared dynamic-row helper, but BOM Create/Edit still opted out of that path with `data-dynamic-select2="disabled"`, leaving material dropdowns inside the compact table boundary.

### Fix summary

- `LineEditors.css` now makes `.vpl-line-editor` and `.vpl-line-editor.table-responsive` non-clipping with `overflow: visible !important`.
- The shared action column is slightly wider, nowrap, and explicitly visible to avoid internal vertical scroll effects.
- `Posting.js` reinitializes only live inventory line rows after creating the disabled hidden template, preserving add/remove/reindex behavior and excluding template controls from post.
- BOM Create/Edit now use the shared Select2/body-dropdown path instead of opting out, while hidden templates remain disabled/excluded from post.
- Receipt, Issue, Adjustment, BOM Create, and BOM Edit continue to use the same compact line editor hooks.

### Manual smoke checklist

- Receipt: open `/Inventory/Receipt`, select warehouse, open the first stock item dropdown, verify it is not clipped - passed.
- Receipt: add 3 rows and verify each stock item dropdown opens cleanly - passed.
- Receipt: verify action buttons align with row inputs and no internal vertical scrollbar appears in the action column/table - passed.
- BOM Create: open the material dropdown, verify it is not clipped and the action column has no internal scrollbar - blocked by local sign-in gate during final 03H.1 smoke.
- BOM Edit: verify no duplicate select and the material dropdown opens fully - blocked by local sign-in gate during final 03H.1 smoke.
- Issue: quick check for no obvious compact layout regression - passed.
- Adjustment: quick check for no obvious compact layout regression - passed.

### 03H.1 validation

- `dotnet build VPureLux.slnx --no-restore -m:2` - passed with the existing Microsoft.NET.Test.Sdk generated entry-point warning.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Bom|FullyQualifiedName~Inventory" -m:1` - passed, 63/63.
- Repository grep for the legacy Vietnamese component wording - passed, no matches.
- Browser smoke against `https://localhost:44325` was attempted but could not reach authenticated pages: the host admin row exists, is active and not locked, and its stored hash matches the documented dev password, but the running login page still rejected both username and email sign-in with `Invalid username or password!`. No database state was changed during this check.
