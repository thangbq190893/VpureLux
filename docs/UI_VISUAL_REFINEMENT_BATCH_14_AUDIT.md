# Batch 14.1 — UI Visual Refinement Audit & Design-System Plan

**Document type:** Audit / design-system proposal (implementation **not** started)  
**Branch:** `feature/ui-visual-refinement-batch-14`  
**Baseline:** `main` (Batch 13 merged via PR #10)  
**Audit date:** 2026-06-24  
**Owner:** Cursor Agent (Composer 2.5)

---

## 1. Executive summary

Batch 13 delivered VPURELUX branding, a home welcome page, inquiry card scaffolding on Inventory/Sales/Audit, and Vietnamese display formatting. The app is **functionally solid** but still reads as **default LeptonX + ad hoc Bootstrap** on most screens. User feedback is consistent: cards feel oversized, corners too round, spacing too loose, buttons too heavy, and tables lack ERP-grade density.

This audit finds a **split visual language**:

| Pattern | Pages using it | Pages still on legacy layout |
|---------|----------------|------------------------------|
| `vpl-inquiry-*` helpers (`global-styles.css`) | Inventory Ledger/Balances/Lots, Sales Index, Audit Index | — |
| `vpl-home-*` (`Index.css`) | Home welcome | — |
| Raw `abp-card` + `h2` in header | — | Catalog Products/Components, Customers, BOM, most forms |
| Bare `h2` + list-group / tabs | — | Inventory hub, Pricing Index |

**Recommendation:** Do **not** redesign. Introduce a small **VPURELUX ERP token layer** in `global-styles.css` (CSS custom properties + utility classes), then apply in phased batches. Prefer global tokens over per-page one-off tweaks. Keep ABP/LeptonX components (`abp-card`, `abp-table`, tag helpers) and all permission/business behavior unchanged.

**First implementation batch:** **14.2 — Global visual tokens** (foundation for all later batches).

---

## 2. Current UI problems

Problems are ordered by user impact and cross-page frequency.

1. **Inconsistent page chrome** — Three header patterns coexist: external `vpl-inquiry-page-title` (Inventory/Sales/Audit), `h2` inside `abp-card-header` (Catalog, Customers), bare `h2` (Pricing, Inventory hub, Receipt/Issue). Operators perceive “patched” screens.
2. **Oversized cards** — LeptonX/Bootstrap default card padding stacks with Batch 13 `.vpl-inquiry-*` padding (`0.75rem` header / `1rem` body in `global-styles.css`). Catalog wraps title, filter, and table in one card (`Catalog/Products/Index.cshtml`, `Catalog/Components/Index.cshtml`), increasing visual mass.
3. **Border radius too soft** — LeptonX theme defaults plus local uses of `rounded` (home module icons `0.5rem` in `Index.css`, catalog thumbnails, inventory line rows `border rounded p-3` in `Receipt.cshtml`) reinforce a consumer/marketing feel rather than internal ERP.
4. **Loose vertical rhythm** — Frequent `mb-3` / `mb-4`, home grid `g-3`, double-card inquiry layout (filter card + results card each `mb-3`), and hero `mb-4` on home add scroll without information gain.
5. **Heavy buttons** — Full-size `btn btn-primary` for list actions and toolbars; Sales Details mixes `btn-primary`, `btn-success`, `btn-danger` at default size (`Sales/Details.cshtml`). Table row actions use `btn-sm` inconsistently (Sales Index `btn-outline-primary` vs Audit `btn-outline-secondary`).
6. **Tables lack density** — All audited lists use `<abp-table striped-rows="true">` without compact row height. Catalog forces 56×56px thumbnails (`width:72px` column, inline `style` in Products/Components Index), inflating row height. Audit Index packs badges + two-line cells per row without typographic hierarchy tuning.
7. **Filter/action bar inconsistency** — Audit search uses `btn btn-secondary`; Inventory filters use `btn btn-primary` + `btn-outline-secondary`; Catalog uses `<abp-button button-type="Secondary">`. Same operator task, different visual weight.
8. **Monolithic vs split cards** — Polished inquiry pages split filter/results; Catalog/Customers keep filter inside the same card body as the table. Feels like two different products.
9. **Home module cards read “marketing”** — Six large cards with icon tiles, descriptions, and stretched links (`Index.cshtml` + `Index.css`) resemble a SaaS landing grid, not a compact ERP launcher.
10. **Residual formatting/visual drift** — Pricing Index still shows `N0` + currency code (`Pricing/Index.cshtml`) while Catalog/Sales use Vietnamese `₫` formatting — a polish inconsistency visible during cross-module demos.

---

## 3. Design-system direction for VPURELUX ERP

**North star:** A **calm, dense, internal operations console** — closer to classic ERP back-office than LeptonX demo/ecommerce defaults.

| Principle | Direction |
|-----------|-----------|
| Density | More rows per viewport; less decorative whitespace |
| Shape | Subtle corners (≤ 6px); avoid pill-like cards |
| Color | Keep VPURELUX water/login gradients and logo; reduce saturated button blocks in data-heavy views |
| Hierarchy | One clear page title pattern; filters visually subordinate to results |
| Consistency | Extend `vpl-*` tokens to Catalog and remaining list pages **without** rewriting markup semantics |
| Compatibility | Override via `global-styles.css` + minimal Razor class additions only; no new UI framework |
| Operator-first | Readable Vietnamese labels, scannable tables, obvious primary action — no hidden data |

**Visual metaphor:** Ledger paper on a clean desk — bordered regions, tight grids, light shadows, not floating marketing cards.

---

## 4. Proposed visual tokens

Tokens are **targets for Batch 14.2+**. Implement as `:root` / `.vpl-erp` custom properties in `global-styles.css`, consumed by existing and new helper classes.

### Border radius

| Token | Target | Notes |
|-------|--------|-------|
| `--vpl-radius-sm` | `0.25rem` (4px) | Inputs, thumbnails, inline chips |
| `--vpl-radius-md` | `0.375rem` (6px) | Cards, tables wrappers, filter panels |
| `--vpl-radius-lg` | `0.5rem` (8px) | Home launcher tiles only (optional) |

Override LeptonX/Bootstrap `--bs-border-radius*` where safe globally, not per-widget hacks.

### Spacing scale

| Token | Target | Use |
|-------|--------|-----|
| `--vpl-space-1` | `0.25rem` | Tight inline gaps |
| `--vpl-space-2` | `0.5rem` | Toolbar gaps, table cell padding-y |
| `--vpl-space-3` | `0.75rem` | Card header padding |
| `--vpl-space-4` | `1rem` | Card body padding (max for dense views) |
| `--vpl-space-5` | `1.25rem` | Page section separation (replace `mb-4` habit) |

Reduce default `mb-3` between inquiry cards to `--vpl-space-3` where stacked.

### Card density

| Element | Current (observed) | Target |
|---------|------------------|--------|
| Card header padding | LeptonX default + `0.75rem` (`.vpl-inquiry-filter-card`) | `0.5rem 0.75rem` |
| Card body padding | `1rem` (`.vpl-inquiry-*`) | `0.625rem 0.875rem` |
| Filter card | Separate card with titled header | Keep split layout; reduce header to `text-sm` section label |
| Results card | Body-only header | No redundant empty card headers |

New helpers (proposed names): `.vpl-card-dense`, `.vpl-page`, `.vpl-page-header`, `.vpl-page-title` — unify inquiry + catalog list chrome.

### Table density

| Element | Target |
|---------|--------|
| Row height | ~36–40px data rows (catalog thumbnails excepted) |
| Cell padding | `0.375rem 0.5rem` |
| Font size | `0.875rem` body, `0.8125rem` secondary lines |
| Header | `0.75rem` uppercase or semibold, muted color |
| Striping | Keep `striped-rows`; optional lighter stripe contrast |
| Thumbnails | 40×40px max in catalog lists (from 56×56) |

New helper: `.vpl-table-dense` wrapping `abp-table` output (via parent class + CSS targeting `.table`).

### Button sizing

| Context | Target |
|---------|--------|
| Toolbar primary | `btn btn-sm btn-primary` or `btn` with reduced padding via token |
| Toolbar secondary | `btn btn-sm btn-outline-secondary` |
| In-table actions | `btn btn-sm btn-outline-secondary` (consistent; avoid mix of primary/secondary per module) |
| Destructive/confirm | Keep semantic colors; use `btn-sm` on Details toolbars |
| Filter submit | Secondary weight unless sole CTA |

New helper: `.vpl-btn-toolbar` for flex gap + sm sizing defaults.

### Typography hierarchy

| Level | Target | Example |
|-------|--------|---------|
| Page title | `1.25rem`, `font-weight: 600` | Replace mixed `h2` / `1.5rem` `.vpl-inquiry-page-title` |
| Card section title | `0.9375rem`, `font-weight: 600` | Filter card “Search” |
| Body | `0.875rem` | Table cells, form labels |
| Muted meta | `0.8125rem`, `text-muted` | Audit event name, entity type sublines |
| Home hero title | `1.5rem` (down from `1.75rem`) | `Index.css` `.vpl-home-title` |

### Shadow / border usage

| Rule | Direction |
|------|-----------|
| Cards | Prefer `1px` border (`--bs-border-color` / subtle `#e5e7eb`) over elevation shadow |
| Hover (home tiles) | Border-color shift only (already in `Index.css`); no extra shadow |
| Dropdowns/modals | Keep Bootstrap defaults; do not restyle in Batch 14 |
| Login shell | Keep Batch 13 gradients; no structural Account page changes |

---

## 5. Page-by-page audit

### Home (`Pages/Index.cshtml`, `Pages/Index.css`)

| Aspect | Finding |
|--------|---------|
| Layout | Hero card + 6-column module grid (`col-md-6 col-lg-4`, `g-3`) |
| Issues | Cards tall (icon + title + paragraph + link); `mb-4` on hero; module icon `2.5rem` tile with `0.5rem` radius feels app-store-like |
| Files likely affected | `Index.cshtml`, `Index.css`, possibly `global-styles.css` for shared launcher tokens |
| Proposal | Compact launcher: shorter copy or single-line descriptions; `g-2`; smaller icons; optional 2-row grid on wide screens; reduce hero padding via `.vpl-card-dense` |

### Login / account shell (`wwwroot/global-styles.css` — `.abp-account-layout`, `--lpx-theme-*-bg`)

| Aspect | Finding |
|--------|---------|
| Layout | CSS-only water gradients (Batch 13); no Account Razor overrides |
| Issues | Gradients are on-brand but may feel “marketing” if paired with large LeptonX login card — out of scope to change Account markup |
| Files likely affected | `global-styles.css` only (optional: slightly desaturate gradient stops) |
| Proposal | **Nice-to-have** token tweak only; do not touch Account pages in Batch 14 |

### Catalog Products (`Pages/Catalog/Products/Index.cshtml`)

| Aspect | Finding |
|--------|---------|
| Layout | Single `abp-card`: header with `h2` + create button, inline GET filter, full table |
| Issues | Monolithic card; 56px thumbnails + `width:72px` inline style; heavy header row; `abp-button` vs `btn` mix; no `vpl-inquiry-*` |
| Files likely affected | `Index.cshtml` (class additions only), `global-styles.css`, `CatalogIndex.js` unchanged |
| Proposal | Apply `.vpl-page` header pattern; `.vpl-table-dense`; shrink thumbnails; optional split filter row (class-only restyle before structural split) |

### Catalog Components (`Pages/Catalog/Components/Index.cshtml`)

| Aspect | Finding |
|--------|---------|
| Layout | Same as Products |
| Issues | Same as Products; suggested price column already Vietnamese-formatted |
| Files likely affected | Same as Products |
| Proposal | Mirror Products batch 14.5 changes for parity |

### Inventory Ledger / Balances / Lots (`Pages/Inventory/Ledger.cshtml`, `Balances.cshtml`, `Lots.cshtml`)

| Aspect | Finding |
|--------|---------|
| Layout | `vpl-inquiry-page` + filter card + results card; labels on Ledger/Balances/Lots filters |
| Issues | Double card stack adds height; `vpl-inquiry-page-title` at `1.5rem`; primary filter buttons; table not dense; empty state `alert alert-light border` is bulky |
| Files likely affected | Three `.cshtml` files (minimal), `global-styles.css` |
| Proposal | Tighten via global tokens on `.vpl-inquiry-*`; table density batch; unify empty state to compact `.vpl-empty-state` |

### Sales Index (`Pages/Sales/Index.cshtml`)

| Aspect | Finding |
|--------|---------|
| Layout | `vpl-inquiry-page` toolbar + results card only (no filter card) |
| Issues | Toolbar full-size buttons; results card body-only; table default density; `btn-outline-primary` on Details |
| Files likely affected | `Index.cshtml`, `global-styles.css` |
| Proposal | `.vpl-btn-toolbar` sm sizing; `.vpl-table-dense`; align Details button style with Audit (`outline-secondary`) |

### Sales Details (`Pages/Sales/Details.cshtml`)

| Aspect | Finding |
|--------|---------|
| Layout | Bare `h2` header row + `dl.row` + line table + back button |
| Issues | Not using inquiry pattern; large action buttons (Edit/Confirm/Cancel); no card wrapper around summary — feels unlike Index |
| Files likely affected | `Details.cshtml`, `global-styles.css` |
| Proposal | Optional `.vpl-page-header` + compact summary card (visual only); `btn-sm` on actions; dense line table — **should-fix after demo** |

### Audit Index (`Pages/Audit/Index.cshtml`)

| Aspect | Finding |
|--------|---------|
| Layout | Full inquiry pattern with filter + results |
| Issues | Dense content (badges + 2-line cells) in non-dense table; `btn-secondary` search; filter grid `g-2` good but card chrome heavy |
| Files likely affected | `Index.cshtml`, `global-styles.css` |
| Proposal | Table density + typographic meta lines; token-tighten cards; keep badge colors (semantic) |

---

## 6. Must-fix before demo

Items with highest visibility when demoing VPURELUX ERP to stakeholders:

1. **Global card/table density tokens (14.2 + 14.3)** — Immediately reduces “boxes too large” feedback on Inventory, Sales, Audit, Catalog.
2. **Unify page title + toolbar pattern on Catalog Products/Components (14.5)** — Biggest non-inquiry modules still look legacy next to polished inquiry pages.
3. **Toolbar button downsizing (14.4)** — Sales Index, Audit Index, Catalog create headers.
4. **Catalog thumbnail / row height (14.3)** — Products list is often demoed first; 56px rows dominate the viewport.
5. **Consistent in-table action button style (14.4)** — Stop mixing `outline-primary` vs `outline-secondary` on Details links.

---

## 7. Should-fix after demo

1. **Sales Details** header/summary card alignment with inquiry visual language.
2. **Home launcher density** — shorter module cards, tighter grid.
3. **Extend `vpl-page` pattern to Customers, CustomerGroups, BOM Index** (list pages outside audit scope but visible in menu).
4. **Pricing Index** — tabs + tables without cards; apply table density + Vietnamese money display alignment (display-only, separate from Batch 13 Catalog/Sales formatters).
5. **Inventory hub** (`Inventory/Index.cshtml`) — replace plain list-group with compact link panel matching ERP nav density.
6. **Filter button semantics** — standardize Apply/Search to secondary weight, primary reserved for create/export.
7. **Empty states** — replace bulky `alert alert-light border` with `.vpl-empty-state` compact text.

---

## 8. Nice-to-have items

1. Slightly desaturate login background gradients (`global-styles.css` `--lpx-theme-*-bg`).
2. Home module icon tiles: square → smaller circle or flat icon without background box.
3. Audit Details / BOM Details card stack density (many nested `abp-card` in `Audit/Details.cshtml`).
4. Form pages (Receipt/Issue/Adjustment): compact `border rounded p-3` line rows.
5. Warehouses admin table density.
6. Dark-theme token verification for new radius/spacing overrides.
7. Document token reference in a short `docs/UI_DESIGN_TOKENS.md` after 14.2 lands.

---

## 9. Safe implementation batches

### Batch 14.2 — Global visual tokens

**Goal:** Add `--vpl-*` CSS variables and base helpers in `global-styles.css`.

| Touch | Files |
|-------|-------|
| Primary | `src/VPureLux.Web/wwwroot/global-styles.css` |
| Optional | `src/VPureLux.Web/Pages/Index.css` (home-only overrides) |

**Deliverables:** `--vpl-radius-*`, `--vpl-space-*`, `.vpl-card-dense`, `.vpl-page-title`, `.vpl-table-dense` (table child selectors), `.vpl-btn-toolbar`, `.vpl-empty-state`.

**Safety:** No Razor logic changes; no JS. Web.Tests should pass unchanged if selectors are additive.

---

### Batch 14.3 — Table density polish

**Goal:** Apply `.vpl-table-dense` to audited list tables; reduce catalog thumbnail size.

| Touch | Files |
|-------|-------|
| Razor (class only) | `Catalog/Products/Index.cshtml`, `Catalog/Components/Index.cshtml`, `Inventory/Ledger.cshtml`, `Balances.cshtml`, `Lots.cshtml`, `Sales/Index.cshtml`, `Audit/Index.cshtml` |
| CSS | `global-styles.css` |
| Tests | Update HTML assertions in `CatalogPagesTests.cs`, `InventoryPagesTests.cs`, `SalesPagesTests.cs`, `AuditPagesTests.cs` **only if** class names change |

**Safety:** No column add/remove; no data hiding; permission gates untouched.

---

### Batch 14.4 — Button / action polish

**Goal:** Smaller toolbar and row actions; consistent outline-secondary for Details.

| Touch | Files |
|-------|-------|
| Razor | `Sales/Index.cshtml`, `Audit/Index.cshtml`, `Catalog/*/Index.cshtml`, optionally `Sales/Details.cshtml` |
| CSS | `global-styles.css` (`.vpl-btn-toolbar`) |

**Safety:** No new actions; no permission changes; button types remain semantic (primary/create, danger/cancel).

---

### Batch 14.5 — Page header / card density polish

**Goal:** Align Catalog and inquiry pages to shared `.vpl-page` chrome; tighten `.vpl-inquiry-*` padding via tokens.

| Touch | Files |
|-------|-------|
| Razor | `Catalog/Products/Index.cshtml`, `Catalog/Components/Index.cshtml`, `Inventory/Ledger.cshtml`, `Balances.cshtml`, `Lots.cshtml`, `Sales/Index.cshtml`, `Audit/Index.cshtml` |
| CSS | `global-styles.css`, `Index.css` (home) |

**Safety:** Layout structure may gain wrapper classes; no form field or handler changes.

---

### Batch 14.6 — Final validation

**Goal:** Full Web.Tests, build, manual smoke checklist, PR summary doc.

| Touch | Files |
|-------|-------|
| Docs | `docs/UI_VISUAL_REFINEMENT_BATCH_14_PR_SUMMARY.md` (future) |
| Tests | Run full `VPureLux.Web.Tests`; adjust only if assertions break on class/CSS |

**Checklist:** Home, login background, Catalog lists, Inventory inquiry trio, Sales Index/Details, Audit Index, sidebar logo/footer unchanged functionally.

---

## 10. Explicit non-goals

- No backend, Domain, Application, EF Core, migration, or DB schema changes
- No API contract or DTO changes
- No business-rule changes (Inventory FIFO/posting, Sales calculations, Pricing logic, Catalog lifecycle)
- No permission gate changes or data exposure (Sales cost/profit, Catalog pricing context)
- No new UI framework (React, Blazor SPA, etc.)
- No LeptonX package rewrite or `wwwroot/libs/` fork
- No ecommerce/marketing redesign (no hero carousels, pricing pages, or landing funnels)
- No requirement for screenshots or video evidence in this batch
- No hiding table columns or reducing data visibility without explicit approval
- No Account/login Razor markup changes in initial batches

---

## 11. Risk and rollback notes

| Risk | Mitigation |
|------|------------|
| LeptonX upgrade overrides custom CSS | Keep overrides in `global-styles.css` with narrow selectors; document `--vpl-*` tokens |
| Global Bootstrap variable changes affect modals/admin | Scope radius/spacing overrides to `.lpx-content` / `.vpl-*` containers first |
| Web.Tests assert exact HTML | Prefer additive classes; update tests only when assertions fail |
| Catalog thumbnail resize breaks layout tests | Change width/height together; run `CatalogPagesTests` after 14.3 |
| Dark theme regressions | Verify logo, cards, tables in dim/dark after token pass |
| Perceived “too cramped” | Tokens are adjustable; ship 14.2 alone for stakeholder preview |

**Rollback:** Each batch is one or few commits. Revert commit or remove `global-styles.css` token block to restore Batch 13 appearance. No migrations or data impact.

---

## 12. Recommended first implementation batch

**Start with Batch 14.2 — Global visual tokens.**

**Why first:**

1. Single file primary touch (`global-styles.css`) — lowest blast radius.
2. Unblocks 14.3–14.5 without re-deciding spacing/radius per page.
3. Delivers immediate density improvement on existing `.vpl-inquiry-*` pages before Catalog Razor edits.
4. Easy to review in isolation (CSS-only diff).
5. Aligns with Batch 13 plan deferred approach: “define token-level targets before code changes.”

**Exit criteria for 14.2:**

- `--vpl-*` tokens documented in CSS comments
- Helper classes available but not required on all pages yet
- `dotnet build VPureLux.slnx` succeeds
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj` — all pass with zero or minimal test updates

---

## References

- `docs/UI_BRAND_POLISH_BATCH_13_PLAN.md` — Batch 14 deferred section
- `docs/UI_BRAND_POLISH_BATCH_13_PR_SUMMARY.md` — Batch 13 scope and safety notes
- `src/VPureLux.Web/wwwroot/global-styles.css` — current `vpl-inquiry-*` and branding
- `src/VPureLux.Web/Pages/Index.cshtml`, `Index.css` — home welcome
- `src/VPureLux.Web/Menus/VPureLuxMenuContributor.cs` — module navigation (no visual changes needed)

---

**Status:** Batch 14.1 audit **complete**. Batch 14.2 global visual tokens **complete** — opt-in `--vpl-*` variables and helper classes added to `global-styles.css` (no page wiring yet).
