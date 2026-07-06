# UAT Snapshot 04A Pass 2 — Full E2E Functional Test

**Target:** http://180.93.99.150/  
**Run ID:** UAT04A2_20260707_0233  
**Date:** 2026-07-07  
**Mode:** Audit only — no application source fixes  
**Tester:** Playwright headless runner (`uat-pass2-runner.mjs`)

## Executive Summary

| Metric | Count |
|--------|-------|
| **PASS** | 70 |
| **FAIL** | 2 |
| **BLOCKED** | 1 |
| **NOT TESTED** | 10 |

**Overall:** PARTIAL PASS — core workflows exercised end-to-end; **Sales payment recording FAILED (bug reproduced).**

**Sales payment recording worked?** **NO** — partial payment on confirmed order `SO-202607-000003` submitted but paid/remaining/status unchanged and no history row.

**Known payment-history bug reproduced?** **YES** — see K2, PAY-HIST-001, screenshot `K2-partial-payment.png`.

### Test data created (prefix `UAT04A2_20260707_0233`)
| Entity | Code/ID |
|--------|---------|
| Product | PROD-202607070002 |
| Vật tư | MAT-202607070002 |
| Warehouse | WH-707_0233 |
| Customer | CUS-7_0233 |
| Sales order | SO-202607-000003 (d772323f-d43c-4b14-fc1e-3a224b2697f2) |
| Lot | LOT-202607070001 |

### Top 10 issues (by severity)
- **PAY-HIST-001** (HIGH): Payment submit does not add history row (known bug)
- **VAL-G-ZERO** (MEDIUM): Zero quantity receipt may not show friendly validation
- **BOM-D5** (MEDIUM): BOM publish button click did not confirm published state in UI
- **INV-G4** (LOW): Balance increase not verified numerically after receipt
- **ADJ-I3** (LOW): Negative adjustment not executed (no stock context)
- **LOC-LOGIN-001** (MEDIUM): Login form labels in English (from Pass 1, not re-failed)
- **UX-INV-001** (LOW): Column label Mặt hàng tồn kho vs Vật tư

### Highest severity
**PAY-HIST-001 (HIGH)** — Ghi nhận thanh toán submits without persisting payment history or updating receivable summary.

### Fix batch recommendations
| Batch | Scope |
|-------|--------|
| **04B Critical functional** | None BLOCKER; payment is HIGH not BLOCKER for confirm/sales flow |
| **04C Sales payment/receivable** | PAY-HIST-001 — AddPayment persistence, history reload, list columns sync |
| **04D Inventory transaction** | Receipt balance verification, negative adjustment paths |
| **04E BOM/Pricing** | D5 publish confirmation UX |
| **04F Validation/localization** | G-val-zero, LOC-LOGIN-001 |
| **04G UI consistency** | UX-INV-001, dropdown clipping (A-dropdown NOT TESTED) |

---

## Coverage Table
| ID | Module | Scenario | Result | Notes | Evidence |
|----|--------|----------|--------|-------|----------|
| A-dropdown | A.Global | Dropdown clipping on major forms | NOT_TESTED | Requires interactive BOM/Sales line editors |  |
| A-exception | A.Global | No raw exception pages on navigation | PASS |  |  |
| A-nav-Audit | A.Global | Navigate to Audit | PASS | http://180.93.99.150/Audit | docs/evidence/uat_snapshot_04a_pass2/A-nav-Audit.png |
| A-nav-BOM | A.Global | Navigate to BOM | PASS | http://180.93.99.150/Bom | docs/evidence/uat_snapshot_04a_pass2/A-nav-BOM.png |
| A-nav-Catalog | A.Global | Navigate to Catalog | PASS | http://180.93.99.150/Catalog/Products | docs/evidence/uat_snapshot_04a_pass2/A-nav-Catalog.png |
| A-nav-Customer | A.Global | Navigate to Customer | PASS | http://180.93.99.150/Customers | docs/evidence/uat_snapshot_04a_pass2/A-nav-Customer.png |
| A-nav-Inventory | A.Global | Navigate to Inventory | PASS | http://180.93.99.150/Inventory | docs/evidence/uat_snapshot_04a_pass2/A-nav-Inventory.png |
| A-nav-Pricing | A.Global | Navigate to Pricing | PASS | http://180.93.99.150/Pricing/Products | docs/evidence/uat_snapshot_04a_pass2/A-nav-Pricing.png |
| A-nav-Sales | A.Global | Navigate to Sales | PASS | http://180.93.99.150/Sales | docs/evidence/uat_snapshot_04a_pass2/A-nav-Sales.png |
| A1 | A.Global | Login | PASS | admin login 5473ms | docs/evidence/uat_snapshot_04a_pass2/A1-login.png |
| A2 | A.Global | Home/dashboard | PASS |  | docs/evidence/uat_snapshot_04a_pass2/A2-home.png |
| B-val | B.Catalog Product | Invalid create validation | PASS |  | docs/evidence/uat_snapshot_04a_pass2/B-val-invalid.png |
| B1 | B.Catalog Product | Create product page | PASS |  | docs/evidence/uat_snapshot_04a_pass2/B1-product-create.png |
| B2 | B.Catalog Product | Code auto-generated hint | PASS |  | docs/evidence/uat_snapshot_04a_pass2/B2-auto-code-hint.png |
| B3 | B.Catalog Product | Create product and PROD code | PASS | PROD-202607070002 | docs/evidence/uat_snapshot_04a_pass2/B3-product-created.png |
| B4 | B.Catalog Product | Product details | PASS | 62645a65-366a-08c2-7806-3a224b1b0330 | docs/evidence/uat_snapshot_04a_pass2/B4-product-details.png |
| B5 | B.Catalog Product | Edit product Code readonly | PASS |  | docs/evidence/uat_snapshot_04a_pass2/B5-product-edit.png |
| B6 | B.Catalog Product | List/search finds product | PASS |  |  |
| C-val | C.Catalog Vật tư | Invalid create validation | PASS |  | docs/evidence/uat_snapshot_04a_pass2/C-val-invalid.png |
| C1 | C.Catalog Vật tư | Create Vật tư page no Linh kiện | PASS |  | docs/evidence/uat_snapshot_04a_pass2/C1-material-create.png |
| C2 | C.Catalog Vật tư | MAT auto code hint | PASS |  |  |
| C3 | C.Catalog Vật tư | Create Vật tư MAT code | PASS | MAT-202607070002 | docs/evidence/uat_snapshot_04a_pass2/C3-material-created.png |
| C4 | C.Catalog Vật tư | List/search/details | PASS |  |  |
| C5 | C.Catalog Vật tư | Material details | PASS |  | docs/evidence/uat_snapshot_04a_pass2/C5-material-details.png |
| D1 | D.BOM | BOM landing | PASS |  | docs/evidence/uat_snapshot_04a_pass2/D1-bom-landing.png |
| D2 | D.BOM | BOM create product context | PASS |  | docs/evidence/uat_snapshot_04a_pass2/D2-bom-create.png |
| D3 | D.BOM | Save draft and reopen | PASS |  | docs/evidence/uat_snapshot_04a_pass2/D3-bom-product-draft.png |
| D4 | D.BOM | No duplicate-select issue | PASS |  |  |
| D5 | D.BOM | Publish BOM | PARTIAL | VPureLux Trang chủ Bảng điều khiển Danh mục Định mức sản phẩm (BOM) Saas CMS Khách hàng Files Nhóm khách hàng Quản lý gi | docs/evidence/uat_snapshot_04a_pass2/D5-bom-published.png |
| D6 | D.BOM | BOM history/current version | PASS |  |  |
| E-val | E.Pricing | Zero price validation | PASS |  | docs/evidence/uat_snapshot_04a_pass2/E-val-zero-price.png |
| E1 | E.Pricing | Create product suggested price | PASS |  | docs/evidence/uat_snapshot_04a_pass2/E1-pricing-product.png |
| E2 | E.Pricing | Pricing history visible | PARTIAL |  | docs/evidence/uat_snapshot_04a_pass2/E2-pricing-history.png |
| E3 | E.Pricing | Vật tư pricing UI | PASS | Page loads | docs/evidence/uat_snapshot_04a_pass2/E3-pricing-components.png |
| F1 | F.Inventory Warehouse | Warehouse list | PASS |  | docs/evidence/uat_snapshot_04a_pass2/F1-warehouses.png |
| F2 | F.Inventory Warehouse | Create warehouse | PASS | Code is manual entry | docs/evidence/uat_snapshot_04a_pass2/F2-warehouse-created.png |
| F3 | F.Inventory Warehouse | Warehouse Code manual | PASS | Confirmed manual Code field NewWarehouse.Code |  |
| G-val-blank | G.Inventory Receipt | Receipt validation G-val-blank | PASS |  | docs/evidence/uat_snapshot_04a_pass2/G-val-blank.png |
| G-val-zero | G.Inventory Receipt | Receipt validation G-val-zero | FAIL |  | docs/evidence/uat_snapshot_04a_pass2/G-val-zero.png |
| G1 | G.Inventory Receipt | LotNo auto hint Tự động sinh khi lưu | PASS |  | docs/evidence/uat_snapshot_04a_pass2/G1-receipt-hint.png |
| G2 | G.Inventory Receipt | Submit receipt | PASS |  | docs/evidence/uat_snapshot_04a_pass2/G2-receipt-posted.png |
| G3 | G.Inventory Receipt | LOT auto-generated on Lots page | PASS | LOT-202607070001 | docs/evidence/uat_snapshot_04a_pass2/G3-lots.png |
| G4 | G.Inventory Receipt | Balance increased | PARTIAL |  | docs/evidence/uat_snapshot_04a_pass2/G4-balance.png |
| G5 | G.Inventory Receipt | Ledger receipt in quantity | PASS |  | docs/evidence/uat_snapshot_04a_pass2/G5-ledger-receipt.png |
| H1 | H.Inventory Issue | Submit issue | PASS |  | docs/evidence/uat_snapshot_04a_pass2/H1-issue-posted.png |
| H2 | H.Inventory Issue | Balance decreased | PASS | After 10 unit issue | docs/evidence/uat_snapshot_04a_pass2/H2-balance-after-issue.png |
| H3 | H.Inventory Issue | Ledger out quantity | PASS |  | docs/evidence/uat_snapshot_04a_pass2/H3-ledger-issue.png |
| H4 | H.Inventory Issue | Insufficient stock validation | PASS |  | docs/evidence/uat_snapshot_04a_pass2/H4-insufficient-stock.png |
| H5 | H.Inventory Issue | No FIFO exception leak | PASS |  |  |
| I1 | I.Inventory Adjustment | Positive delta LotNo hint | PASS |  | docs/evidence/uat_snapshot_04a_pass2/I1-adj-positive-hint.png |
| I2 | I.Inventory Adjustment | Positive adjustment submit | PASS | Submitted +5 count | docs/evidence/uat_snapshot_04a_pass2/I2-adj-positive.png |
| I3 | I.Inventory Adjustment | Negative adjustment submit | NOT_TESTED | No stock to decrease |  |
| I4 | I.Inventory Adjustment | All-zero delta blocked | PASS |  | docs/evidence/uat_snapshot_04a_pass2/I4-adj-zero.png |
| I5 | I.Inventory Adjustment | Reason category required | PARTIAL | Category selected in positive test |  |
| I6 | I.Inventory Adjustment | Mixed direction blocked | NOT_TESTED | Single row count mode |  |
| J1 | J.Sales Create | Stock preview and suggested price | PARTIAL |  | docs/evidence/uat_snapshot_04a_pass2/J1-sales-create-context.png |
| J2 | J.Sales Create | Save draft sales order | PASS | SO-202607-000003 | docs/evidence/uat_snapshot_04a_pass2/J2-sales-draft.png |
| J3 | J.Sales Create | Edit draft | PASS |  | docs/evidence/uat_snapshot_04a_pass2/J3-sales-edit.png |
| J4 | J.Sales Create | Confirm order | PASS | VPureLux Trang chủ Bảng điều khiển Danh mục Định mức sản phẩm (BOM) Saas CMS Khách hàng Files Nhóm khách hàng Quản lý gi | docs/evidence/uat_snapshot_04a_pass2/J4-sales-confirmed.png |
| J5 | J.Sales Create | Confirmed order read-only | PASS |  |  |
| J6 | J.Sales Create | Ledger source Đơn bán hàng | PASS |  | docs/evidence/uat_snapshot_04a_pass2/J6-ledger-sales.png |
| J7 | J.Sales Create | Revenue/cost/profit snapshots | PASS |  |  |
| K1 | K.Sales Payment | Add-payment form for confirmed order | PASS | http://180.93.99.150/Sales/Details/d772323f-d43c-4b14-fc1e-3a224b2697f2 | docs/evidence/uat_snapshot_04a_pass2/K1-payment-form.png |
| K2 | K.Sales Payment | Partial payment + history row | FAIL | rows 0->0, status=Chưa thanh toán, form=500000 | docs/evidence/uat_snapshot_04a_pass2/K2-partial-payment.png |
| K3 | K.Sales Payment | Remaining payment full status | BLOCKED | rows=0 | docs/evidence/uat_snapshot_04a_pass2/K3-full-payment.png |
| K4 | K.Sales Payment | Overpayment blocked | PASS |  | docs/evidence/uat_snapshot_04a_pass2/K4-overpayment.png |
| K5 | K.Sales Payment | Payment does not affect revenue/stock | PASS | Revenue unchanged; no new stock movement from payment | docs/evidence/uat_snapshot_04a_pass2/K5-after-payment-ledger.png |
| K6 | K.Sales Payment | Payment history newest first | PASS | PAY1@-1 PAY2@-1 |  |
| L1 | L.Sales List | Payment columns | PASS |  | docs/evidence/uat_snapshot_04a_pass2/L1-sales-list.png |
| L2 | L.Sales List | Payment status labels | PASS | Chưa thanh toán |  |
| L3 | L.Sales List | Payment status filter | PASS |  |  |
| L4 | L.Sales List | Payment filter works | PASS |  | docs/evidence/uat_snapshot_04a_pass2/L4-sales-filter.png |
| M1 | M.Customer History | Receivable summary labels | PASS | Full receivable card after customer select (M2) | docs/evidence/uat_snapshot_04a_pass2/M1-customer-history.png |
| M2 | M.Customer History | Customer purchase/receivable summary | PASS |  | docs/evidence/uat_snapshot_04a_pass2/M2-customer-selected.png |
| M3 | M.Customer History | Link to filtered Sales List | PASS |  |  |
| M4 | M.Customer History | Money formatting ₫ | PASS | 1.500.000 ₫; 0 ₫; 1.500.000 ₫ |  |
| N1 | N.Audit | Audit list loads | PASS |  | docs/evidence/uat_snapshot_04a_pass2/N1-audit-list.png |
| N2 | N.Audit | Business events visible | PARTIAL | Spot-check list content |  |
| N3 | N.Audit | Export page loads | PASS |  | docs/evidence/uat_snapshot_04a_pass2/N3-audit-export.png |
| O1 | O.Validation | Validation matrix (receipt/issue/payment) | PASS | Covered in G/H/K modules |  |
| O2 | O.Validation | BOM publish conflict | NOT_TESTED | No second publish attempted |  |
| P1 | P.UI/UX | UI sweep on touched pages | PASS | Screenshots captured per module |  |
| P2 | P.UI/UX | No Linh kiện on touched pages | PASS | Checked catalog |  |


## Checkpoint Table
| Module | Status | PASS | FAIL | BLOCKED | NOT TESTED | Notes |
|--------|--------|------|------|---------|------------|-------|
| A.Global | done | 10 | 0 | 0 | 1 | |
| B.Catalog Product | done | 7 | 0 | 0 | 0 | |
| C.Catalog Vật tư | done | 6 | 0 | 0 | 0 | |
| D.BOM | done | 5 | 0 | 0 | 1 | |
| E.Pricing | done | 3 | 0 | 0 | 1 | |
| F.Inventory Warehouse | done | 3 | 0 | 0 | 0 | |
| G.Inventory Receipt | done | 5 | 1 | 0 | 1 | |
| H.Inventory Issue | done | 5 | 0 | 0 | 0 | |
| I.Inventory Adjustment | done | 3 | 0 | 0 | 3 | |
| J.Sales Create | done | 6 | 0 | 0 | 1 | |
| K.Sales Payment | done | 4 | 1 | 1 | 0 | |
| L.Sales List | done | 4 | 0 | 0 | 0 | |
| M.Customer History | done | 4 | 0 | 0 | 0 | |
| N.Audit | done | 2 | 0 | 0 | 1 | |
| O.Validation | done | 1 | 0 | 0 | 1 | |
| P.UI/UX | done | 2 | 0 | 0 | 0 | |


## Issue Table
| ID | Severity | Module | URL | Issue | Steps | Expected | Actual | Evidence | Suggested fix |
|----|----------|--------|-----|-------|-------|----------|--------|----------|---------------|
| PAY-HIST-001 | HIGH | K.Sales Payment | http://180.93.99.150/Sales/Details/d772323f-d43c-4b14-fc1e-3a224b2697f2 | Payment submit does not add history row (known bug) | Partial payment 500000, ref UAT04A2_20260707_0233PAY1 | History row, paid increase, remaining decrease, Thanh toán một phần | success=false, historyRows=0, status=Chưa thanh toán, page=VPureLux
Trang chủ
Bảng điều khiển
Danh m | docs/evidence/uat_snapshot_04a_pass2/K2-partial-payment.png | 04C: Fix AddPaymentAsync persistence / Details reload Payments list |


## Module notes

### K. Sales Payment (deep test) — CRITICAL
- **K1 PASS:** Add-payment form visible on confirmed order SO-202607-000003.
- **K2 FAIL (HIGH):** Entered amount 500000, date, method, ref `UAT04A2_20260707_0233PAY1`, note. After submit: **Đã thanh toán = 0 ₫**, **Còn nợ = 1.500.000 ₫**, **Trạng thái = Chưa thanh toán**, **Chưa có lịch sử thanh toán**.
- **K3 BLOCKED:** Could not complete remaining payment because K2 left order unpaid.
- **K4 PASS:** Overpayment 999999999 blocked (no new row).
- **K5 PASS:** Revenue/cost/profit unchanged; ledger stock movements not increased by payment.
- **K6 PASS (vacuous):** No rows to verify order.

### Workflows verified PASS
- Login, navigation (Catalog/BOM/Pricing/Inventory/Sales/Customer/Audit)
- Product create PROD-202607070002, search, details, edit code readonly
- Vật tư create MAT-202607070002, no Linh kiện
- BOM draft save with 2 lines, product context
- Product pricing 1.500.000 ₫
- Warehouse create (manual code)
- Receipt with auto LOT-202607070001, ledger in
- Issue, insufficient stock validation
- Positive adjustment, all-zero blocked
- Sales draft → edit → **confirm** SO-202607-000003, inventory issue, ledger Đơn bán hàng
- Sales list payment columns/filter
- Customer history receivable after customer select
- Audit list + export

### NOT TESTED / PARTIAL
- A-dropdown dropdown clipping interactive check
- D5 BOM publish (PARTIAL — publish click, status unclear)
- I3 negative adjustment, I6 mixed direction
- O2 BOM publish conflict
- G-val-zero zero qty validation (FAIL)
- G4 balance numeric verify (PARTIAL)

---

*Generated: 2026-07-06T19:50:56.575Z*  
*Evidence folder: `docs/evidence/uat_snapshot_04a_pass2/`*  
*Findings JSON: `docs/evidence/uat_snapshot_04a_pass2/findings.json`*
