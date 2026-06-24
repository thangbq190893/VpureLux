# Batch 17 — UI Residual Hardening Audit — PR Summary

**Title:** Batch 17: UI residual hardening audit close-out  
**Branch:** `feature/ui-residual-hardening-batch-17`  
**Base branch:** `main` (@ `f7f5357` — Merge PR #15 Batch 16A)  
**Baseline tag:** `ui-polish-phase-2026-06-25`  
**Document type:** Merge-request / pull-request summary (documentation-only)

---

## Scope summary

Batch 17 performs a **static post-polish audit** of the VPureLux Web UI after Batches 13–16A merged to `main` and the **`ui-polish-phase-2026-06-25`** baseline tag was created.

This MR is **documentation-only**:

- Batch **17.1** — residual audit and hardening plan (`docs/UI_RESIDUAL_HARDENING_BATCH_17_PLAN.md`)
- Batch **17.4** — this close-out summary and plan status update

**Batch 17.2 (optional detail-page UI chrome) and Batch 17.3 (optional test-only permission assertions) were not implemented.** The audit found no mandatory visual patches; the current UI polish baseline is **sufficient for UAT**.

**No backend, Domain, Application, EF Core, migration, API contract, Razor, CSS, JavaScript, or test changes.**

Static repository inspection only — **no runtime/browser/video verification** was performed in Batch 17.

---

## Commit list (oldest → newest on branch vs `main`)

| Commit | Message |
|--------|---------|
| `76783a3` | docs: add Batch 17 UI residual hardening plan |
| _(wrap-up, latest)_ | docs: add Batch 17 UI residual hardening close-out |

---

## Audit conclusion

| Result | Detail |
|--------|--------|
| **Blockers** | **None** |
| **Should Fix visual findings** | **None** on high-traffic list/inquiry/history/hub pages |
| **UI baseline** | High-traffic pages align with Batch 14/15/16 VPURELUX helpers (`.vpl-page`, `.vpl-card-dense`, `.vpl-table-dense`, `.vpl-btn-toolbar`, `.vpl-empty-state`) |
| **Remaining gaps** | Mostly **Defer** or **Nice-to-have** — form/create/edit/modal pages, empty-table-only states, optional detail-page chrome |
| **Permission leaks** | **None** identified in static Razor review |
| **Route/handler risks** | **None** requiring changes |
| **Decision** | **Close Batch 17 with documentation only** — no UI polish implementation in this MR |

---

## High-priority findings summary

No Blocker or Should Fix **visual** items remain after Batches 13–16A.

Deferred visual items (not in scope for this MR):

| Item | Severity | Notes |
|------|----------|-------|
| Sales Create/Edit `alert` on `data-sales-product-context` | Defer | JS product context panel — not an empty-state |
| Audit Index empty table (no `.vpl-empty-state`) | Defer | Empty tbody only; deferred since 16A |
| BOM Product empty version table | Defer | No existing empty alert |
| Audit/BOM Details page chrome | Nice-to-have | Low-traffic detail pages |
| Inventory Receipt/Issue/Adjustment form chrome | Defer | Form-heavy posting workflow |
| ~35 create/edit/modal pages | Defer | Intentionally plain per ABP modal/full-page patterns |
| Pricing `N0`/currency vs Sales `₫` | Defer | Separate formatting approval |

---

## Permission visibility findings summary

Static review of permission-sensitive UI found **no leaks**. Gates are present in Razor for:

- Pricing Index history links (`Pricing.History`, component history permission)
- Sales Index Customer History toolbar (`Sales.ViewProfit` + `CanViewHistory`)
- Sales History/Details cost and profit columns (`Sales.ViewCost`, `Sales.ViewProfit`)
- Inventory hub Ledger link (`Inventory.ViewLedger`)
- Audit Export button (`Audit.Export`)
- BOM publish/archive actions; Customer/CustomerGroup status actions

**Optional future work (Batch 17.3 — not in this MR):** add **test-only** source-level assertions on Pricing/Sales/Inventory Index Razor gates for extra regression assurance. No permission **behavior** changes proposed.

---

## Route/form/handler findings summary

**No changes required.** High-risk surfaces remain intact:

- Inventory posting forms — idempotency keys, `data-inventory-posting-form`, dynamic line bindings
- Sales Details — Confirm/Cancel handlers, `data-sales-action-form`
- Sales Create/Edit — `data-sales-product-context` and context endpoint hooks
- BOM Product — Publish/Archive POST handlers, `data-bom-action-form`
- Catalog/Customer status POST handlers and modal `data-*` hooks

Any future visual work on these pages must remain **class-only** with no handler, route, or `data-*` changes.

---

## Test coverage findings summary

Baseline at tag `ui-polish-phase-2026-06-25`: **120/120 Web.Tests passed**.

| Area | Status |
|------|--------|
| Catalog, Customers, BOM, Sales, Inventory, Audit, Pricing | Strong module coverage |
| Permission PageModel tests | Present for Customers, Groups, Inventory hub, Audit Export, Sales Index actions |
| Optional gap (17.3 deferred) | Source-level assertions for Pricing/Sales/Inventory Index Razor permission gates |
| Fragile CSS note | `InventoryPagesTests` asserts `vpl-empty-state` (from 16A) — acceptable; localization assertions preferred if revised later |

**No test changes in this MR.**

---

## Explicit batch decisions

| Batch | Decision |
|-------|----------|
| **17.2** — Audit/BOM Details UI chrome | **Skipped for now** — cosmetic only; no stakeholder request |
| **17.3** — Test-only permission gate source assertions | **Deferred as optional future work** — low risk, no UI impact; team may run separately if desired |
| **17.4** — This close-out | **Completed** — documentation + validation only |

---

## Deferred items

- Sales Create/Edit JS context panels (`data-sales-product-context`)
- Audit Index empty table state
- BOM Product empty table / no-alert consideration
- Audit/BOM Details chrome (nice-to-have)
- Inventory posting form chrome (nice-to-have / defer)
- Broad create/edit/modal page visual polish
- Pricing currency display alignment (`N0` vs `₫`)
- Dark-theme verification across refined pages
- Backend permission/security refactor
- New dashboards, metrics, KPIs, or navigation destinations

---

## What explicitly did not change

- **Backend / Domain / Application / EF Core / migrations / DB schema**
- **API contracts** and PageModel logic
- **Razor pages**, **CSS**, **JavaScript**
- **Tests** (`test/VPureLux.Web.Tests/**`)
- **Business rules** — calculations, FIFO, posting, Sales lifecycle, BOM publish semantics
- **Permission behavior** — no gates added, removed, or altered
- **Routes, handlers, form field names, GET/POST bindings, idempotency keys**
- **UI polish implementation** — no Batch 17.2 or 17.3 code
- **`global-styles.css`** and LeptonX theme

---

## Test results

Close-out validation (2026-06-24):

```bash
dotnet build VPureLux.slnx --no-restore -m:2
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build -m:1
```

| Check | Result |
|-------|--------|
| Build | **Succeeded** — 0 errors, 3 pre-existing warnings (Domain nullability ×2, Test SDK entry point ×1) |
| Full Web.Tests | **Passed** — 120/120, 0 failed, 0 skipped (~4m 57s) |

Confirms baseline tag `ui-polish-phase-2026-06-25` remains green with documentation-only branch changes.

---

## Suggested PR/MR description

**Title:** Batch 17: UI residual hardening audit close-out

```markdown
## Summary

Documentation-only close-out for Batch 17 UI residual hardening audit after the `ui-polish-phase-2026-06-25` baseline tag.

Static audit of 64 Razor pages found:
- No Blockers
- No Should Fix visual findings on high-traffic pages
- UI polish baseline sufficient for UAT

Batch 17.2 (detail-page UI chrome) skipped. Batch 17.3 (optional test-only permission assertions) deferred.

## Checklist

- [x] Static UI residual audit completed
- [x] No Blockers found
- [x] No Should Fix visual findings found
- [x] Build passed
- [x] Web.Tests passed
- [x] Documentation-only PR
- [x] No Razor/CSS/JavaScript/test changes
- [x] No backend/domain/application/EF/migration/API changes
- [x] No business-rule changes
- [x] No permission behavior changes
- [x] No route/handler/form binding changes
- [x] Optional 17.3 test-only hardening deferred

## Files

- `docs/UI_RESIDUAL_HARDENING_BATCH_17_PLAN.md` — audit plan (17.1)
- `docs/UI_RESIDUAL_HARDENING_BATCH_17_PR_SUMMARY.md` — this close-out (17.4)

## Optional follow-up (not in this MR)

Batch 17.3: test-only source assertions for Pricing/Sales/Inventory Index permission gates — see plan doc §10.
```

---

**Wrap-up status:** Batch 17 **closed** (documentation-only). Ready for MR/PR from `feature/ui-residual-hardening-batch-17` → `main`.
