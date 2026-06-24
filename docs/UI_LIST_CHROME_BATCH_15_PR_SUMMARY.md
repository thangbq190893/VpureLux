# Batch 15 — Remaining List Page Chrome — PR Summary

**Title:** feat(web): apply Batch 14 list chrome to remaining VPURELUX index pages  
**Branch:** `feature/ui-list-chrome-batch-15`  
**Base branch:** `main` (@ `115870a`)  
**Document type:** Merge-request / pull-request summary

---

## Scope summary

Batch 15 applies **existing Batch 14 opt-in visual helpers** to the remaining menu-visible **list/index** pages that were deferred after Batch 14. All work is **Web Razor presentation only** — wrapper markup and CSS class additions on existing structure.

Implemented phases:

| Phase | Focus |
|-------|--------|
| 15.1 | Audit and implementation plan (`UI_LIST_CHROME_BATCH_15_PLAN.md`) |
| 15.2A | Pricing Index, Customers Index, CustomerGroups Index |
| 15.3a | BOM Index (product launcher), BOM Product (version history) |
| 15.4 | Inventory hub (`list-group` navigation), Warehouses admin |
| 15.5 | Full Web.Tests validation and this PR summary |

**No backend, Domain, Application, EF Core, migration, API contract, permission, or business-rule changes.**

Visual changes are **Razor class/layout additions only** using helpers already defined in Batch 14 `global-styles.css`. **Formatting was not changed in Batch 15** (price/date/money/quantity display strings unchanged). Batch 13 branding and Batch 14 inquiry/catalog polish are preserved.

---

## Commit list (oldest → newest on branch vs `main`)

| Commit | Message |
|--------|---------|
| `7f4da77` | docs: add Batch 15 list chrome plan |
| `c2fac13` | Merge remote-tracking branch `origin/main` into feature/ui-list-chrome-batch-15 |
| `486e2af` | feat(web): polish Pricing and Customer list chrome |
| `a1c33e4` | feat(web): polish BOM list page chrome |
| `47713b8` | feat(web): polish Inventory hub and warehouse chrome |
| _(wrap-up, latest)_ | docs: add Batch 15 list chrome PR summary |

---

## Files changed by area (`main...HEAD`)

| Area | Files |
|------|-------|
| **Docs** | `docs/UI_LIST_CHROME_BATCH_15_PLAN.md`, `docs/UI_LIST_CHROME_BATCH_15_PR_SUMMARY.md` |
| **Pricing** | `Pages/Pricing/Index.cshtml` |
| **Customers** | `Pages/Customers/Index.cshtml`, `Pages/CustomerGroups/Index.cshtml` |
| **BOM** | `Pages/Bom/Index.cshtml`, `Pages/Bom/Product.cshtml` |
| **Inventory** | `Pages/Inventory/Index.cshtml`, `Pages/Inventory/Warehouses.cshtml` |

**Total:** 9 paths vs `main` before wrap-up doc commit (1 plan doc added, 7 Razor pages modified, 1 PR summary added at wrap-up).

**Not touched:** `global-styles.css`, PageModel `.cshtml.cs` files, JavaScript, create/edit/details/modal pages, Inventory posting (Receipt/Issue/Adjustment), Inventory inquiry trio (Ledger/Balances/Lots — already Batch 14), Catalog/Sales/Audit pages, Home launcher, backend layers, tests (no test file changes required).

---

## What changed

### Shared pattern (all target pages)

- `.vpl-page` page wrapper
- `.vpl-page-header` + `.vpl-page-title` (external header; localization keys unchanged)
- `.vpl-card-dense` on main list/card containers where applicable
- `.vpl-table-dense` on list tables where applicable
- `.vpl-btn-toolbar` on existing create/action header rows (Customers, CustomerGroups, BOM Product)

### 15.2A — Pricing, Customers, CustomerGroups

- **Pricing Index:** tabbed layout preserved; both tables dense; tabs wrapped in dense card; Open History buttons standardized to `btn-outline-secondary` (permission gates unchanged)
- **Customers / CustomerGroups Index:** Catalog 14.5a pattern — external header, dense list card, dense table; monolithic card header removed; all `data-customer-*` / `data-customer-group-*` hooks preserved

### 15.3a — BOM

- **BOM Index:** external page title; dense card around POST product picker (form fields/handlers unchanged)
- **BOM Product:** external header + toolbar for Create; dense history card; dense version table; pricing context block and row actions (Publish/Archive/Clone/Edit) unchanged

### 15.4 — Inventory hub and Warehouses

- **Inventory Index:** bare `h2` replaced with page chrome; `list-group` wrapped in dense card; all navigation links and permission `@if` gates preserved
- **Warehouses:** external page title; dense create card and dense list card; dense warehouse table; create form and activate/deactivate POST handlers unchanged

---

## What explicitly did not change

- **Backend / Domain / Application / EF Core / migrations / DB schema**
- **API contracts** and **PageModel query logic**
- **Routes, handlers, form field names, GET/POST bindings**
- **Permissions** and authorization policies — no gates removed
- **Business rules:**
  - **Pricing** — tab content, history links, price display logic unchanged
  - **Customers / CustomerGroups** — status activate/deactivate, modal links, filters unchanged
  - **BOM** — create/edit/publish/archive/clone behavior unchanged
  - **Inventory posting / FIFO** — Receipt/Issue/Adjustment/Ledger/Balances/Lots untouched
  - **Warehouse** create/list/activate/deactivate behavior unchanged
- **Create/edit/details/modal** pages and **JavaScript** for Customers, CustomerGroups, BOM, Warehouses
- **Formatting** — no price/date/money/quantity formatter changes in Batch 15
- **Batch 14** pages (Catalog, Sales Index, Audit, Inventory inquiry trio) — no drive-by edits
- **Home** launcher, **login** shell, **LeptonX** package sources
- **Tests** — no Web.Tests source changes

Layout changes are **class-only** on existing Razor structure. No new CSS tokens or global overrides of all `.card`, `.table`, or `.btn` elements.

---

## Permission / business-rule safety notes

| Concern | Status |
|---------|--------|
| Permission gates removed | **No** — all `@if` / `AuthorizeAsync` checks preserved |
| Pricing history links | **No change** — component/product history permission gates unchanged |
| Customer/CustomerGroup status actions | **No change** — dropdown forms and confirm hooks preserved |
| BOM publish/archive/create | **No change** — row action forms and `CanCreate`/`CanPublish`/`CanArchive` preserved |
| Warehouse activate/deactivate | **No change** — POST handlers and confirm attributes preserved |
| Inventory hub links | **No change** — `CanManageWarehouses`, `CanReceive`, `CanIssue`, `CanAdjust`, `CanViewLedger` gates preserved |
| New or removed UI actions/columns | **No** — same links, dropdowns, tables, handlers |
| Inventory posting / FIFO calculations | **No change** — posting pages not in scope |

---

## Test results

Wrap-up validation (2026-06-24):

```bash
dotnet build VPureLux.slnx --no-restore -m:2
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build -m:1
```

| Check | Result |
|-------|--------|
| Build | **Succeeded** — 0 errors, 3 pre-existing warnings (Domain nullability, Test SDK entry point) |
| Full Web.Tests | **Passed** — 120/120, 0 failed, 0 skipped (~5m 0s) |

Per-phase filters run during implementation:

| Filter | Result |
|--------|--------|
| `FullyQualifiedName~Pricing` | 15/15 passed |
| `FullyQualifiedName~Customer` | 22/22 passed |
| `FullyQualifiedName~Bom` | 13/13 passed |
| `FullyQualifiedName~Inventory` | 31/31 passed |

---

## Known deferred items

Not in this MR (see `docs/UI_LIST_CHROME_BATCH_15_PLAN.md` and Batch 14 audit):

- **Sales History / Sales CustomerHistory** page chrome
- **Pricing detail/history** pages (`Pricing/Components/History`, `Pricing/Products/History`)
- **Home launcher** density (`Pages/Index.cshtml` / `Index.css`)
- **Empty-state wiring** — `.vpl-empty-state` helper exists but not applied to zero-row states on Batch 15 pages
- **Remaining form/details page density** polish (create/edit/details modals, posting forms)
- **Pricing display alignment** — Pricing Index still uses `N0` + currency code vs Catalog/Sales `₫` formatting (separate approval)
- **Dark-theme** verification pass across all refined pages

---

## Suggested PR/MR description

```markdown
## Summary

Batch 15 applies Batch 14 VPURELUX list/page chrome helpers to remaining index pages:

- Pricing Index (tabbed tables)
- Customers and CustomerGroups Index
- BOM Index and BOM Product history
- Inventory hub navigation and Warehouses admin

Web-only Razor class/layout changes. No backend, permission, route, handler, or formatting changes.

## Safety

- No backend, Domain, Application, EF, migration, or API changes
- No business rule, route, handler, or permission changes
- No create/edit/details/modal or JavaScript changes
- No Inventory posting/FIFO behavior changes
- Visual changes are Razor class-only plus documentation

## Test plan

- [x] `dotnet build VPureLux.slnx --no-restore`
- [x] `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build`
- [ ] Manual smoke: Pricing tabs, Customers/CustomerGroups filters, BOM product picker/history, Inventory hub links, Warehouses create/list

## Deferred

Sales History, Pricing detail pages, Home launcher, empty-state wiring — see `docs/UI_LIST_CHROME_BATCH_15_PLAN.md`
```

---

**Wrap-up status:** PR summary complete; ready for MR/PR from `feature/ui-list-chrome-batch-15` → `main`.
