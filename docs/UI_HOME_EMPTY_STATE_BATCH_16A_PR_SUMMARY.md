# Batch 16A — Home Launcher Density & Inventory Empty-State Polish — PR Summary

**Title:** Batch 16A: Home launcher and Inventory empty-state polish  
**Branch:** `feature/ui-home-empty-state-batch-16a`  
**Base branch:** `main` (@ `6c78ca6` — Merge PR #14 Batch 16B history/detail chrome)  
**Document type:** Merge-request / pull-request summary

---

## Scope summary

Batch 16A applies **existing Batch 14 VPURELUX visual helpers** to two deferred surfaces called out since Batch 14 audit and Batch 16B deferrals:

1. **Home launcher density** — align hero and module cards with `.vpl-page`, `.vpl-card-dense`, and `.vpl-page-subtitle`; tighten `Index.css` spacing using existing `--vpl-*` tokens.
2. **Inventory inquiry empty states** — replace bulky `alert alert-light border` wrappers with `.vpl-empty-state` on Ledger, Balances, and Lots.

All work is **Web Razor/CSS presentation only** — class additions, minimal Home-specific CSS tuning, one test assertion update, and documentation.

**No backend, Domain, Application, EF Core, migration, API contract, permission, or business-rule changes.**

Visual changes are **Razor/CSS class/layout-only plus documentation**. Batch 16A applies existing VPURELUX visual helpers to Home density and Inventory inquiry empty states. Inventory empty-state text/localization keys and conditionals were preserved.

---

## Commit list (oldest → newest on branch vs `main`)

| Commit | Message |
|--------|---------|
| `917381c` | docs: add Batch 16A home empty-state plan |
| `6ad0812` | feat(web): polish Home launcher density |
| `8d206b3` | feat(web): polish Inventory inquiry empty states |
| _(wrap-up, latest)_ | docs: add Batch 16A home empty-state PR summary |

---

## Files changed by area (`main...HEAD`)

| Area | Files |
|------|-------|
| **Docs** | `docs/UI_HOME_EMPTY_STATE_BATCH_16A_PLAN.md`, `docs/UI_HOME_EMPTY_STATE_BATCH_16A_PR_SUMMARY.md` |
| **Home launcher** | `src/VPureLux.Web/Pages/Index.cshtml`, `src/VPureLux.Web/Pages/Index.css` |
| **Inventory inquiry empty states** | `src/VPureLux.Web/Pages/Inventory/Ledger.cshtml`, `Balances.cshtml`, `Lots.cshtml` |
| **Tests** | `test/VPureLux.Web.Tests/Pages/InventoryPagesTests.cs` (empty-state CSS assertion only) |

**Total:** 8 paths vs `main` before wrap-up doc commit (1 plan doc added, 2 Home files modified, 3 Inventory Razor files modified, 1 test file modified, 1 PR summary added at wrap-up).

**Not touched:** `global-styles.css`, PageModel `.cshtml.cs` files, JavaScript, Inventory Index/Warehouses/Receipt/Issue/Adjustment, Sales/Pricing/BOM/Catalog/Audit pages, backend layers.

---

## What changed

### 16A.2 — Home launcher density (`Pages/Index.cshtml`, `Pages/Index.css`)

| Element | Change |
|---------|--------|
| Outer wrapper | Added `.vpl-page` on `.vpl-home-welcome` |
| Hero card | Added `.vpl-card-dense`; hero margin `mb-4` → `mb-3` |
| Hero subtitle | Added `.vpl-page-subtitle` (same Vietnamese copy) |
| Module grid | `g-3` → `g-2`; each module card gets `.vpl-card-dense` |
| Module icons | `mb-3` → `mb-2` per card |
| Index.css | Tighter title/icon/card typography and padding via `--vpl-space-*`, `--vpl-font-size-table`, `--vpl-radius-sm` |

**Preserved:** same six module links (Catalog, Pricing, BOM, Inventory, Sales, Audit); auth/login branching; hardcoded Vietnamese module titles/descriptions; no new tiles or destinations.

### 16A.3 — Inventory inquiry empty states

| Page | Change |
|------|--------|
| **Ledger** | `alert alert-light border mb-0` → `vpl-empty-state` for `@L["Inventory:NoLedgerEntries"]` inside existing `@if (Model.Items.Count == 0)` |
| **Balances** | Same pattern for `@L["Inventory:NoBalances"]` |
| **Lots** | Same pattern for `@L["Inventory:NoLots"]` |

**Preserved:** Batch 14 `.vpl-page`, `.vpl-card-dense`, `.vpl-table-dense` inquiry chrome; filter forms, warehouse/stock selectors, table columns, routes, permissions, and displayed values.

### 16A.3 — Test update

`InventoryPagesTests.Inquiry_Pages_Should_Render_Empty_State_When_No_Rows` now asserts `vpl-empty-state` instead of `alert alert-light border`. Localization key assertions unchanged.

---

## What explicitly did not change

- **Backend / Domain / Application / EF Core / migrations / DB schema**
- **API contracts** and **PageModel query logic**
- **Business rules** — no calculation, FIFO, posting, receipt, issue, or adjustment behavior changes
- **Permissions** and authorization policies
- **Routes, handlers, form field names, GET/POST bindings**
- **Create/edit/modal behavior** and **JavaScript**
- **Inventory query/filter behavior** — warehouse/stock filters unchanged
- **Inventory posting/FIFO/receipt/issue/adjustment behavior**
- **Home navigation destinations** — same six `asp-page` targets
- **Module launcher cards** — none added or removed
- **Fake counts, fake metrics, fake summaries, or dashboard KPIs** — none added
- **Inventory empty-state text/localization keys** — `Inventory:NoLedgerEntries`, `Inventory:NoBalances`, `Inventory:NoLots` preserved
- **Inventory empty-state conditionals** — same `@if (Model.Items.Count == 0)` blocks
- **Date/number/money formatting** on inquiry pages
- **Batch 16B** Sales/Pricing history-detail pages
- **Batch 15** list chrome pages
- **`global-styles.css`** — existing `.vpl-empty-state` helper used as-is

Layout changes are **class-only** on existing Razor structure plus conservative Home-specific CSS in `Index.css`. No new global tokens or LeptonX overrides.

---

## Permission / business-rule safety notes

| Concern | Status |
|---------|--------|
| Home module links changed | **No** — same six routes |
| Home module tiles added/removed | **No** |
| Home permission gates added | **No** — Home shows same links for all authenticated users |
| Inventory inquiry filters changed | **No** — GET bindings, warehouse/stock selectors preserved |
| Inventory empty-state conditionals changed | **No** |
| Inventory empty-state text changed | **No** — same localization keys |
| Inventory posting/FIFO behavior | **No change** |
| Inventory table columns/data hidden | **No** |
| New or removed UI actions | **No** |

**Out of scope for this visual PR:** any separate permission/security hardening beyond preserving existing gates.

---

## Test results

Wrap-up validation (2026-06-24):

```bash
dotnet build VPureLux.slnx --no-restore -m:2
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build -m:1
```

| Check | Result |
|-------|--------|
| Build | **Succeeded** — 0 errors, 3 pre-existing warnings (Domain nullability ×2, Test SDK entry point ×1) |
| Full Web.Tests | **Passed** — 120/120, 0 failed, 0 skipped (~5m 4s) |

Per-phase filters run during implementation:

| Filter | Result |
|--------|--------|
| `FullyQualifiedName~Home\|FullyQualifiedName~Index` | 12/12 passed (16A.2) |
| `FullyQualifiedName~Inventory` | 31/31 passed (16A.3) |

---

## Known deferred items

Not in this MR (see `docs/UI_HOME_EMPTY_STATE_BATCH_16A_PLAN.md`):

| Item | Notes |
|------|-------|
| **Empty-state wiring outside Inventory inquiry trio** | Sales Create/Edit context panels, BOM Product, Audit Index, Catalog lists, Pricing Index inline cells |
| **Sales Create/Edit context panels** | `alert alert-light border` with JS hooks — not zero-data empty state |
| **BOM Product no-alert empty-state** | No existing empty alert; adding block = new UX |
| **Audit Index empty table state** | Empty table body only today |
| **Catalog list / Pricing Index inline empty cells** | Inline `text-muted` placeholders, not page-level empty-state |
| **Remaining form/create/edit density polish** | Posting forms, Sales Create/Edit, Pricing Create, etc. |
| **Dark-theme verification** across refined pages | Future |

Batch 16B already wired `.vpl-empty-state` on Sales CustomerHistory and Pricing history pages.

---

## Suggested PR/MR description

**Title:** Batch 16A: Home launcher and Inventory empty-state polish

```markdown
## Summary

Batch 16A applies Batch 14 VPURELUX visual helpers to deferred Home launcher density and Inventory inquiry empty states:

- Home: `.vpl-page`, `.vpl-card-dense`, `.vpl-page-subtitle`, tighter `Index.css` spacing
- Inventory Ledger/Balances/Lots: `alert alert-light border` → `.vpl-empty-state`

Web-only Razor/CSS class/layout changes plus documentation. No backend, permission, route, handler, or business-rule changes.

## Checklist

- [x] Build passed
- [x] Web.Tests passed
- [x] No backend/domain/application/EF/migration/API changes
- [x] No business-rule changes
- [x] No permission changes
- [x] No route/handler/form binding changes
- [x] No Inventory posting/FIFO behavior changes
- [x] No Inventory query/filter behavior changes
- [x] No Home navigation destination changes
- [x] No module cards added or removed
- [x] No fake counts/metrics/summaries added
- [x] Inventory empty-state text and conditionals preserved
- [x] Visual changes are Razor/CSS class/layout-only plus documentation

## Test plan

- [x] `dotnet build VPureLux.slnx --no-restore`
- [x] `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build`
- [ ] Manual smoke: Home (authenticated/unauthenticated), Inventory Ledger/Balances/Lots with empty warehouse filter

## Deferred

Empty-state wiring outside Inventory inquiry trio, Sales Create/Edit context panels, BOM/Audit/Catalog/Pricing inline empty UX — see `docs/UI_HOME_EMPTY_STATE_BATCH_16A_PLAN.md`
```

---

**Wrap-up status:** PR summary complete; ready for MR/PR from `feature/ui-home-empty-state-batch-16a` → `main`.
