# UAT Fix - Component List ABP DataTables And Newest-First Sorting

## Issue

The Vật tư list was Razor-rendered on the server with a fixed page-size-style limit. Search could find records, but normal browsing did not provide proper table paging, so older/newer records could appear unreachable from the visible list.

## Root Cause

`Pages/Catalog/Components/Index` loaded a bounded set of components during `OnGetAsync` and rendered rows directly in Razor. The Component application service also always sorted by code ascending, so newly-created Vật tư did not naturally appear at the top.

## Why ABP DataTables

ABP DataTables provides the project-standard MVC/Razor list pattern:

- server-side paging
- server-side sorting
- AJAX reloads for search/filter changes
- modal create/edit result handling without full page reloads
- row actions integrated with ABP modal/action conventions

This avoids loading all Components into the browser and removes custom Razor-only list paging concerns.

## Files Changed

- `src/VPureLux.Application.Contracts/Catalog/Components/ComponentDto.cs`
- `src/VPureLux.Application/Catalog/CatalogApplicationMapper.cs`
- `src/VPureLux.Application/Catalog/Components/ComponentAppService.cs`
- `src/VPureLux.Web/Pages/Catalog/Components/Index.cshtml`
- `src/VPureLux.Web/Pages/Catalog/Components/Index.cshtml.cs`
- `src/VPureLux.Web/Pages/Catalog/Components/Index.js`
- `src/VPureLux.Web/Pages/Catalog/CatalogIndex.js`
- `src/VPureLux.Domain.Shared/Localization/VPureLux/vi-VN.json`
- `test/VPureLux.Web.Tests/Pages/CatalogPagesTests.cs`

## List Event Flow

1. The page renders permissions, filter controls, and `<abp-table id="ComponentsTable">`.
2. `Pages/Catalog/Components/Index.js` initializes DataTables with `serverSide: true`.
3. DataTables calls the Razor Page `List` handler through `abp.libs.datatables.createAjax`.
4. The handler calls `IComponentAppService.GetListAsync` with DataTables paging, sorting, and keyword input.
5. The handler enriches only the returned page of rows with current suggested price context when the user has pricing permission.
6. Search submit and clear button both call `dataTable.ajax.reload()`.

## Paging Behavior

DataTables owns pagination. The initial Razor page no longer fetches and renders a fixed list of Vật tư rows. Only the current table page is fetched from the server.

## Sorting Behavior

Default sorting is `CreationTime DESC`, backed by the audited `Component` entity. `CreationTime` was added to `ComponentDto` so the contract can carry the audited timestamp without changing schema or domain rules.

User column sorting is mapped server-side for Code, Name, Unit, Status, and CreationTime. Unknown sorting fields fall back to `CreationTime DESC`.

## Modal Reload Behavior

Component create/edit modals now reload the DataTable after success when the page declares a table selector. Product pages do not declare a table selector, so their existing full-page reload behavior remains unchanged.

Status row actions use ABP confirmation and AJAX POST, then reload the DataTable after success.

## Intentionally Not Changed

- No Domain rule changes.
- No DB schema or migration changes.
- No Product list refactor.
- No Sales, Inventory, FIFO, costing, or pricing formula changes.
- No Select2 added.
- Backend names remain Component/ComponentId/ComponentDto.

## Tests Run

- `dotnet build VPureLux.slnx --no-restore -m:2` - passed, 0 errors; existing warnings only.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Component|FullyQualifiedName~Catalog" -m:1` - passed, 45 tests.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Product|FullyQualifiedName~Catalog" -m:1` - passed, 56 tests.
- `git diff --check` - passed.
- Terminology grep - passed with existing historical UAT/evidence references only.

## Manual Smoke Checklist

- Create 12-15 Vật tư.
- Open Vật tư list.
- Verify DataTables pagination is visible.
- Verify newest-created Vật tư is first.
- Search item not on first page; verify it appears.
- Clear search; verify list reloads.
- Create a new Vật tư from modal; verify table reloads and new item appears at top.
- Edit a Vật tư; verify table reloads.
- Activate/deactivate a Vật tư; verify table reloads.
- Verify no duplicate Select2/custom select issue.
