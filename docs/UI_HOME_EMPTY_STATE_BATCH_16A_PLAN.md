# Batch 16A — Home Launcher Density & Empty-State Polish — Audit & Implementation Plan

**Document type:** Audit / implementation plan (no code changes in 16A.1)  
**Branch:** `feature/ui-home-empty-state-batch-16a`  
**Base branch:** `main` (@ `6c78ca6` — Merge PR #14 Batch 16B history/detail chrome)  
**Audit date:** 2026-06-24  
**Owner:** Cursor Agent (Composer 2.5)

---

## 1. Executive summary

Batches 13–16B established VPURELUX branding, Batch 14 dense ERP tokens/helpers, Batch 15 list/index chrome, and Batch 16B Sales/Pricing history-detail chrome. **Batch 16A** (explicitly deferred in Batch 16B) addresses two remaining visual gaps:

1. **Home launcher density** — `Pages/Index.cshtml` still uses Batch 13 module card grid (`vpl-home-*` + `Index.css`) without Batch 14 `.vpl-card-dense` / page chrome alignment.
2. **Empty-state wiring** — Inventory inquiry trio still uses bulky `alert alert-light border` for zero-result states; `.vpl-empty-state` helper exists in `global-styles.css` but is not applied there (Sales CustomerHistory and Pricing history already wired in Batch 16B).

All work is **Web Razor/CSS presentation only** — class additions and minimal Home-specific CSS tuning. No backend, route, permission, or business-rule changes.

---

## 2. Exact branch name and base branch

| Item | Value |
|------|--------|
| **Working branch** | `feature/ui-home-empty-state-batch-16a` |
| **Base branch** | `main` @ `6c78ca6` (Merge PR #14 — Batch 16B) |
| **Prerequisite** | Batch 14 helpers present in `global-styles.css` on `main` (confirmed) |

---

## 3. Pages inspected

| Path | Inspected | Notes |
|------|-----------|-------|
| `Pages/Index.cshtml` | Yes — **16A.2 target** | Home launcher; no `Pages/Home/Index.cshtml` exists |
| `Pages/Index.css` | Yes — **16A.2 target** | Home-specific density styles |
| `wwwroot/global-styles.css` | Yes | `.vpl-empty-state`, `.vpl-card-dense`, tokens |
| `Pages/Catalog/**` | Yes | List pages polished; no page-level empty alerts |
| `Pages/Bom/**` | Yes | Product history table; no dedicated empty alert block |
| `Pages/Pricing/**` | Yes | History uses `.vpl-empty-state`; Index uses inline `text-muted` in cells |
| `Pages/Inventory/**` | Yes | Inquiry trio has `alert alert-light border` empty blocks |
| `Pages/Sales/**` | Yes | CustomerHistory uses `.vpl-empty-state`; Create/Edit use context alerts |
| `Pages/Audit/**` | Yes | Index shows empty table body only; no empty alert |
| Prior batch PR summaries | Yes | Deferred 16A items confirmed |

---

## 4. Home launcher current state

**File:** `src/VPureLux.Web/Pages/Index.cshtml`  
**Styles:** `src/VPureLux.Web/Pages/Index.css` (page-scoped via `@section styles`)

| Aspect | Current pattern |
|--------|-----------------|
| Layout | `.vpl-home-welcome` wrapper; hero card `.vpl-home-hero`; authenticated module grid `row g-3` |
| Module cards | 6× Bootstrap `.card.vpl-module-card` (Catalog, Pricing, BOM, Inventory, Sales, Audit) |
| Links | Existing `asp-page` routes only — no new destinations |
| Auth | Unauthenticated: welcome + login CTA; authenticated: module grid only |
| Batch 14 helpers | **Not used** — no `.vpl-page`, `.vpl-card-dense`, or `.vpl-page-header` |
| Density | Custom CSS: title `1.75rem`, module icon `2.5rem`, card text `0.925rem`; default Bootstrap card padding |
| Localization | Hero subtitle hardcoded Vietnamese; module titles/descriptions hardcoded Vietnamese (Batch 13 pattern) |

**Not on Home grid today:** Customers, CustomerGroups (menu-only modules — do **not** add without explicit product decision).

---

## 5. Home launcher proposed polish

Apply **existing helpers + minimal Index.css tuning** (no grid redesign, no new modules):

| Element | Proposed change |
|---------|-----------------|
| Outer wrapper | Optional `.vpl-page` on `.vpl-home-welcome` (or keep wrapper, add dense classes to cards) |
| Hero card | Add `.vpl-card-dense` to `.vpl-home-hero` |
| Module cards | Add `.vpl-card-dense` to each `.vpl-module-card` |
| Grid spacing | Reduce `g-3` → `g-2` if needed after dense cards (Razor-only) |
| Index.css | Tighten hero/module padding and icon size slightly using existing `--vpl-space-*` where practical — **only if dense cards insufficient** |
| Links / routes | **Unchanged** — same six `asp-page` targets |
| Permission | **N/A** — Home shows same links for all authenticated users (no per-module `@if` on Home today) |

**Do not:** add dashboard metrics, fake counts, new module tiles, or LeptonX overrides.

---

## 6. Empty-state candidates found (exact file paths)

| # | File | Pattern | Condition | Localization key |
|---|------|---------|-----------|------------------|
| 1 | `Pages/Inventory/Ledger.cshtml` | `alert alert-light border` | `Model.Items.Count == 0` | `Inventory:NoLedgerEntries` |
| 2 | `Pages/Inventory/Balances.cshtml` | `alert alert-light border` | `Model.Items.Count == 0` | `Inventory:NoBalances` |
| 3 | `Pages/Inventory/Lots.cshtml` | `alert alert-light border` | `Model.Items.Count == 0` | `Inventory:NoLots` |
| 4 | `Pages/Sales/CustomerHistory.cshtml` | `.vpl-empty-state` | Already Batch 16B | `Sales:NoPurchaseHistory` |
| 5 | `Pages/Pricing/Components/History.cshtml` | `.vpl-empty-state` | Already Batch 16B | `Pricing:NoVersion` |
| 6 | `Pages/Pricing/Products/History.cshtml` | `.vpl-empty-state` | Already Batch 16B | `Pricing:NoVersion` |
| 7 | `Pages/Sales/Create.cshtml` | `alert alert-light border` | Product context panel | **Not empty-state** — `data-sales-product-context` |
| 8 | `Pages/Sales/Edit.cshtml` | `alert alert-light border` | Product context panel | **Not empty-state** — `data-sales-product-context` |
| 9 | `Pages/Bom/Product.cshtml` | None | Empty table when no versions | No alert today |
| 10 | `Pages/Audit/Index.cshtml` | None | Empty table when no logs | No alert today |
| 11 | `Pages/Pricing/Index.cshtml` | Inline `text-muted` in cells | Per-row missing data | **Not page-level empty-state** |

---

## 7. Empty-state candidates that should be changed in Batch 16A

**Batch 16A.3 scope — replace existing inquiry empty alerts only:**

| File | Change |
|------|--------|
| `src/VPureLux.Web/Pages/Inventory/Ledger.cshtml` | `alert alert-light border` → `div.vpl-empty-state` (same `@L[...]` text, same `@if (Model.Items.Count == 0)` conditional) |
| `src/VPureLux.Web/Pages/Inventory/Balances.cshtml` | Same |
| `src/VPureLux.Web/Pages/Inventory/Lots.cshtml` | Same |

**Rationale:** Already-polished Batch 14 inquiry pages; tests assert empty localization keys (`InventoryPagesTests` empty-state routes). Class-only swap inside existing results card.

---

## 8. Empty-state candidates that should be deferred

| File / area | Reason deferred |
|-------------|-----------------|
| `Sales/Create.cshtml`, `Sales/Edit.cshtml` | Context/info panel with JS hooks — not zero-data empty state |
| `Bom/Product.cshtml` | No existing empty alert; adding block = new UX, not wiring |
| `Audit/Index.cshtml` | No existing empty alert; empty table only |
| `Catalog/*/Index.cshtml`, list index pages | No dedicated empty alert blocks today |
| `Pricing/Index.cshtml` | Inline cell placeholders, not page empty-state |
| `Sales/CustomerHistory`, `Pricing/*/History` | **Already done** in Batch 16B |
| Create/edit/modal pages | Form workflow — out of 16A scope |
| `global-styles.css` | Helper sufficient; no token changes expected |

---

## 9. Proposed target pattern per page

### Home (`Pages/Index.cshtml`)

| Helper | Apply? | Notes |
|--------|--------|-------|
| `.vpl-page` | Optional | On `.vpl-home-welcome` if spacing aligns with ERP pages |
| `.vpl-page-header` / `.vpl-page-title` | No | Home uses `h1` hero branding, not list-page header pattern |
| `.vpl-page-subtitle` | No | Keep `.vpl-home-subtitle` |
| `.vpl-card-dense` | **Yes** | Hero + each module card |
| `.vpl-empty-state` | N/A | No empty states on Home |
| `.vpl-table-dense` / `.vpl-btn-toolbar` | N/A | |

### Inventory inquiry empty swap (Ledger, Balances, Lots)

| Helper | Apply? | Notes |
|--------|--------|-------|
| `.vpl-page` etc. | **No change** | Already Batch 14 chrome |
| `.vpl-card-dense` | **No change** | Results card already dense |
| `.vpl-empty-state` | **Yes** | Replace alert wrapper only; preserve conditional and localization key |

---

## 10. Exact files proposed for implementation

| Batch | Files |
|-------|-------|
| **16A.2 Home launcher** | `src/VPureLux.Web/Pages/Index.cshtml`, `src/VPureLux.Web/Pages/Index.css` (only if dense classes insufficient) |
| **16A.3 Empty-state** | `src/VPureLux.Web/Pages/Inventory/Ledger.cshtml`, `Pages/Inventory/Balances.cshtml`, `Pages/Inventory/Lots.cshtml` |
| **16A.4 Wrap-up** | `docs/UI_HOME_EMPTY_STATE_BATCH_16A_PR_SUMMARY.md`; status line in this plan |
| **Tests (only if assertions break)** | `test/VPureLux.Web.Tests/Pages/InventoryPagesTests.cs` (empty-state tests assert localization text, not alert class) |

---

## 11. Explicit files that must not be touched

| Category | Paths / examples |
|----------|------------------|
| **Backend** | `src/VPureLux.Domain/**`, `Application/**`, `EntityFrameworkCore/**`, `HttpApi/**` |
| **PageModel logic** | All `*.cshtml.cs` |
| **JavaScript** | `Sales/Details.js`, `SalesProductContext.js`, `CatalogIndex.js`, etc. |
| **Sales Create/Edit** | Product context alerts — not empty-state scope |
| **Batch 16B pages** | Sales History/CustomerHistory/Details; Pricing history — already complete |
| **Other polished modules** | Catalog lists, Sales Index, Audit Index, Batch 15 list pages — no drive-by edits |
| **Menus / permissions** | `Menus/**`, authorization policies |
| **Themes / libs** | `Themes/**`, `wwwroot/libs/**` |
| **global-styles.css** | Avoid unless `.vpl-empty-state` proves insufficient (unlikely) |

---

## 12. Permission / business-rule safety notes

| Concern | Batch 16A approach |
|---------|-------------------|
| Home module links | **No change** — same six routes; no new navigation |
| Permission-gated menu items | **No change** — Home does not gate tiles today; do not add gates in 16A |
| Inventory inquiry filters | **No change** — GET bindings, warehouse/stock selectors preserved |
| Empty-state conditionals | **No change** — same `@if (Model.Items.Count == 0)` blocks |
| Empty-state text | **No change** — same `@L["Inventory:No*"]` keys |
| Data calculations | **No change** — display-only class swap |
| Sales/BOM/Pricing business rules | **Untouched** |

---

## 13. Proposed implementation batches

### Batch 16A.2 — Home launcher density

**Goal:** Align Home module launcher with Batch 14 dense card language.

**Touch:** `Pages/Index.cshtml`; `Pages/Index.css` only if needed.

**Changes:** `.vpl-card-dense` on hero and module cards; optional grid `g-2`; minimal CSS padding tweaks. Preserve auth branching, links, and hardcoded copy.

**Validation:** Manual smoke Home (authenticated/unauthenticated); `dotnet build VPureLux.slnx`

---

### Batch 16A.3 — Empty-state polish

**Goal:** Replace Inventory inquiry `alert alert-light border` with `.vpl-empty-state`.

**Touch:** `Pages/Inventory/Ledger.cshtml`, `Balances.cshtml`, `Lots.cshtml`.

**Changes:** Class-only swap inside existing results cards; conditionals and localization unchanged.

**Validation:** `dotnet test ... --filter FullyQualifiedName~Inventory`

---

### Batch 16A.4 — Final validation and PR summary

**Goal:** Full Web.Tests, `docs/UI_HOME_EMPTY_STATE_BATCH_16A_PR_SUMMARY.md`.

**Checklist:** Home Index, Inventory inquiry trio empty states; confirm Batch 16B and other modules unchanged.

---

## 14. Non-goals

- No backend, Domain, Application, EF Core, migration, or DB schema changes
- No API contract or PageModel query changes
- No business-rule changes
- No permission gate changes or new Home tile permissions
- No route, handler, or form field name / binding changes
- No create/edit/modal behavior or JS changes
- No hiding columns, data, or history entries
- No new navigation destinations or Home module tiles
- No new dashboard metrics, fake counts, or fake summaries
- No formatting changes unless separately approved
- No LeptonX rewrite or new UI framework
- No Audit/Catalog/BOM empty-state invention where no alert exists today
- No separate permission/security hardening in this visual PR

---

## 15. Recommended first implementation batch

**Start with Batch 16A.2 — Home launcher density.**

**Why:**

1. **Single cohesive surface** — one Razor file (+ optional CSS) vs three Inventory files.
2. **Highest visibility deferral** — called out since Batch 14 audit §7 item 2 and all subsequent batch PR summaries.
3. **No test coupling** — Inventory empty-state tests assert localization strings, not CSS classes; Home has no dedicated empty-state tests.
4. **Low behavioral risk** — class additions on existing cards; links unchanged.
5. **16A.3 is a mechanical follow-up** — three identical alert → `.vpl-empty-state` swaps after Home pattern is validated.

**Exit criteria for 16A.2:**

- Home hero and module cards use `.vpl-card-dense` (and optional spacing tweak)
- Same six module links and auth/login behavior
- `dotnet build VPureLux.slnx` succeeds

---

## References

- `docs/UI_VISUAL_REFINEMENT_BATCH_14_AUDIT.md` — Home density + empty-state deferrals (§7 items 2, 7)
- `docs/UI_VISUAL_REFINEMENT_BATCH_14_PR_SUMMARY.md` — `.vpl-empty-state` helper definition
- `docs/UI_LIST_CHROME_BATCH_15_PR_SUMMARY.md` — Home/empty-state deferred
- `docs/UI_HISTORY_DETAIL_CHROME_BATCH_16_PR_SUMMARY.md` — Batch 16A explicit deferral list
- `src/VPureLux.Web/wwwroot/global-styles.css` — helper definitions

---

**Status:** Batch 16A.1 audit/plan **complete**. Awaiting Batch 16A.2 Home launcher density.
