# Batch 17 — UI Residual Audit & Final Hardening Plan

**Document type:** Audit / implementation plan (no code changes in 17.1)  
**Branch:** `feature/ui-residual-hardening-batch-17`  
**Base branch:** `main` (@ `f7f5357` — Merge PR #15 Batch 16A home launcher + Inventory empty states)  
**Baseline tag:** `ui-polish-phase-2026-06-25` (UI polish phase complete on `main`; build + 120/120 Web.Tests passed at tag)  
**Audit date:** 2026-06-24  
**Owner:** Cursor Agent (Composer 2.5)

---

## 1. Executive summary

Batches **13–16A** delivered VPURELUX branding, Batch 14 dense ERP helpers (`.vpl-page`, `.vpl-card-dense`, `.vpl-table-dense`, `.vpl-btn-toolbar`, `.vpl-empty-state`), Batch 15 list/index chrome, Batch 16B Sales/Pricing history-detail chrome, and Batch 16A Home launcher density + Inventory inquiry empty states.

**Static audit conclusion:** High-traffic list, inquiry, history, and hub pages are **visually aligned** with the Batch 14/15/16 helper pattern. Remaining gaps are overwhelmingly **form/create/edit/detail/modal surfaces** that were intentionally left plain, **empty-table-only states** deferred since Batch 16A, or **cosmetic detail-page chrome** on lower-traffic routes.

**No Blocker findings.** No permission leaks, route/handler regressions, or missing gates were identified in Razor source. Residual work is optional: small **test hardening** (source-level permission gate assertions) is the highest-value follow-up; visual hardening on detail/form pages is **Defer** unless product explicitly prioritizes it.

**Recommendation:** Proceed with a **small Batch 17.3 test-only hardening** (optional) or **close Batch 17 with documentation only** if the team accepts the current UI baseline at tag `ui-polish-phase-2026-06-25`. Skip broad Batch 17.2 visual changes unless Audit/BOM Details chrome is explicitly requested.

---

## 2. Current branch / base branch

| Item | Value |
|------|--------|
| **Working branch** | `feature/ui-residual-hardening-batch-17` |
| **Base branch** | `main` @ `f7f5357` |
| **Baseline tag** | `ui-polish-phase-2026-06-25` |
| **Shared helpers** | Defined in `src/VPureLux.Web/wwwroot/global-styles.css` (Batch 14) |

---

## 3. Baseline tag note

Tag **`ui-polish-phase-2026-06-25`** marks the end of the planned UI polish phase (Batches 13–16A merged). Validation at baseline: `dotnet build VPureLux.slnx` succeeded; full `VPureLux.Web.Tests` **120/120 passed**. Batch 17 audits **residual** issues only — not a reopening of Batches 14–16 scope.

---

## 4. Audit scope

| Area | Inspected |
|------|-----------|
| `src/VPureLux.Web/Pages/**/*.cshtml` | All **64** Razor pages inventoried |
| `src/VPureLux.Web/wwwroot/global-styles.css` | Helper definitions (read-only) |
| `test/VPureLux.Web.Tests/Pages/**/*.cs` | **12** test files (~120 Web.Tests total) |
| Prior batch docs | Batch 14 audit, 15/16/16A PR summaries |
| Reference docs listed in task | `UI_PERMISSION_VISIBILITY_MATRIX.md`, `UI_ROUTE_PRESERVATION_MATRIX.md`, `UI_ABP_PATTERN_GUIDE.md`, `UI_REFACTOR_EXECUTION_CHECKLIST.md` — **not present in repo**; audit used `UI_RAZOR_PAGES_GUIDE.md`, batch PR summaries, and static grep instead |

**Method:** Repository search and file reads only. No browser, HTTP, or runtime verification.

---

## 5. Pages inspected by module

### Home (1 page)

| Page | Status |
|------|--------|
| `Pages/Index.cshtml` | **Polished** (16A: `.vpl-page`, `.vpl-card-dense`, `.vpl-page-subtitle`, `Index.css` density) |

### Catalog (10 pages)

| Page | Status |
|------|--------|
| `Catalog/Products/Index.cshtml` | **Polished** (15: page chrome, dense card/table, toolbar, thumbnails) |
| `Catalog/Components/Index.cshtml` | **Polished** (same) |
| `Catalog/*/Create.cshtml`, `Edit.cshtml`, `Details.cshtml`, `*Modal.cshtml` | **Intentionally plain** — modal/full-page create-edit workflow; image upload hooks |

### Customers (7 pages)

| Page | Status |
|------|--------|
| `Customers/Index.cshtml` | **Polished** (15: page chrome, dense card/table, toolbar, status forms) |
| `Customers/Create.cshtml`, `Edit.cshtml`, `Details.cshtml`, `*Modal.cshtml` | **Intentionally plain** — CRUD/modal pattern |

### CustomerGroups (7 pages)

| Page | Status |
|------|--------|
| `CustomerGroups/Index.cshtml` | **Polished** (15) |
| `CustomerGroups/Create.cshtml`, `Edit.cshtml`, `Details.cshtml`, `*Modal.cshtml` | **Intentionally plain** |

### BOM (6 pages)

| Page | Status |
|------|--------|
| `Bom/Index.cshtml` | **Polished** (15: `.vpl-page`, dense card; product picker form — no table) |
| `Bom/Product.cshtml` | **Polished** (15: page chrome, toolbar, dense table; publish/archive POST forms) |
| `Bom/Details.cshtml` | **Plain** — card + default table (no `.vpl-page` / `.vpl-table-dense`) |
| `Bom/Create.cshtml`, `Edit.cshtml`, `Clone.cshtml` | **Intentionally plain** — multi-line BOM editing |

### Pricing (5 pages)

| Page | Status |
|------|--------|
| `Pricing/Index.cshtml` | **Polished** (15: page chrome, dense tabs/tables; inline `text-muted` for missing prices — not page empty-state) |
| `Pricing/Components/History.cshtml`, `Products/History.cshtml` | **Polished** (16B: page chrome, `.vpl-empty-state`) |
| `Pricing/Components/Create.cshtml`, `Products/Create.cshtml` | **Intentionally plain** |

### Inventory (8 pages)

| Page | Status |
|------|--------|
| `Inventory/Index.cshtml` | **Polished** (15: hub with permission-gated links) |
| `Inventory/Warehouses.cshtml` | **Polished** (15) |
| `Inventory/Ledger.cshtml`, `Balances.cshtml`, `Lots.cshtml` | **Polished** (14 inquiry + 16A `.vpl-empty-state`) |
| `Inventory/Receipt.cshtml`, `Issue.cshtml`, `Adjustment.cshtml` | **Intentionally plain** — posting forms with `data-inventory-*` JS, idempotency keys, dynamic line rows |

### Sales (6 pages)

| Page | Status |
|------|--------|
| `Sales/Index.cshtml`, `History.cshtml`, `CustomerHistory.cshtml`, `Details.cshtml` | **Polished** (14/15/16B) |
| `Sales/Create.cshtml`, `Edit.cshtml` | **Intentionally plain** — order workflow; `alert alert-light border` on **`data-sales-product-context`** (JS panel, not empty-state) |

### Audit (4 pages)

| Page | Status |
|------|--------|
| `Audit/Index.cshtml` | **Polished** (14/15: inquiry chrome, export gate, dense table; **no page empty-state** — empty tbody only) |
| `Audit/Details.cshtml` | **Plain** — stacked `abp-card` + `dl` + JSON `<pre>` blocks |
| `Audit/Export.cshtml`, `Reports.cshtml` | **Plain** — utility/report surfaces |

### Account / framework (4 pages)

| Page | Status |
|------|--------|
| `HostDashboard.cshtml`, `TenantDashboard.cshtml` | **ABP SaaS widgets** — out of VPURELUX polish scope |
| `PrivacyPolicy.cshtml`, `CookiePolicy.cshtml` | **Legal/static** — acceptable default Bootstrap |

---

## 6. Residual UI findings table

| Severity | Module | File path | Finding | Recommendation | Risk |
|----------|--------|-----------|---------|----------------|------|
| Defer | Sales | `Sales/Create.cshtml`, `Sales/Edit.cshtml` | `alert alert-light border` on `data-sales-product-context` — product context panel, not zero-data empty state | **Do not** swap to `.vpl-empty-state`; would break JS selector semantics | Medium if class changed without JS update |
| Defer | Audit | `Audit/Index.cshtml` | Empty log list renders empty table only (no `.vpl-empty-state`) | Defer — adding block is new UX; low traffic vs inquiry pages | Low |
| Defer | BOM | `Bom/Product.cshtml` | No versions → empty table tbody only | Defer — same rationale as 16A audit | Low |
| Defer | Audit | `Audit/Details.cshtml` | No `.vpl-page` / dense cards; heavy card stack | Nice-to-have detail chrome only if product requests | Low — cosmetic |
| Defer | BOM | `Bom/Details.cshtml` | No page chrome; table lacks `.vpl-table-dense` | Nice-to-have; match 16B detail pattern optionally in 17.2 | Low — cosmetic |
| Defer | Inventory | `Receipt.cshtml`, `Issue.cshtml`, `Adjustment.cshtml` | Plain `h2` + default cards; no `.vpl-page` | Defer — form-heavy posting workflow; Batch 14 audit item 4 | Low if forced list chrome onto forms |
| Defer | Pricing | `Pricing/Index.cshtml` | Money uses `N0` + currency code vs Sales/Catalog `₫` | Defer — display alignment needs separate product approval (16B deferred) | Low — not a helper gap |
| Defer | All modules | Create/Edit/Details/Modal pages (~35 files) | Default LeptonX card/forms without Batch 14 list chrome | **Accept** — modals and multi-step forms should stay plain per `UI_RAZOR_PAGES_GUIDE.md` | Low |
| Nice-to-have | Audit | `Audit/Details.cshtml` | Could add `.vpl-page-header` + `.vpl-card-dense` without logic change | Optional 17.2 if detail consistency matters | Low |
| Nice-to-have | BOM | `Bom/Details.cshtml` | Same optional detail chrome + `.vpl-table-dense` on component table | Optional 17.2 | Low |

**No Blocker or Should Fix visual items** on high-traffic list/inquiry/history pages after Batch 16A.

---

## 7. Permission visibility findings table

Static review of permission-sensitive UI (Razor `@if` / PageModel flags / `[Authorize]`). **No leaks found.**

| Area | File / permission | Gate pattern | Test coverage | Finding |
|------|-------------------|--------------|---------------|---------|
| Pricing history links | `Pricing/Index.cshtml` — `Pricing.ComponentSuggestedSellingPrices.History`, `Pricing.History` | `canViewComponentHistory` / `canViewProductHistory` wrap history buttons | PageModel N/A (inline Razor auth); **no source assertion** on Index gates | Gates present in Razor — **Should Fix test** (17.3): add source-level assertions |
| Sales Customer History toolbar | `Sales/Index.cshtml` — `Sales.ViewProfit` + `CanViewHistory` | `canViewCustomerHistory` composite | PageModel `CanViewHistory` tested; **ViewProfit Razor gate not source-asserted on Index** | Gates present — **Should Fix test** (17.3) |
| Sales profit columns | `Sales/History.cshtml`, `Details.cshtml` — `Sales.ViewProfit` | `canViewProfit` column `@if` | Source tests on History + Details | **OK** |
| Sales cost columns | `Sales/Details.cshtml` — `Sales.ViewCost` | `canViewCost` column `@if` | Source test `Sales_Details_Should_Guard_Cost_Columns_With_ViewCost` | **OK** |
| Customer History page | `Sales/CustomerHistory.cshtml.cs` | `[Authorize]` ViewCustomerHistory + ViewProfit on PageModel | Renders under default test admin | **OK** |
| Inventory Ledger hub link | `Inventory/Index.cshtml` — `Inventory.ViewLedger` | `@if (Model.CanViewLedger)` | PageModel `CanViewLedger` false tested; **Razor `@if` not source-asserted** | **Should Fix test** (17.3) |
| Inventory Ledger page | `Inventory/Ledger.cshtml.cs` | `[Authorize(ViewLedger)]` | Route render test | **OK** |
| Audit Export | `Audit/Index.cshtml` — `Audit.Export` | `@if (Model.CanExport)` | PageModel `CanExport` false tested | **OK** |
| BOM publish/archive | `Bom/Product.cshtml` | `CanPublish` / `CanArchive` + POST handlers | HTML tests for confirm hooks + publish/archive flow | **OK** |
| Customer/Group status | `Customers/Index.cshtml`, `CustomerGroups/Index.cshtml` | `CanManageStatus` + POST handlers | PageModel permission tests + confirmation hook tests | **OK** |
| Pricing history Create | `Pricing/*/History.cshtml` | `@if (Model.CanCreate)` | History pages render under admin | **OK** |

**No permission behavior changes recommended in Batch 17.**

---

## 8. Route / handler / form binding risk table

| Severity | Module | File | Hook / binding | Finding | Recommendation |
|----------|--------|------|----------------|---------|----------------|
| — | Sales | `Details.cshtml` | `asp-page-handler="Confirm|Cancel"`, `data-sales-action-form` | Unchanged since 16B; JS in `Details.js` | Preserve in any future chrome work |
| — | Sales | `Create.cshtml`, `Edit.cshtml` | `data-sales-context-endpoint`, `data-sales-product-selector`, `data-sales-product-context` | Context panel tied to `SalesProductContext.js` | **Do not** rename `data-*` or handler names |
| — | BOM | `Product.cshtml` | `asp-page-handler="Publish|Archive"`, `data-bom-action-form` | Confirmed in tests | Preserve |
| — | Catalog | `*/Index.cshtml` | `data-catalog-*`, status POST handlers | Confirmed in Catalog tests | Preserve |
| — | Customers / Groups | `Index.cshtml` | `data-customer-status-form`, `data-customer-group-status-form` | Confirmed in tests | Preserve |
| — | Inventory | `Receipt/Issue/Adjustment.cshtml` | `Input.IdempotencyKey`, `data-inventory-posting-form`, dynamic line `data-name` templates | Extensively tested in `InventoryPagesTests` | **High-risk surface** — no Batch 17 UI edits |
| — | Inventory | `Warehouses.cshtml` | Activate/Deactivate POST handlers | Tested | Preserve |
| — | BOM | `Index.cshtml` | `asp-page-handler="OpenProduct"` | Simple POST redirect | Preserve |

**No route/handler/form binding risks identified requiring code changes.** Any future visual work must remain class-only on these pages.

---

## 9. Test coverage findings table

| Area | Coverage | Gap | Severity | Recommendation |
|------|----------|-----|----------|----------------|
| Home | `Index_Tests.cs` (2 tests) | No `.vpl-card-dense` assertion | Defer | Avoid fragile CSS tests |
| Catalog | `CatalogPagesTests.cs`, image + permission tests | Good list/thumbnail/route coverage | — | OK |
| Customers / Groups | `CustomerPagesTests.cs`, `CustomerPageModelPermissionTests.cs` | Good | — | OK |
| BOM | `BomPagesTests.cs` (14 tests) | Publish/archive hooks covered | — | OK |
| Pricing | `PricingPagesTests.cs` (20 tests) | Index history **permission gates not source-asserted** | Should Fix | 17.3: assert `canViewComponentHistory` / `canViewProductHistory` in Index.cshtml source |
| Inventory | `InventoryPagesTests.cs` (42 tests) | Hub `CanViewLedger` Razor `@if` not source-asserted; **`vpl-empty-state` CSS asserted** (16A) | Should Fix / Note | 17.3: source assert `@if (Model.CanViewLedger)`; consider asserting localization not CSS for empty states long-term |
| Sales | `SalesPagesTests.cs` (26 tests) | Index `canViewCustomerHistory` / ViewProfit gate not source-asserted | Should Fix | 17.3: source assert on `Sales/Index.cshtml` |
| Audit | `AuditPagesTests.cs` (18 tests) | Export gate PageModel tested | — | OK |
| Detail pages | Audit/BOM Details | Render-only via module tests; no chrome assertions | Defer | OK unless 17.2 touches Details |
| Fragile CSS | `InventoryPagesTests` | Asserts `"vpl-empty-state"` string | Note | Accept for now; prefer localization-key assertions if tests are revised |

**Web.Tests total:** 120 tests — strong coverage for business-critical flows (posting idempotency, Sales formatting, BOM actions, Catalog routes).

---

## 10. Proposed Batch 17 implementation batches

### Batch 17.2 — Highest-value residual UI hardening (optional / likely skip)

**Goal:** Only if product requests detail-page consistency.

| Candidate | Change | Files |
|---------|--------|-------|
| Audit Details chrome | Add `.vpl-page`, `.vpl-page-header`, `.vpl-card-dense` (class-only) | `Audit/Details.cshtml` |
| BOM Details chrome | Same + `.vpl-table-dense` on component table | `Bom/Details.cshtml` |

**Skip if:** Team accepts baseline tag without detail-page polish (recommended).

**Validation:** `dotnet build`; focused module tests; manual smoke on Details pages.

---

### Batch 17.3 — Permission / test hardening (recommended if any code batch runs)

**Goal:** Lock permission gates in Razor source without changing UI behavior.

| Test addition | File under test |
|---------------|-----------------|
| Assert `canViewComponentHistory` / `canViewProductHistory` gates wrap history links | `Pricing/Index.cshtml` source read |
| Assert `canViewCustomerHistory` uses `ViewProfit` + `CanViewHistory` | `Sales/Index.cshtml` source read |
| Assert `@if (Model.CanViewLedger)` wraps Ledger hub link | `Inventory/Index.cshtml` source read |

**No Razor/CSS production changes.** Tests only.

**Validation:** `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --filter "FullyQualifiedName~Pricing|FullyQualifiedName~Sales|FullyQualifiedName~Inventory"`

---

### Batch 17.4 — Final validation + PR summary

**Goal:** If 17.2/17.3 executed: full Web.Tests + `docs/UI_RESIDUAL_HARDENING_BATCH_17_PR_SUMMARY.md`.  
If **no code batches**: document-only close-out stating baseline tag acceptance.

---

## 11. Explicit deferrals

| Item | Reason |
|------|--------|
| Empty-state wiring outside Inventory inquiry trio | Sales context panels, Audit/BOM empty tables, Pricing inline cells — deferred since 16A |
| Sales Create/Edit context `alert` styling | JS-bound panel; not empty-state |
| Form/create/edit density (posting, Sales, Pricing Create, BOM edit) | Workflow pages — do not force list chrome |
| Pricing `N0`/currency vs `₫` alignment | Separate product/formatting approval |
| Catalog/Customer modal pages | ABP ModalManager pattern — plain is correct |
| Host/Tenant dashboards, legal pages | Framework/static |
| Backend permission refactor | Out of UI scope |
| New dashboards, metrics, KPIs, module tiles | Explicit non-goals |
| New navigation destinations | Explicit non-goals |
| Dark-theme verification | Future |
| `global-styles.css` token changes | Helpers sufficient |
| Broad LeptonX redesign | Explicit non-goals |

---

## 12. Non-goals

- No backend, Domain, Application, EF Core, migration, or DB schema changes
- No business-rule changes
- No permission **behavior** changes in Batch 17
- No route, handler, or form field name / binding changes
- No JavaScript hook or idempotency key changes
- No Inventory posting/FIFO/receipt/issue/adjustment behavior changes
- No broad visual redesign or new UI framework
- No runtime/browser/video verification in this batch
- No inventing empty states where none exist today

---

## 13. Recommended next step

**Primary recommendation:** **Close Batch 17 with documentation only** — the UI polish phase at tag `ui-polish-phase-2026-06-25` is consistent enough for UAT; no mandatory visual patches remain on high-traffic pages.

**Optional follow-up (low risk, high value):** Run **Batch 17.3 test-only** hardening (~3 source-level assertions) to guard Pricing/Sales/Inventory permission gates without touching Razor.

**Do not run Batch 17.2** unless stakeholders explicitly want Audit/BOM Details chrome; benefit is cosmetic only.

---

## References

- `docs/UI_VISUAL_REFINEMENT_BATCH_14_PR_SUMMARY.md` — helper definitions
- `docs/UI_LIST_CHROME_BATCH_15_PR_SUMMARY.md` — list page scope
- `docs/UI_HISTORY_DETAIL_CHROME_BATCH_16_PR_SUMMARY.md` — history/detail scope + deferrals
- `docs/UI_HOME_EMPTY_STATE_BATCH_16A_PR_SUMMARY.md` — Home + Inventory empty states
- `docs/UI_RAZOR_PAGES_GUIDE.md` — modal vs full-page workflow rules
- `src/VPureLux.Web/wwwroot/global-styles.css` — `.vpl-*` helpers

---

**Status:** Batch 17.1 audit/plan **complete**. Next: team decision — **skip to 17.4 doc close-out** or **Batch 17.3 test hardening** (recommended optional).
