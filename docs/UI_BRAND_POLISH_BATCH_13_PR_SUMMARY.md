# Batch 13 — VPURELUX UI Brand Polish (Recovery) — PR Summary

**Title:** feat(web): VPURELUX UI brand polish — branding, inquiry layout, Vietnamese display formatting  
**Branch:** `feature/ui-brand-polish-batch-13-recovery`  
**Base branch:** `main` (@ `91c233b`)  
**Document type:** Merge-request / pull-request summary (recovery reimplementation)

---

## Scope summary

This branch reimplements **Batch 13 UI Brand Polish** after the original feature branch was lost before push. Work is scoped to **Web Razor/CSS presentation** only:

- VPURELUX logo assets and LeptonX theme variable overrides
- ERP footer replacement
- Operator home welcome page (replacing ABP getting-started marketing)
- Subtle water-themed login background (CSS gradients only)
- Inquiry-page card layout polish (Inventory, Sales Index, Audit Index)
- Vietnamese **display-only** formatting for money, quantities, and dates on selected inquiry/list pages
- Batch 14 deferred UI Visual Refinement backlog captured in plan doc

**No backend, Domain, Application, EF Core, migration, API contract, permission, or business-rule changes.**

---

## Commit list (oldest → newest)

| Commit | Message |
|--------|---------|
| `c7c11d3` | docs: restore Batch 13 UI brand polish plan |
| `905f82c` | feat(web): replace default home page with VPURELUX ERP welcome |
| `0655f36` | feat(web): add VPURELUX logo and footer branding |
| `8106a5f` | feat(web): add subtle VPURELUX login background branding |
| `1270dd7` | feat(web): polish Inventory inquiry page layout |
| `a2d8551` | feat(web): polish Sales index inquiry layout |
| `afa5314` | feat(web): polish Audit index inquiry layout |
| `9f834da` | feat(web): format Inventory inquiry values for Vietnamese display |
| `5bf8bba` | feat(web): format Sales values for Vietnamese display |
| `bea8af8` | feat(web): format Catalog prices for Vietnamese display |
| _(wrap-up, latest)_ | docs: add Batch 13 UI brand polish PR summary |

---

## Files changed by area (`main...HEAD`)

| Area | Files |
|------|-------|
| **Docs** | `docs/UI_BRAND_POLISH_BATCH_13_PLAN.md`, `docs/UI_BRAND_POLISH_BATCH_13_PR_SUMMARY.md` |
| **Branding / theme** | `wwwroot/global-styles.css`, `wwwroot/images/logo/vpurelux/*`, `Themes/LeptonX/Layouts/Application/_Footer.cshtml` |
| **Home** | `Pages/Index.cshtml`, `Pages/Index.css` |
| **Inventory inquiry** | `Pages/Inventory/Ledger.cshtml`, `Balances.cshtml`, `Lots.cshtml` |
| **Sales inquiry** | `Pages/Sales/Index.cshtml`, `Pages/Sales/Details.cshtml` |
| **Audit inquiry** | `Pages/Audit/Index.cshtml` |
| **Catalog list** | `Pages/Catalog/Products/Index.cshtml`, `Pages/Catalog/Components/Index.cshtml` |
| **Web tests** | `InventoryPagesTests.cs`, `SalesPagesTests.cs`, `CatalogPagesTests.cs` |

**Total:** 19 paths changed vs `main` before wrap-up doc commit (1 plan doc added, 3 logo PNGs added, 15 modified). Wrap-up adds `docs/UI_BRAND_POLISH_BATCH_13_PR_SUMMARY.md` and updates plan status only.

---

## What changed

### Branding (13.2, 13.3b)
- Local VPURELUX logo PNGs under `wwwroot/images/logo/vpurelux/`
- `--lpx-logo` / `--lpx-logo-icon` CSS variables point to local assets (light/dark)
- Footer: `© {year} VPURELUX ERP`, internal tagline, Privacy link only
- Login/unauthenticated shell: water-themed **CSS gradients** via `--lpx-theme-*-bg` and `.abp-account-layout` overrides

### Home (13.3a)
- Replaced ABP getting-started/marketing home with VPURELUX ERP operator welcome + module shortcut cards (Vietnamese copy)

### Inquiry layout (13.4a–c)
- Shared `.vpl-inquiry-*` helpers in `global-styles.css`
- Inventory Ledger/Balances/Lots: page header, filter card, results card
- Sales Index: header toolbar + results card (no new filter form)
- Audit Index: header toolbar, filter card, results card

### Vietnamese display formatting (13.5a–c) — **display only**
- **Inventory:** money (`41.000 ₫`), quantity (`7,25`), ledger dates (`dd/MM/yyyy`)
- **Sales:** revenue/cost/profit/prices/quantities/dates on Index + Details (cost/profit remain permission-gated)
- **Catalog:** suggested selling prices as `150.000 ₫` (replacing raw `N0 VND` in list columns)

---

## What explicitly did not change

- **Backend / Domain / Application / EF Core / migrations / DB schema**
- **API contracts** and **PageModel query logic**
- **Permissions** and authorization policies
- **Business rules:** Inventory posting, FIFO, balances, lots, ledger calculations
- **Sales:** order create/confirm/cancel, revenue/cost/profit **calculations**
- **Pricing** module behavior; Catalog create/edit/details/image/status flows
- **Login / Account** Razor markup and authentication behavior
- **Receipt, Issue, Adjustment** inventory forms
- LeptonX package sources and `wwwroot/libs/`

Layout polish is **Razor/CSS-only**. Formatting is **Razor display-only** (local `@functions` helpers); stored values and DTOs unchanged.

---

## Permission / business-rule safety notes

| Concern | Status |
|---------|--------|
| Permission gates removed | **No** — all existing `@if` / `Authorize` checks preserved |
| Sales cost/profit exposed without permission | **No** — `canViewCost` / `canViewProfit` unchanged on Details |
| Catalog pricing visibility | **No change** — `CanViewPricingContext` still gates price columns |
| Inventory posting / FIFO | **No change** — display formatting only in inquiry views |
| Catalog/Pricing business behavior | **No change** — suggested price **display** only on list pages |
| Purchase-cost wording in Catalog | **Not introduced** — component input cost remains Inventory Receipt domain |

---

## Test results

Wrap-up validation (2026-06-24):

```bash
dotnet build VPureLux.slnx --no-restore
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build
```

| Check | Result |
|-------|--------|
| Build | **Succeeded** — 0 errors, 3 pre-existing warnings (Domain nullability, Test SDK entry point) |
| Full Web.Tests | **Passed** — 120/120, 0 failed, 0 skipped (~3m 35s) |

Per-phase filters were also run during recovery: `Inventory` (31), `Sales` (15), `Catalog` (20), `Audit` (11).

---

## Known deferred items — Batch 14

Captured in `docs/UI_BRAND_POLISH_BATCH_13_PLAN.md`:

**Batch 14 — UI Visual Refinement / Design System Pass** (audit-first; implement after approval)

- Cards/boxes feel too large
- Border radius too rounded
- Spacing too loose
- Buttons feel heavy
- Many screens still visually rough

No Batch 14 code in this MR.

---

## Suggested PR/MR description

```markdown
## Summary

Recovers Batch 13 VPURELUX UI brand polish on `feature/ui-brand-polish-batch-13-recovery` after the original branch was lost before push.

Scoped Web-only changes:
- VPURELUX logo + ERP footer branding
- Operator home welcome page (replaces ABP marketing)
- Subtle water-themed login background (CSS gradients)
- Inquiry page card layout (Inventory, Sales Index, Audit Index)
- Vietnamese display formatting for Inventory/Sales/Catalog list views

## Safety

- No backend, Domain, Application, EF, migration, or API changes
- No business rule or permission changes
- Sales cost/profit remain permission-gated
- Formatting is display-only; layout is Razor/CSS-only

## Test plan

- [x] `dotnet build VPureLux.slnx --no-restore`
- [x] `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build`
- [ ] Manual smoke: home, login background, sidebar logo, footer, Inventory/Sales/Audit/Catalog inquiry pages

## Deferred

Batch 14 UI Visual Refinement — see `docs/UI_BRAND_POLISH_BATCH_13_PLAN.md`
```

---

**Wrap-up status:** PR summary complete; ready for MR/PR from `feature/ui-brand-polish-batch-13-recovery` → `main`.
