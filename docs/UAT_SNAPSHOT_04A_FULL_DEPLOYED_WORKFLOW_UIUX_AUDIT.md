# UAT Snapshot 04A - Full Deployed Workflow and UI/UX Audit

## 1. Executive summary

| Field | Value |
|-------|-------|
| Date/time | 2026-07-07 ~02:24 UTC+7 |
| Target URL | http://180.93.99.150/ |
| Browser/tool | Playwright (Chromium headless) — Cursor IDE Browser MCP unavailable |
| User/role | `admin` (seeded operator) |
| Overall result | **PARTIAL PASS** — smoke/UI verification complete; deep end-to-end workflows largely **NOT TESTED** |
| Scenarios run | 36 recorded |
| PASS / FAIL / BLOCKED / NOT TESTED (run) | **31 / 0 / 0 / 5** |
| Extended scope (not executed) | ~70+ deep workflow/validation scenarios marked **NOT TESTED** |
| Top risks | English login/cookie strings; Customer History money formatting inconsistent; full inventory/sales posting flows not re-verified in this pass |

Deployed build includes recent UI polish: Inventory LotNo auto-hint (`96f7ab8`), Sales receivable columns/summary (`37bb0d1`).

## 2. Environment

| Item | Value |
|------|-------|
| URL | http://180.93.99.150/ |
| Date/time | 2026-07-07 |
| Browser | Playwright Chromium 1.61.1 |
| User/role | admin |
| Commit/version if visible | Not shown in UI; server redeployed from `main` after `37bb0d1` |

## 3. Test data created

| Entity | Code/Name | Notes |
|--------|-----------|-------|
| Product | PROD-202607070001 / UAT04A_20260707_0210 Product | Created during audit |
| Vật tư | MAT-202607070001 / UAT04A_20260707_0210 Material | Created during audit |
| Prefix | UAT04A_20260707_0210 | Used for audit entities |

No warehouse/customer/BOM/receipt/sales test transactions were created in this pass (read-only + catalog create only).

## 4. Progress checkpoint

| Module | Status | Last scenario completed | Evidence folder/files | Notes |
|--------|--------|-------------------------|----------------------|-------|
| Global/Shell | **PARTIAL** | A8 Vietnamese spot-check | 00-login-form.png, 01*.png | Login PASS; logout/deep UX deferred |
| Catalog | **PARTIAL** | C3-save MAT code | 02-catalog-*.png | List/create/auto-code PASS; edit/validation NOT TESTED |
| BOM | **SMOKE** | D1 landing | 03-bom-landing.png | Create/publish NOT TESTED |
| Pricing | **SMOKE** | E1 product pricing list | 03-pricing-*.png | Create/validation NOT TESTED |
| Inventory | **PARTIAL** | F4 count-first UI | 04-*, 05-* | Receipt LotNo hint PASS; post/issue/adjust NOT TESTED |
| Sales/Payment | **PARTIAL** | G5 payment details | 06-*, 07-* | List columns/filter/details PASS; create/confirm/payment flow NOT TESTED |
| Customer/Receivable | **PARTIAL** | H6 sales list link | 08-* | Receivable summary PASS when customer selected |
| Audit | **SMOKE** | I4 export page | 09-* | List/export load PASS |
| Validation/Error Handling | **NOT STARTED** | — | — | Intentional invalid-input matrix deferred |
| UI/UX Consistency | **PARTIAL** | Money format spot-check | 08-customer-history-receivable-selected.png | Login/cookie English; money format inconsistency |

## 5. Workflow coverage matrix

| ID | Module | Scenario | Result | Notes | Evidence |
|----|--------|----------|--------|-------|----------|
| A1 | Global | Login | PASS | admin login ~4.5s | 01-login-home.png |
| A2 | Global | Logout | NOT TESTED | Session preserved | — |
| A3 | Global | Dashboard/home | PASS | VPURELUX ERP hub cards | 01b-home-dashboard.png |
| A4 | Global | Left menu structure | PASS | Vietnamese menu labels | 01b-home-dashboard.png |
| A5 | Global | Inventory submenu | PASS | /Inventory hub | 01c-inventory-hub.png |
| A6 | Global | Sales menu | PASS | /Sales list | 01d-sales-list.png |
| A7 | Global | BOM menu title `Định mức sản phẩm (BOM)` | PASS | Sidebar + landing | 01e-bom-landing.png |
| A8 | Global | Vietnamese localization (app shell) | PASS | Main app Vietnamese | — |
| A9–A12 | Global | Breadcrumb/dropdown/overflow/empty | NOT TESTED | Deferred | — |
| B1 | Catalog | Product list | PASS | | 02-catalog-product-list.png |
| B3 | Catalog | Product create auto-code hint | PASS | Tự động sinh khi lưu | 02-catalog-product-create.png |
| B3-save | Catalog | Product save PROD code | PASS | PROD-202607070001 | 02-catalog-product-created.png |
| C1 | Catalog | Vật tư list | PASS | Uses Vật tư wording | 02-catalog-component-list.png |
| C3 | Catalog | Vật tư create auto-code | PASS | No Linh kiện | 02-catalog-component-create.png |
| C3-save | Catalog | Vật tư save MAT code | PASS | MAT-202607070001 | 02-catalog-component-created.png |
| B2,B4–B8,C2,C4–C7 | Catalog | Search/edit/validation/status | NOT TESTED | | — |
| D1 | BOM | Landing table/search style | PASS | | 03-bom-landing.png |
| D2–D12 | BOM | Create/edit/publish/conflict | NOT TESTED | | — |
| E1 | Pricing | Product pricing list | PASS | | 03-pricing-products.png |
| E2–E6 | Pricing | Component pricing/create/validation | NOT TESTED | Pricing components page loaded only | 03-pricing-components.png |
| F1-1 | Inventory | Warehouse list | PASS | | 04-inventory-warehouses.png |
| F2-1 | Inventory | Receipt LotNo auto hint | PASS | Tự động sinh khi lưu | 04-inventory-receipt-auto-lot.png |
| F2-2–F2-8 | Inventory | Receipt post/LOT verify/ledger | NOT TESTED | | — |
| F3-1–F3-6 | Inventory | Issue workflow | NOT TESTED | | — |
| F4-1 | Inventory | Adjustment count-first UI | PASS | | 04-inventory-adjustment.png |
| F4-2–F4-9 | Inventory | Adjustment post/guards/ledger | NOT TESTED | | — |
| F5-1–F5-7 | Inventory | Balances/Lots/Ledger deep | PARTIAL | Ledger/Lots pages load | 04-inventory-lots.png, 05-inventory-ledger-source-reference.png |
| G1-3 | Sales | Payment summary columns | PASS | Tổng đơn/Đã TT/Còn nợ/TT status | 07-sales-confirm-payment-summary.png |
| G1-4 | Sales | Payment status labels | PASS | Chưa thanh toán badge seen | 06-sales-list-payment.png |
| G1-5 | Sales | Payment status filter | PASS | | 06-sales-list-filter.png |
| G2-1 | Sales | Create draft page | PASS | | 06-sales-create-draft.png |
| G2-2–G4 | Sales | Create/edit/confirm flows | NOT TESTED | | — |
| G5-1 | Sales | Details payment summary | PASS | Confirmed order SO-202607-000002 | 07-sales-details-payment.png |
| G5-2 | Sales | Add payment form | PASS | Ghi nhận thanh toán visible | 07-sales-details-payment.png |
| G5-3–G5-10 | Sales | Partial/paid/overpay/idempotency | NOT TESTED | | — |
| H1 | Customer | Customer list | PASS | | 08-customer-list.png |
| H5 | Customer | Receivable summary (customer selected) | PASS | All four labels + values | 08-customer-history-receivable-selected.png |
| H6 | Customer | Link to filtered Sales List | PASS | Xem đơn bán của khách | 08-customer-history-receivable-selected.png |
| H2–H4,H7 | Customer | Create/edit/dealer workflow | NOT TESTED | | — |
| I1 | Audit | Audit list | PASS | | 09-audit-list.png |
| I4 | Audit | Audit export | PASS | | 09-audit-export.png |
| I2–I3 | Audit | Detail/events | NOT TESTED | | — |
| J1–J10 | Validation | Cross-module invalid inputs | NOT TESTED | | — |
| K1–K15 | UI/UX | Consistency checklist | PARTIAL | Issues logged below | multiple |

## 6. Functional issues

| ID | Severity | Module | URL | Issue | Steps | Expected | Actual | Evidence | Suggested fix |
|----|----------|--------|-----|-------|-------|----------|--------|----------|---------------|
| — | — | — | — | No functional blockers found in executed scenarios | — | — | — | — | — |

## 7. Business logic issues

| ID | Severity | Module | URL | Issue | Steps | Expected | Actual | Evidence | Suggested fix |
|----|----------|--------|-----|-------|-------|----------|--------|----------|---------------|
| — | — | — | — | Deep posting/FIFO/payment flows not executed | — | — | — | — | Re-run 04A pass 2 for end-to-end |

## 8. UI/UX issues

| ID | Severity | Module | URL | Issue | Steps | Expected | Actual | Evidence | Suggested fix |
|----|----------|--------|-----|-------|-------|----------|--------|----------|---------------|
| UX-CH-001 | MEDIUM | Customer/Receivable | /Sales/CustomerHistory | Inconsistent money formatting in purchase summary cards/table | Select customer 001-Hiển | Same `#,0 ₫` format as receivable card | Raw `1500001,00` without ₫ in cards/table | 08-customer-history-receivable-selected.png | Apply SalesUiFormatter/FormatMoney to Customer History summary + table |
| UX-INV-001 | LOW | Inventory | /Inventory/Receipt | Line editor column uses `Mặt hàng tồn kho` not `Vật tư` | Open receipt | Consistent Vật tư terminology in operator flows | Header shows Mặt hàng tồn kho | 04-inventory-receipt-auto-lot.png | Align Inventory:StockItem label or use Inventory:Material where appropriate |

## 9. Validation/error-message issues

| ID | Severity | Module | URL | Issue | Steps | Expected | Actual | Evidence | Suggested fix |
|----|----------|--------|-----|-------|-------|----------|--------|----------|---------------|
| — | — | — | — | Validation matrix not executed | — | — | — | — | Dedicated validation pass |

## 10. Localization/wording issues

| ID | Severity | Module | URL | Issue | Steps | Expected | Actual | Evidence | Suggested fix |
|----|----------|--------|-----|-------|-------|----------|--------|----------|---------------|
| LOC-LOGIN-001 | MEDIUM | Global/Shell | /Account/Login | Login labels English | Open login page | Vietnamese labels | User name or email address / Password / Remember me | 00-login-form.png | Localize Abp Account login in vi-VN |
| LOC-COOKIE-001 | LOW | Global/Shell | / | Cookie banner English | Any authenticated page | Vietnamese notice | English cookie banner | 01b-home-dashboard.png | Localize cookie consent text |

No `Linh kiện` wording observed in executed pages.

## 11. Performance observations

| Module | Observation | Severity | Evidence | Suggested fix |
|--------|-------------|----------|----------|---------------|
| Global | Login ~4.5s acceptable on deployed VPS | LOW | findings.json A1 | Monitor if >10s |

## 12. Security/permission observations

| Module | Observation | Severity | Evidence | Suggested fix |
|--------|-------------|----------|----------|---------------|
| Global | Audit run as full admin; permission matrix not tested | NOT TESTED | — | Run restricted-role pass |

## 13. Evidence index

| File | Screen/workflow | Notes |
|------|-----------------|-------|
| 00-login-form.png | Login form (pre-auth) | English labels issue |
| 01-login-home.png | Post-login landing | Misnamed; shows home |
| 01b-home-dashboard.png | VPURELUX ERP home | Cookie banner |
| 01c-inventory-hub.png | Inventory hub | |
| 01d-sales-list.png | Sales list (early) | |
| 01e-bom-landing.png | BOM landing | |
| 02-catalog-product-list.png | Product list | |
| 02-catalog-product-create.png | Product create | Auto-code hint |
| 02-catalog-product-created.png | Product after save | PROD-202607070001 |
| 02-catalog-component-list.png | Vật tư list | |
| 02-catalog-component-create.png | Vật tư create | |
| 02-catalog-component-created.png | Vật tư after save | MAT-202607070001 |
| 03-bom-landing.png | BOM landing | |
| 03-pricing-products.png | Product pricing | |
| 03-pricing-components.png | Vật tư pricing | |
| 04-inventory-receipt-auto-lot.png | Receipt LotNo hint | PASS 03K.5B |
| 04-inventory-adjustment.png | Adjustment count-first | |
| 04-inventory-lots.png | Lots inquiry | |
| 04-inventory-warehouses.png | Warehouses | |
| 05-inventory-ledger-source-reference.png | Ledger filters | |
| 06-sales-create-draft.png | Sales create | |
| 06-sales-list-filter.png | Payment status filter | |
| 06-sales-list-payment.png | Sales list payment cols | |
| 07-sales-confirm-payment-summary.png | Sales list payment | |
| 07-sales-details-payment.png | Sales details payment | 03L.5B |
| 08-customer-list.png | Customer list | |
| 08-customer-history-receivable.png | Customer history empty filter | Before customer select |
| 08-customer-history-receivable-selected.png | Receivable summary | 03L.5B PASS |
| 09-audit-list.png | Audit list | |
| 09-audit-export.png | Audit export | |

## 14. Recommended fix batches

* **04B Critical blockers** — None found in smoke pass; run deep workflow pass before sign-off.
* **04C Sales/payment/receivable fixes** — Customer History money formatting (`UX-CH-001`).
* **04D Inventory fixes** — Terminology alignment receipt column (`UX-INV-001`); full receipt/adjustment E2E verification.
* **04E BOM/Pricing fixes** — BOM create/publish and pricing create validation pass deferred.
* **04F Catalog/Customer fixes** — Login localization (`LOC-LOGIN-001`).
* **04G UI consistency cleanup** — Cookie banner VI (`LOC-COOKIE-001`); money/date format sweep on Customer History.

## 15. Final conclusion

**Partial audit complete.** Deployed app at http://180.93.99.150/ is **operational** for admin login and core module navigation. Recent UAT fixes **03K.5B** (Inventory LotNo `Tự động sinh khi lưu`) and **03L.5B** (Sales payment columns + Customer receivable summary) are **verified on deployed build**.

**Not completed:** full end-to-end workflows (inventory post, sales confirm/payment, BOM publish, validation matrix, permission matrix, logout). Recommend **UAT 04A Pass 2** for destructive-safe workflow execution.

---

### Checkpoint - Global/Shell

Status: **PARTIAL**

Scenarios completed:
- Login, home, menu structure, Inventory/Sales/BOM routes, Vietnamese shell spot-check

Issues found:
- LOC-LOGIN-001 (English login labels)
- LOC-COOKIE-001 (English cookie banner)

Evidence saved:
- 00-login-form.png, 01-login-home.png, 01b-home-dashboard.png, 01c-inventory-hub.png, 01d-sales-list.png, 01e-bom-landing.png

Next module:
- Catalog (completed in same run)

### Checkpoint - Catalog

Status: **PARTIAL**

Scenarios completed:
- Product/Vật tư list, create with auto-code hint, PROD/MAT code generation

Issues found:
- None blocking

Evidence saved:
- 02-catalog-*.png

Next module:
- BOM/Pricing smoke

### Checkpoint - BOM / Pricing

Status: **SMOKE**

Scenarios completed:
- BOM landing, pricing product/component list pages load

Issues found:
- None in smoke

Evidence saved:
- 03-bom-landing.png, 03-pricing-products.png, 03-pricing-components.png

Next module:
- Inventory

### Checkpoint - Inventory

Status: **PARTIAL**

Scenarios completed:
- Warehouses, Receipt LotNo hint, Adjustment UI, Lots, Ledger filters

Issues found:
- UX-INV-001 (Mặt hàng tồn kho vs Vật tư label)

Evidence saved:
- 04-*, 05-inventory-ledger-source-reference.png

Next module:
- Sales/Payment

### Checkpoint - Sales/Payment

Status: **PARTIAL**

Scenarios completed:
- Sales list payment columns/badges/filter, create page, details payment summary + add payment form

Issues found:
- None blocking in executed scenarios

Evidence saved:
- 06-*, 07-sales-details-payment.png

Next module:
- Customer/Receivable

### Checkpoint - Customer/Receivable

Status: **PARTIAL**

Scenarios completed:
- Customer list, receivable summary with customer selected, link to Sales List

Issues found:
- UX-CH-001 (money formatting inconsistency)

Evidence saved:
- 08-customer-list.png, 08-customer-history-receivable-selected.png

Next module:
- Audit (completed); Validation/UI consistency deferred

### Checkpoint - Audit

Status: **SMOKE**

Scenarios completed:
- Audit list and export pages load

Evidence saved:
- 09-audit-list.png, 09-audit-export.png

---

*Audit artifacts: `docs/evidence/uat_snapshot_04a/findings.json`, Playwright runners `audit-runner.mjs`, `audit-workflow-ext.mjs`, `audit-fixups.mjs`.*
