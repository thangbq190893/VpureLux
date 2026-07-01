# UAT Fix 03I - Navigation, BOM UX, and Dropdown Overflow

## Reason

UAT found three web UX gaps: compact line editors could clip dropdown menus, BOM wording and navigation did not match business language, and users had to pass through sparse landing pages before reaching common BOM and inventory actions.

## Scope

- Web-layer Razor Pages, shared line-editor CSS/JavaScript, menu contributor, Vietnamese localization, focused Web tests, and this note.
- No Domain, Application, database schema, migration, FIFO/posting, Sales, or BOM business-rule changes.
- Backend identifiers such as `Component`, `ComponentId`, and `StockItemType.Component` remain unchanged.

## Dropdown Overflow Fix

- `LineEditors.css` now makes `.vpl-line-editor` horizontally scrollable while keeping vertical overflow visible.
- Dynamic row Select2 initialization no longer uses ordinary forms as dropdown parents; modal/offcanvas and Sales create contexts are still respected.
- Receipt, Issue, Adjustment, BOM Create, and BOM Edit continue to use the compact line editor hooks.

## BOM Terminology Change

- The BOM menu/title now uses `Định mức sản phẩm (BOM)`.
- BOM line labels remain `Vật tư`.
- Legacy component wording is not reintroduced.

## BOM Landing Page Behavior

- `/Bom` is now a searchable product/BOM summary table.
- Rows show product code/name, status, version count, and current published version when available.
- Row actions include history, create version, and current-version details when supported.
- The old `OpenProduct` post handler remains for compatibility, but the visible UX is no longer dropdown-only.

## BOM Create Product Context

- `/Bom/Create/{productId}` now shows product code, product name, product status, published-BOM context, and links back to BOM history/list.
- Existing create/save rules and line editor behavior are unchanged.

## Inventory Submenu Behavior

- The left menu now exposes `Kho hàng` as a parent submenu with direct links to:
  - `Sổ kho`
  - `Ghi nhận nhập kho`
  - `Ghi nhận xuất kho`
  - `Ghi nhận điều chỉnh kho`
  - `Tồn kho hiện tại`
  - `Lô hàng`
- `/Inventory` remains available for compatibility.

## Tests Run

- `dotnet build VPureLux.slnx --no-restore -m:2` - passed.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Bom|FullyQualifiedName~Inventory|FullyQualifiedName~Menu" -m:1` - passed, 65/65.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build -m:1` - passed, 180/180.
- `git grep -n -i "linh kiện" -- src test docs BUSINESS_ARCHITECTURE_DECISIONS_V2.md UI_IMPLEMENTATION_DECISION_MATRIX.md UI_REFACTOR_SOURCE_OF_TRUTH.md UI_UX_ABP_GUIDE_V2.md` - passed, no matches after final cleanup.

## Manual Smoke Checklist Deferred

- Receipt/Issue/Adjustment/BOM dropdown opens without being clipped.
- BOM menu/title shows `Định mức sản phẩm (BOM)`.
- BOM landing page is searchable/table/action style.
- Create BOM version shows product context.
- Inventory menu expands into direct submenu items.
