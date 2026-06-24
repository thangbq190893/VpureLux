# Batch 13 — VPURELUX UI Brand Polish Plan (Recovery)

**Document type:** Recovery / reimplementation plan  
**Branch:** `feature/ui-brand-polish-batch-13-recovery`  
**Baseline:** `origin/main` @ `91c233b` (merged UAT branch `feature/audit-ui-ux-batch-3`)  
**Status:** Recovery in progress — prior Batch 13 commits were lost (branch `feature/ui-brand-polish-batch-13` never pushed)

---

## Recovery context

The original Batch 13 implementation (`5474271` … `a599f70`) existed only in a prior agent workspace and is **not** in this clone. This branch re-implements the same scoped UI work incrementally, one sub-batch per commit, with Web.Tests validation after each phase.

**Do not** treat `docs/UI_BRAND_POLISH_BATCH_13_PR_SUMMARY.md` from commit `c40d257` (on `feature/audit-ui-ux-batch-3`) as code — add PR summary only at wrap-up after all phases are restored.

---

## Design direction (unchanged)

- Keep ABP LeptonX side-menu ERP layout.
- Light VPURELUX branding: local logo assets, footer, subtle water-themed login CSS, operator welcome home.
- Inquiry pages: ABP card layout for filters/results.
- Vietnamese display formatting for money (`NNN.NNN ₫`), quantities, dates (`dd/MM/yyyy`) — **display only**.
- No backend, Domain, Application, EF, migrations, API, permissions, or business-rule changes.

---

## Implementation phases

| Phase | Goal | Primary touch points | Exit criteria |
|-------|------|----------------------|---------------|
| **13.1** | Recovery plan (this document) | `docs/UI_BRAND_POLISH_BATCH_13_PLAN.md` | Plan committed on recovery branch |
| **13.2** | Logo + footer branding | `wwwroot/images/logo/vpurelux/*`, `global-styles.css`, `Themes/.../_Footer.cshtml` | Sidebar logo + VPURELUX footer visible |
| **13.3a** | Home welcome page | `Pages/Index.cshtml`, `Pages/Index.css` | No ABP getting-started; module shortcut cards |
| **13.3b** | Login background CSS | `global-styles.css` (gradient theme vars + login override) | Login readable; no Account Razor changes |
| **13.4a** | Inventory inquiry layout | `Inventory/Ledger`, `Balances`, `Lots` `.cshtml`, `global-styles.css` (`.vpl-inquiry-*`) | Card layout; filters unchanged |
| **13.4b** | Sales Index layout | `Sales/Index.cshtml` | Header + results cards |
| **13.4c** | Audit Index layout | `Audit/Index.cshtml` | Header + filter + results cards |
| **13.5a** | Inventory value formatting | `Inventory/Ledger`, `Balances`, `Lots` `.cshtml`, `InventoryPagesTests.cs` | No raw decimals like `41000,000000` |
| **13.5b** | Sales value formatting | `Sales/Index`, `Sales/Details` `.cshtml`, `SalesPagesTests.cs` | Cost/profit permission checks unchanged |
| **13.5c** | Catalog price formatting | `Catalog/Products/Index`, `Components/Index` `.cshtml`, `CatalogPagesTests.cs` | Suggested prices as `155.000 ₫` |
| **Wrap-up** | Validation + PR summary | `docs/UI_BRAND_POLISH_BATCH_13_PR_SUMMARY.md`, full Web.Tests | Build + Web.Tests pass; PR ready |

---

## Explicit do-not rules

1. Do not hotlink assets from `vpurelux.com`.
2. Do not change login/Account Razor markup or auth behavior.
3. Do not recalculate FIFO, inventory value, revenue, cost, or profit in views.
4. Do not change PageModel query logic, permissions, or API contracts.
5. Do not edit `wwwroot/libs/` or LeptonX package sources.
6. One sub-batch per commit; validate before next phase.

---

## Validation per phase

| Check | Command |
|-------|---------|
| Build | `dotnet build VPureLux.slnx --no-restore` |
| Web tests | `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build` |
| Module filter (when applicable) | `--filter "FullyQualifiedName~Inventory\|Sales\|Catalog\|Audit\|Index"` |

Full solution tests optional; known unrelated EF Audit SQLite seed flakiness may appear in non-Web projects.

---

## Rollback

Each sub-batch is an isolated commit. Revert individual commits or restore files from `main` — no database migration rollback required.

---

## References

- `UI_FIX_BACKLOG.md` — Batch 13 entry
- `docs/V2_UAT_FUNCTIONAL_SIGN_OFF.md` — deferred polish note
- UAT catalog codes: `UAT_UI_20260622_1809_P`, `UAT_UI_20260622_1809_C`

---

## Deferred — Batch 14: UI Visual Refinement / Design System Pass

**Status:** Deferred — audit-first; implementation only after stakeholder approval.

**User feedback (captured for future pass):**
- Boxes/cards feel too large
- Border radius too rounded
- Spacing too loose
- Buttons feel heavy
- Many screens still visually rough overall

**Approach:**
1. Audit current LeptonX + VPURELUX screens (home, inquiry pages, forms, modals) and produce a short design-system proposal.
2. Define token-level targets (card padding, radius, button size, grid gaps) before any code changes.
3. Implement in small scoped batches after approval — no drive-by refactors during functional recovery work.

---

**Status:** Batch 13.1 recovery plan complete (`c7c11d3`). **Batch 13.2 recovery complete** (`0655f36`) — human-approved local VPURELUX logo assets, CSS variables, ERP footer. **Batch 13.3a recovery complete** (`905f82c`) — VPURELUX ERP welcome home page with module shortcut cards. **Batch 13.3b recovery complete** (`8106a5f`) — subtle water-themed login background CSS (gradients only). **Batch 13.4a recovery complete** (`1270dd7`) — Inventory inquiry card layout (`Ledger`, `Balances`, `Lots`). **Batch 13.4b recovery complete** (`a2d8551`) — Sales Index inquiry card layout. **Batch 13.4c recovery complete** (`afa5314`) — Audit Index inquiry card layout. **Batch 13.5a recovery complete** (`9f834da`) — Inventory Vietnamese display formatting. **Batch 13.5b recovery complete** (`5bf8bba`) — Sales Vietnamese display formatting. **Batch 13.5c recovery complete** — Catalog suggested price Vietnamese display formatting. Wrap-up **pending reimplementation**. Batch 14 UI Visual Refinement **deferred** (see above).
