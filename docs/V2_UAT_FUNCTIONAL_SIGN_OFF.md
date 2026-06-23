# VPureLux ERP V2 — UAT Functional Sign-Off Package

**Document type:** Functional UAT sign-off summary  
**Cycle:** UAT-5 through UAT-8 (June 2026)  
**Prepared for:** Operator-facing functional acceptance after UAT-6/UAT-8 blocker remediation  
**Status:** Conditional functional sign-off recommended

---

## 1. Environment

| Item | Value |
|------|-------|
| Application | VPureLux ERP V2 (ABP 10.4.1, .NET 10, LeptonX) |
| Admin URL | `https://localhost:44325` |
| Database | Remote VPL on `103.172.236.78` (UAT/E2E data present) |
| Auth | Seeded admin (`admin`) |
| Infrastructure | Redis required for distributed locking; SQL Server backend |

---

## 2. Branch and key commits

| Branch | `feature/audit-ui-ux-batch-3` |
|--------|-------------------------------|

| Commit | Description |
|--------|-------------|
| `7ba0455` | Inventory Issue selector fix (StockItem filter before paging) |
| `268ae84` | Catalog Product Details / Component Edit modal no-op fix (ModalManager query-string id) |
| `65b18db` | Catalog modal route compatibility (path id + query-string id) |
| `6f6c695` | Warehouse create form inputs visible on `/Inventory/Warehouses` |

Earlier UAT cycle baseline commits on the same branch include Inventory inquiry polish (`fd0b3bf`), Catalog modal/action UX (`d0b6951`), and Sales operator context (`2364a6f`).

---

## 3. UAT reference data

| Entity | Code / identifier | Notes |
|--------|-------------------|-------|
| Product | `UAT_UI_20260622_1809_P` | Catalog Details modal verification |
| Component | `UAT_UI_20260622_1809_C` | Catalog Edit modal + Issue selector verification |
| Warehouse (Issue) | `UAT_E2E_WH_20260620023951` | Issue posting context |
| Warehouse (Create) | `UAT_UI8_WH_20260623` | Created via fixed Warehouse UI |
| E2E chain | `UAT_E2E_*` prefix | Prior smoke pass: Catalog → Pricing → BOM → Inventory → Sales → Audit |

---

## 4. UAT functional blocker status

| ID | Blocker | Resolution | Runtime verified |
|----|---------|------------|------------------|
| UAT-7.2 | Inventory Issue selector: stocked component not selectable | Application-layer StockItem filter before paging; Web helper requests Active + Component + inventory-enabled items | **Yes** — `UAT_UI_20260622_1809_C` appeared; issue qty 1 posted; balance 8 → 7; ledger recorded issue |
| UAT-8.1 | Catalog Product **Details** action no-op | Modal pages accept ModalManager query-string `?id=` binding | **Yes** — Details modal opens for `UAT_UI_20260622_1809_P` |
| UAT-8.1 | Catalog Component **Edit** action no-op | Same ModalManager id binding fix | **Yes** — Edit modal opens for `UAT_UI_20260622_1809_C` |
| UAT-8.1b | Catalog modal path vs query route mismatch | Optional route `@page "{id:guid?}"` on modal pages | **Yes** — both `/DetailsModal/{id}` and `/DetailsModal?id={id}` render |
| UAT-8.2 | Warehouse create UI: inputs not visible/usable | Presentation-layer form restructure (`abp-card`, labels, `abp-row`/`abp-column`) | **Yes** — Code/Name/Address visible; `UAT_UI8_WH_20260623` created |

**Functional blocker count remaining:** 0 (within UAT-6/UAT-8 scope)

---

## 5. Runtime verification summary

### Core ERP chain (UAT-5)

Prior operator UAT confirmed end-to-end flow via UI:

Catalog → Pricing → BOM publish → Inventory receipt → Sales confirm → FIFO/profit → Audit.

Example proof (UAT-5 isolated run): revenue 310,000; FIFO cost 82,000; profit 228,000.

### Blocker re-verification (UAT-7.2 / UAT-8.x)

| Area | URL / action | Result |
|------|--------------|--------|
| Inventory Issue | `/Inventory/Issue` → warehouse `UAT_E2E_WH_20260620023951` → component `UAT_UI_20260622_1809_C` → issue 1 | PASS |
| Catalog Product Details | `/Catalog/Products` → **Chi tiết** on `UAT_UI_20260622_1809_P` | PASS |
| Catalog Component Edit | `/Catalog/Components` → **Chỉnh sửa** on `UAT_UI_20260622_1809_C` | PASS |
| Catalog modal routes | Path and query-string modal URLs | PASS |
| Warehouse create | `/Inventory/Warehouses` → create `UAT_UI8_WH_20260623` | PASS |

### Evidence paths (where collected)

| Step | Path |
|------|------|
| Product Details modal (UAT-8.1) | `/opt/cursor/artifacts/screenshots/product-details-modal.png` |
| Component Edit modal (UAT-8.1) | `/opt/cursor/artifacts/screenshots/component-edit-modal.png` |
| Product Details modal (UAT-8.1b) | `/opt/cursor/artifacts/screenshots/uat81b-product-details.png` |
| Component Edit modal (UAT-8.1b) | `/opt/cursor/artifacts/screenshots/uat81b-component-edit.png` |
| Warehouse create form (UAT-8.2) | `/opt/cursor/artifacts/screenshots/uat82-warehouses-create-form.png` |
| Warehouse created (UAT-8.2) | `/opt/cursor/artifacts/screenshots/uat82-warehouses-created.png` |
| UAT-7.2 Issue selector / balance / ledger | not collected in this task |

---

## 6. Test summary

| Scope | Result | Notes |
|-------|--------|-------|
| Build (`VPureLux.slnx`) | PASS | Validated during UAT-8.x and UAT-8.2 finalization |
| Web Inventory tests | **30/30** PASS | Includes Warehouse create form tests |
| Web.Tests (full Web project) | **118/118** PASS | Post UAT-8.2 |
| Catalog Web tests (UAT-8.1) | **20/20** PASS | Modal route and action hook coverage |
| Full solution (post Catalog fixes) | **323/323** PASS | Observed once after UAT-8.1 |
| Full solution (later runs) | Intermittent FAIL | Unrelated EF Audit SQLite/seed init in `VPureLuxTestBaseModule.SeedTestData`; failing test name changed between runs; individual EF retry passed |

**Assessment:** Web-layer and Inventory validation is green for sign-off scope. Full-suite green is not required for functional UAT sign-off because failures are classified as test infrastructure flakiness, not application regressions.

---

## 7. Known non-blocking issues (P3)

| Issue | Severity | Impact on functional UAT |
|-------|----------|--------------------------|
| HealthCheck self-HTTPS `UntrustedRoot` log noise | P3 | None — known dev cert behavior |
| EF Audit test fixture flakiness on full-suite parallel runs | P3 | None on runtime UAT; blocks automated full-suite gate only |
| Product image upload: 1×1 PNG poor validation UX; 16×16 PNG passed | P3 | Minor — operators should use reasonably sized images |
| SignalR Chat transaction log noise | P3 | None — known from earlier UAT-4.1 |

---

## 8. Deferred polish and backlog (out of functional sign-off scope)

| Item | Tracking |
|------|----------|
| UI Brand Polish / VPURELUX visual alignment (`vpurelux.com`) | `UI_FIX_BACKLOG.md` — Batch 13 |
| Product image remove UI — not clearly tested/found in UAT-6 | Deferred verification |
| Component details/image/status flows — not fully re-tested after primary action fixes | Deferred verification |
| General table/filter/money formatting polish | Deferred |
| Inventory Receipt/Issue/Adjustment selector UX batches | `UI_FIX_BACKLOG.md` Batches 1–3 (separate from UAT-8 blockers) |

---

## 9. Sign-off recommendation

### Conditional functional sign-off: **APPROVED**

**Included in sign-off:**

- Core ERP operator flows validated in UAT-5 (Catalog, Pricing, BOM, Inventory posting, Sales confirm, FIFO/profit, Audit).
- All UAT-6/UAT-8 functional blockers resolved and runtime-verified.
- Warehouse master-data create restored on `/Inventory/Warehouses`.
- Catalog modal actions and route compatibility restored.

**Excluded from full sign-off (explicit deferrals):**

- Deferred UI visual/brand polish (Batch 13).
- P3 infrastructure/log noise and EF test fixture flakiness.
- Secondary Catalog flows (image remove, component details/image/status) not fully re-tested.
- Broader UI refactor backlog items in `UI_FIX_BACKLOG.md`.

**Sign-off statement:**

> Functional UAT for VPureLux ERP V2 core operator workflows may be **conditionally signed off** on branch `feature/audit-ui-ux-batch-3` at commit `6f6c695` and later, subject to stakeholder acceptance of deferred UI polish and known P3 items above. Full product sign-off should await Batch 13 brand alignment and stabilization of EF Core test infrastructure for CI gates.

---

## 10. Related documents

- `DATA_FLOW_UAT_SCENARIOS_V2.md` — scenario definitions
- `UI_UAT_FLOW_TEST_PLAN.md` — operator checklist
- `V2_FINAL_UAT_BUG_BACKLOG.md` — bug tracking
- `UI_FIX_BACKLOG.md` — UI polish backlog (Batch 13 brand alignment)

---

*Generated as part of the UAT functional sign-off package task. No application code was modified in this document-only update.*
