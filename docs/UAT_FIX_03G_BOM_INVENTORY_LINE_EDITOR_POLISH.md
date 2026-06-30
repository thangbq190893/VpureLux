# UAT Fix 03G - BOM/Inventory Line Editor Polish

## Reason

UAT found that BOM Edit could surface a raw concurrency exception on a normal save after line changes. The BOM Edit and Inventory Receipt line editors were also visually tall and could leave duplicated select controls from dynamic row templates.

## Scope

- BOM Edit save handling and friendly concurrency feedback.
- BOM Edit compact multi-line editor layout.
- Inventory Receipt compact multi-line editor layout.
- Shared dynamic row select template behavior.
- Regression tests for layout, duplicate select prevention, add/remove/reindex hooks, save behavior, and terminology.

## Files Changed

- `src/VPureLux.Web/Pages/Bom/Edit.cshtml`
- `src/VPureLux.Web/Pages/Bom/Edit.cshtml.cs`
- `src/VPureLux.Web/Pages/Bom/BomItems.js`
- `src/VPureLux.Web/Pages/Inventory/Receipt.cshtml`
- `src/VPureLux.Web/Pages/Shared/DynamicRowSelects.js`
- `src/VPureLux.Web/Pages/Shared/LineEditors.css`
- `src/VPureLux.Domain.Shared/Localization/VPureLux/vi-VN.json`
- `src/VPureLux.EntityFrameworkCore/Bom/EfCoreBomVersionRepository.cs`
- `test/VPureLux.Web.Tests/Pages/BomPagesTests.cs`
- `test/VPureLux.Web.Tests/Pages/InventoryPagesTests.cs`

## Root Cause

The BOM save path can receive a tracked aggregate whose newly added BOM item rows are interpreted as modified rows. EF then attempts to update rows that do not exist yet, which raises a concurrency exception even on a normal save. Separately, hidden dynamic row templates kept enabled controls, so template select elements could participate in form state and appear as duplicate row controls.

## BOM Behavior

- BOM Edit now renders rows in a compact table-like editor.
- Each row has one material select, one quantity input, and one aligned remove button.
- Add/remove/reindex hooks remain in `BomItems.js`.
- Normal draft save with added/updated rows no longer raises a false concurrency failure.
- Real concurrency conflicts return a friendly validation message instead of a raw exception page.

## Inventory Behavior

- Inventory Receipt now renders rows in a compact table-like editor.
- Each row has one stock item select and aligned quantity, lot, date, cost, and remove controls.
- Add/remove/reindex hooks remain in `Posting.js` and shared dynamic row helpers.
- Hidden dynamic templates have disabled controls and clones are re-enabled before use.

## Production Code Changed?

Yes. Web UI/PageModel code changed, a shared Web CSS file was added, Vietnamese localization was extended, and the BOM EF repository update path was fixed to prevent false concurrency failures for new BOM item rows.

## Domain/Application/DB Changed?

No Domain model, Application service, database schema, migration, or business rule changes were made. The only non-Web code change is an EntityFrameworkCore repository state-handling fix for the existing BOM aggregate persistence behavior.

## Terminology

Display text uses `Vật tư`. Backend identifiers such as `Component`, `ComponentId`, and `StockItemType.Component` remain unchanged.

## Tests Added/Updated

- BOM Edit compact multi-line layout with one material select per row.
- BOM Edit add/remove/reindex hook guards.
- BOM Edit normal save path with added rows.
- BOM Edit friendly concurrency feedback.
- Inventory Receipt compact multi-line layout with one stock item select per row.
- Inventory Receipt add/remove/reindex and hidden template control guards.
- Terminology guard for BOM material display text.

## Tests Run

- `dotnet build VPureLux.slnx --no-restore -m:2`
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Bom|FullyQualifiedName~Inventory" -m:1`
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Sales" -m:1`
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build -m:1`
- `git grep -n -i "<legacy component wording>" -- src test docs BUSINESS_ARCHITECTURE_DECISIONS_V2.md UI_IMPLEMENTATION_DECISION_MATRIX.md UI_REFACTOR_SOURCE_OF_TRUTH.md UI_UX_ABP_GUIDE_V2.md`

## Manual Smoke

- BOM Edit smoke target: three rows, remove/add/save, no raw concurrency error, no duplicate select.
- Inventory Receipt smoke target: add three rows, compact aligned UI, no duplicate select.

## Intentionally Not Changed

- Sales behavior.
- BOM or Inventory business rules.
- Domain model rules.
- Application service contracts.
- Database schema, migrations, or indexes.
- Product-stock selling without BOM.
- Stock reservation.
