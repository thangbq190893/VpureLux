# VPureLux V2 Final UAT Bug Backlog

## 1. Audit Baseline

Audit date: 2026-06-19

Scope:

- `src/VPureLux.Web/Pages`
- `src/VPureLux.Web/Menus`
- `src/VPureLux.Domain.Shared/Localization/VPureLux/vi-VN.json`
- UI-related tests where needed for source inspection

Documents read:

- `CODEX_README_VPURELUX_V2.md`
- `BUSINESS_ARCHITECTURE_DECISIONS_V2.md`
- `MODULE_SPECIFICATIONS_V2.md`
- `UI_UX_ABP_GUIDE_V2.md`
- `V2_ARCHITECTURE_ALIGNMENT_REPORT.md`

Current certified/V2-aligned baseline from latest project state:

- Pricing V2 aligned.
- BOM V2 selectors and stability improved.
- Catalog activation aligned.
- Sales V2 Product/SKU-only backend aligned.
- Inventory raw-ID cleanup completed.
- Previous full solution test baseline: 271 passed / 0 failed.

This backlog is UAT/operator UX focused. It does not propose changes to certified domain rules, EF mappings, migrations, pricing semantics, inventory costing, Sales V2 Product/SKU-only behavior, or BOM business rules.

## 2. Severity Legend

| Severity | Meaning |
|---|---|
| P0 | Blocks build, login, navigation, or core UAT execution. |
| P1 | Blocks or seriously slows realistic operator UAT for a certified business flow. |
| P2 | Usability or ABP pattern gap that should be fixed before Go-Live, but has a workaround. |
| P3 | Polish, readability, or consistency improvement. |

## 3. Cross-Cutting Search Results

| Check | Result | Notes |
|---|---:|---|
| `<abp-button href>` in ERP Razor pages | 0 | No current misuse found. |
| hardcoded internal `href="/..."` in ERP Razor pages | 0 | Internal Razor navigation appears route-helper based. |
| inline `<script>` blocks in ERP Razor pages | 0 | No inline scripts found. |
| raw page-level `<script src>` in ERP Razor pages | 4 | Catalog image pages still use raw script tags. |
| PageModels calling DbContext/repositories/domain managers directly | 0 | PageModels appear to stay behind Application Services. |
| raw ID terms in Razor pages | Present as bindings/routes/helpers | Most are internal selectors, hidden fields, route parameters, or display helper inputs. Audit detail still exposes technical entity IDs. |

Raw script tag findings:

- `src/VPureLux.Web/Pages/Catalog/Products/Create.cshtml`
- `src/VPureLux.Web/Pages/Catalog/Products/Edit.cshtml`
- `src/VPureLux.Web/Pages/Catalog/Components/Create.cshtml`
- `src/VPureLux.Web/Pages/Catalog/Components/Edit.cshtml`

## 4. Top UAT Blockers

| ID | Severity | Area | Issue | Expected UAT Behavior | Recommended Fix Batch |
|---|---|---|---|---|---|
| UAT-001 | P1 | Catalog images | Product/Component image pages use raw `<script src="/Pages/Catalog/catalog-image-preview.js">` instead of ABP script registration. | ABP-compliant script registration using lowercase `scripts` section and `<abp-script>`. | Batch 1 |
| UAT-002 | P1 | Inventory posting | Receipt, Issue, and Adjustment workflows are still effectively single-line operator workflows. | Operators can add/remove multiple component lines in one posting while preserving model binding and validation state. | Batch 2 |
| UAT-003 | P1 | Inventory posting | Receipt, Issue, and Adjustment lack ABP confirmation, busy state, and success/error notification. | Posting actions clearly confirm intent and show completion/failure feedback. | Batch 2 |
| UAT-004 | P1 | Audit | Audit Detail is hard to read and exposes technical `EntityId`/raw JSON too directly. | Business audit details are readable, localized, and show entity context where available. JSON is formatted and safe. | Batch 3 |
| UAT-005 | P1 | Audit export | Audit export lacks confirmation, busy state, and completion notification. | Export action is confirmed, shows progress, and reports completion. | Batch 3 |
| UAT-006 | P1 | BOM | Publish/Archive actions lack ABP confirmation, busy state, and success/error notification. | Publishing/archiving requires confirmation and provides clear feedback. | Batch 5 |
| UAT-007 | P1 | Pricing | Product Suggested Price History can render as an empty timeline when no versions exist. | Show localized empty state: `Không tìm thấy phiên bản giá.` | Batch 4 |
| UAT-008 | P1 | Pricing | Product Suggested Price Create page does not show product context as clearly as component price create. | Show `Sản phẩm: {Code} - {Name}` on create/history pages. | Batch 4 |

## 5. Catalog Product/Component UI Findings

| ID | Severity | Finding | Current State | Required Fix |
|---|---|---|---|---|
| UAT-009 | P1 | Raw script registration for image preview. | Four Catalog Create/Edit pages use raw `<script src>`. | Replace with lowercase `scripts` section and `<abp-script src="/Pages/Catalog/catalog-image-preview.js" />`. |
| UAT-010 | P2 | Activate/Deactivate row actions lack confirmation/notification. | Actions are permission-aware and functional, but direct post actions provide limited feedback. | Add ABP confirmation, busy state, and success notification. |
| UAT-011 | P2 | Product/Component CRUD is full-page only. | Full-page Create/Edit/Details remain usable. | Convert to ABP modal pattern with existing full-page fallback preserved. |
| UAT-012 | P2 | Catalog row actions are direct buttons, not action menu. | Lists show direct action buttons. | Use ABP action-menu/dropdown pattern for Details/Edit/Activate/Deactivate. |
| UAT-013 | P2 | Product list lacks V2 commercial context. | Shows image, code, name, status, actions. | Add current suggested selling price and BOM status/context where existing Application Services allow. |
| UAT-014 | P2 | Component list lacks current suggested price context. | Shows image, code, name, unit, status, actions. | Add current component suggested selling price where existing Pricing services allow. |
| UAT-015 | P3 | Remove image has no confirmation. | Image remove is available. | Add confirmation and success notification. |

## 6. BOM UI Findings

| ID | Severity | Finding | Current State | Required Fix |
|---|---|---|---|---|
| UAT-016 | P1 | Publish/Archive lack confirmation/notification. | Full-page forms post correctly. | Add ABP confirmation, busy state, and localized success/error feedback. |
| UAT-017 | P2 | Product page lacks richer product context. | Product label and BOM versions are displayed. | Add product image, current suggested price, and component build price context where existing services allow. |
| UAT-018 | P2 | Component row add/remove needs UAT stress check. | Component selector and add/remove script exist. | Verify validation re-render preserves selected components and quantities across multiple rows. |
| UAT-019 | P3 | Clone date input is text-based but needs clearer helper text. | `dd/MM/yyyy` placeholder exists. | Add localized helper text and validation summary clarity. |

## 7. Pricing UI Findings

| ID | Severity | Finding | Current State | Required Fix |
|---|---|---|---|---|
| UAT-020 | P1 | Product price history empty state is inconsistent. | Component history has explicit empty state; product history can show blank timeline. | Add explicit localized empty state. |
| UAT-021 | P1 | Product price create lacks product context. | Component create shows component context; product create should mirror it. | Show `Sản phẩm: {Code} - {Name}`. |
| UAT-022 | P2 | Component pricing tab does not show current suggested price/effective date. | Component tab lists code/name/actions only. | Add current component suggested selling price and effective date. |
| UAT-023 | P2 | Price create/history pages remain full-page. | Functional full-page workflow. | Convert to modal pattern later, preserving routes. |
| UAT-024 | P3 | Date input UX should be regression-checked after browser UAT. | Pricing date fields are intended to use `dd/MM/yyyy`. | Verify across Chrome/Edge Vietnamese locale. |

## 8. Inventory UI Findings

| ID | Severity | Finding | Current State | Required Fix |
|---|---|---|---|---|
| UAT-025 | P1 | Posting workflows need multi-line support. | Receipt/Issue/Adjustment show one line despite DTO line collections. | Add add/remove line UI with stable indexed model binding. |
| UAT-026 | P1 | Posting workflows lack confirmation/busy/notification. | Full-page postbacks work but feedback is minimal. | Add ABP confirmation, busy state, and localized post result notifications. |
| UAT-027 | P2 | Inventory date fields may rely on native/browser date behavior. | Receipt/Adjustment use direct date binding. | Standardize to Vietnamese `dd/MM/yyyy` text input pattern. |
| UAT-028 | P2 | Balance/Lot/Ledger inquiry pages lack filters. | Pages display code-name labels, but filtering is limited. | Add warehouse and stock item selectors using existing query support. |
| UAT-029 | P2 | Warehouse management is compact and non-modal. | Inline create table workflow exists. | Apply ABP modal/action-menu pattern and confirmations. |
| UAT-030 | P3 | Adjustment type display may need localization review. | Enum select is rendered by ABP. | Verify Vietnamese labels in UAT and localize if needed. |

## 9. Sales UI Findings

| ID | Severity | Finding | Current State | Required Fix |
|---|---|---|---|---|
| UAT-031 | P2 | Product selector is code-name only. | Product/SKU-only workflow is present. | Add image/status context if existing services expose it. |
| UAT-032 | P2 | BOM visibility is text-only. | UI can indicate whether published BOM exists. | Add BOM component availability preview only if approved Application query exists or is explicitly approved. |
| UAT-033 | P2 | User-friendly business errors need UAT review. | Backend BusinessException localization handles many errors. | Verify missing BOM, missing price, inactive customer, insufficient stock messages are operator-friendly. |
| UAT-034 | P2 | Cost display is not exposed in details. | Profit-sensitive display is protected; cost appears hidden. | If UAT requires cost visibility, add `Sales.ViewCost` protected FIFO cost/allocation display. |
| UAT-035 | P3 | Customer history is functional but could improve readability. | Customer selector and product code-name display exist. | Add summary cards and export later if approved. |

## 10. Audit UI Findings

| ID | Severity | Finding | Current State | Required Fix |
|---|---|---|---|---|
| UAT-036 | P1 | Audit detail readability is weak for operators. | Detail page shows technical fields and raw JSON. | Add formatted JSON, localized field names, business entity labels, and safer layout. |
| UAT-037 | P1 | Export flow lacks confirmation/busy/notification. | Export page posts directly. | Add ABP confirmation, busy state, and success/failure notification. |
| UAT-038 | P2 | Audit index severity display is raw enum-like. | Severity is rendered directly. | Localize severity labels and use badges. |
| UAT-039 | P2 | EntityId is visible as raw technical ID. | Audit details may expose raw entity identifiers. | Show code-name/entity context when available; keep raw ID collapsed or secondary for support use. |

## 11. Cross-Cutting ABP Rule Findings

| ID | Severity | Finding | Current State | Required Fix |
|---|---|---|---|---|
| UAT-040 | P1 | Raw script tags remain in Catalog. | Four matches. | Replace with `<abp-script>`. |
| UAT-041 | P2 | Several operational post actions lack ABP confirmation/busy state. | Inventory, BOM, Catalog, Audit are inconsistent with Customer/Sales reference patterns. | Apply shared confirmation/notification pattern by module. |
| UAT-042 | P2 | Modal pattern adoption is uneven. | Customers/CustomerGroups are closest to reference; Catalog/Pricing/Inventory remain full-page. | Refactor gradually, preserving all routes. |
| UAT-043 | P2 | Operator-facing labels need final Vietnamese UAT review. | Localization is broad but not fully human-verified. | Run Vietnamese terminology pass during UAT. |

## 12. Backend Gap vs UI-Only Gap

| Category | Items | Classification |
|---|---|---|
| UI-only fixes | UAT-001, UAT-003, UAT-005, UAT-006, UAT-007, UAT-008, UAT-010, UAT-015, UAT-016, UAT-020, UAT-021, UAT-026, UAT-036, UAT-037, UAT-038 | Can be handled in Razor/PageModel/JS/localization using existing Application Services. |
| Existing AppService/query likely enough | UAT-013, UAT-014, UAT-022, UAT-028, UAT-031, UAT-034 | Verify DTO fields before implementation. |
| Requires explicit approval if new query/contract is needed | UAT-017, UAT-032, UAT-039 | Do not add new contracts unless approved. |
| UAT verification before code | UAT-018, UAT-024, UAT-030, UAT-033, UAT-043 | Confirm actual browser/operator behavior first. |
| Larger UI refactor | UAT-002, UAT-011, UAT-012, UAT-023, UAT-025, UAT-029, UAT-042 | Batch carefully with focused tests. |

## 13. Recommended Fix Batches

### Batch 1: Catalog Script Compliance and Image Action Safety

Fix:

- UAT-001
- UAT-009
- UAT-015
- UAT-040

Expected outcome:

- No raw script tags in Catalog.
- Image preview remains working.
- Remove/replace image operations have confirmation/notification.

### Batch 2: Inventory Posting UAT Workflow

Fix:

- UAT-002
- UAT-003
- UAT-025
- UAT-026
- UAT-027

Expected outcome:

- Receipt/Issue/Adjustment support realistic multi-line operator entry.
- Idempotency remains hidden.
- `dd/MM/yyyy` date input is consistent.
- Posting actions have confirmation, busy state, and success/error notification.

### Batch 3: Audit Readability and Export UX

Fix:

- UAT-004
- UAT-005
- UAT-036
- UAT-037
- UAT-038
- UAT-039, if existing display data is available

Expected outcome:

- Audit is usable by business operators and support users.
- Export action is safe and understandable.

### Batch 4: Pricing UAT Polish

Fix:

- UAT-007
- UAT-008
- UAT-020
- UAT-021
- UAT-022

Expected outcome:

- Product pricing empty states and context match component pricing.
- Component tab shows current suggested price context.

### Batch 5: BOM Operational Feedback and Context

Fix:

- UAT-006
- UAT-016
- UAT-017, if existing display data is available
- UAT-018 after UAT verification

Expected outcome:

- BOM publish/archive are safer.
- BOM pages show stronger product and pricing context.

### Batch 6: Catalog ABP Modal and Action Menu Consistency

Fix:

- UAT-010
- UAT-011
- UAT-012
- UAT-013
- UAT-014

Expected outcome:

- Catalog aligns with the Customer/CustomerGroups reference UI pattern.

### Batch 7: Sales Operator Context Review

Fix:

- UAT-031
- UAT-032, only with approval if new query support is required
- UAT-033
- UAT-034, if UAT requires cost visibility
- UAT-035

Expected outcome:

- Sales remains Product/SKU-only and becomes clearer for operators.

### Batch 8: Inventory Inquiry and Warehouse Management Polish

Fix:

- UAT-028
- UAT-029
- UAT-030

Expected outcome:

- Inventory inquiries and warehouse management become easier to operate.

## 14. Recommended Immediate Next Step

Start with Batch 1: Catalog Script Compliance and Image Action Safety.

Rationale:

- It fixes the only concrete cross-cutting ABP anti-pattern currently found by `rg`.
- It is low risk and confined to the Presentation Layer.
- It improves image UAT without touching certified Catalog business logic.

## 15. Out of Scope for This Backlog

- Sales domain/application redesign.
- Pricing semantics changes.
- Inventory FIFO or UnitCost behavior changes.
- BOM aggregate or persistence changes.
- New migrations.
- New business permissions.
- Product inventory.
- Direct Component sales.
- Warranty, serial number, mobile app, dashboard, or reporting expansion.
