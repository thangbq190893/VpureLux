# VPureLux ERP UI Refactor Source of Truth

## 1. Purpose

This document defines the authoritative UI refactor rules for VPureLux ERP.

The ERP backend is already certified. The UI refactor must make the system easier to operate while preserving all certified business behavior.

## 2. Scope

Allowed by default:

- Razor Pages markup.
- Page-specific JavaScript.
- Localization keys.
- Navigation rendering.
- Permission-aware visibility.
- UI-only PageModel support such as option lists loaded from existing application services.
- Tests for rendered UI behavior.

Forbidden by default:

- Domain changes.
- Application service behavior changes.
- New application service/API contracts.
- Repository or EF Core changes.
- Migrations.
- Database schema changes.
- Business validation changes.
- Permission definition changes.
- State machine changes.
- API route changes.
- Product Inventory enablement.
- Sales/BOM/Inventory business feature expansion.

## 3. Business Specs Remain Authoritative

These UI documents do not replace certified module specifications.

Important certified rules to preserve:

- Customer Code and CustomerGroup Code are required, unique among non-deleted records, and immutable after creation.
- Customer and CustomerGroup support activate/deactivate and expose explicit permissions for view/create/edit/manage-status.
- Inventory uses Generic StockItem architecture.
- Phase 1 inventory operations are enabled only for Component StockItems.
- Product StockItems are structurally supported but inventory-disabled in phase 1.
- InventoryTransactions are the source of truth.
- InventoryBalance is a rebuildable read model.
- FIFO order is `ReceivedAt ASC, CreationTime ASC, Id ASC`.
- Inactive Warehouses and StockItems cannot be used.
- Inventory-disabled StockItems cannot be used.
- Receipt/Issue/Adjustment posting operations are atomic and idempotent.
- Pricing owns only Component Suggested Selling Price Versions and Product Suggested Price Versions.
- Pricing does not own Actual Selling Price, Inventory FIFO issue cost, Sales profit, or Customer-specific pricing.
- Sales owns Actual Selling Price, snapshots, revenue, cost, profit, and customer purchase history.
- Catalog images must not leak Base64 content into general DTOs, Razor HTML, logs, domain events, or audit payloads.
- Audit business records are append-only and must not be deleted or mutated.

## 4. Operator-Visible Data Rules

### 4.1 Raw GUID Rule

Normal ERP operators must not type, copy, or interpret raw GUIDs in daily workflows.

A GUID may be submitted as a hidden value or option value, but the visible UI must show business identifiers:

- `Code - Name`
- `OrderNo`
- `LotNo`
- `EntityDisplay`
- localized status labels

Examples:

| Technical value | Visible label |
|---|---|
| `WarehouseId` | `Kho` with display `WH-HN - Kho Hà Nội` |
| `StockItemId` | `Mặt hàng tồn kho` or `Linh kiện`, display `COMP-PP1M - Lõi PP 1 Micron` |
| `ProductId` | `Sản phẩm`, display `PROD-RO-001 - Máy lọc nước RO tiêu chuẩn` |
| `CustomerId` | `Khách hàng`, display `CUS-DL001 - Đại lý Minh Anh` |
| `CustomerGroupId` | `Nhóm khách hàng`, display `CG-DL1 - Đại lý cấp 1` |

### 4.2 Technical Fields

Technical fields must not be visible to operators unless explicitly approved.

Always hidden or server-managed:

- `Id`
- `IdempotencyKey`
- `ConcurrencyStamp`
- `TenantId`
- `CreatorId`
- `LastModifierId`
- internal route IDs when a code/name display is available

### 4.3 Code Fields

Business `Code` is not a technical GUID. Code is operator-facing and may remain visible and required where certified business rules require it.

Code should be explained as a business identifier, not confused with the database ID.

Examples:

- Product: `PROD-RO-001`
- Component: `COMP-PP1M`
- Customer: `CUS-DL001`
- Customer Group: `CG-DL1`
- Warehouse: `WH-HN`

Do not auto-generate Code in JavaScript. Auto-generation is a business/application-layer change and needs separate approval.

## 5. Modal vs Full-Page Rules

Use modal + full-page fallback for simple CRUD:

- Customer Create/Edit/Details.
- CustomerGroup Create/Edit/Details.
- Warehouse Create/Edit/Details if small and supported by existing handlers.
- Pricing version create if small and route fallback exists.
- Read-only Audit detail.

Keep full page for operational workflows:

- BOM Create/Edit editor.
- Inventory Receipt.
- Inventory Issue.
- Inventory Adjustment.
- Sales Order Create/Edit/Details.
- Audit Reports/Export.

Catalog Product/Component Create/Edit may become modal only if image upload validation and fallback routes are proven safe. Otherwise keep full page and fix selectors/action/menu/scripts first.

## 6. Route Preservation

Existing public/internal Razor Page routes must remain reachable unless a redirect is explicitly approved.

When a modal page is added, it must not replace the full-page fallback route.

Example:

| Route | Purpose |
|---|---|
| `/Customers/Create` | full-page fallback |
| `/Customers/CreateModal` | modal view route |
| `/Customers/Edit/{id}` | full-page fallback |
| `/Customers/EditModal?id=...` | modal view route |

Navigation should use `asp-page` and `asp-route-*`, not hardcoded `href="/..."`.

Never put `href` on `<abp-button>`.

## 7. Permission Rules

Every visible action must use the same permission as the backend operation or destination page.

Rules:

- Create button: create permission.
- Edit link: edit permission.
- Details link: view permission.
- Activate/deactivate/status action: status/manage permission or backend operation permission.
- Export: export permission.
- Sensitive field: data permission, not only page permission.

UI permission checks supplement backend authorization. They do not replace it.

## 8. Selector Rules

A selector can be implemented only using existing approved data sources unless backend work is explicitly approved.

Allowed selector data source:

- Existing application service list/get method already exposed to the Web layer.
- Existing DTO that contains enough information to display `Code - Name`.

Not allowed by default:

- New AppService methods.
- New API endpoints.
- Direct repository/DbContext access from PageModel.
- Hardcoded IDs.
- Querying domain entities directly from UI.

If no existing approved data source exists, do not fake the selector. Report it as `requires approved selector/query contract`.

## 9. JavaScript Rules

- Use lowercase `@section scripts`.
- Register scripts with `<abp-script>`.
- No inline module scripts in `.cshtml`.
- No raw page-level `<script src>`.
- Page JS must be local to the page or a clearly shared approved script.
- Use `abp.message.confirm` for destructive/sensitive actions.
- Use `abp.notify.success/error` for user feedback.
- Use `abp.ui.setBusy/clearBusy` where safe.
- Use `abp.ModalManager` only for approved modal candidates.

## 10. Acceptance Definition For Modal

A modal implementation is accepted only if all are true:

1. The Index/List page registers a page-specific JS file via `<abp-script>`.
2. JS creates `new abp.ModalManager({ viewUrl: ... })`.
3. The click handler calls `event.preventDefault()` when using fallback anchors.
4. The modal opens without changing the browser URL.
5. On successful save, modal closes, shows success notification, and reloads or refreshes the list.
6. On validation error, modal stays open and shows validation errors.
7. Full-page fallback route still works when opened directly.
8. Modal PageModel has correct `[Authorize]` policy.
9. Modal PageModel uses existing application service behavior only.

## 11. Acceptance Definition For Full-Page Operational Workflow

A full-page workflow is acceptable only if:

1. Operators do not enter raw GUIDs.
2. Technical fields are hidden.
3. Required domain fields are clearly labeled.
4. Posting action has confirmation.
5. Posting action has busy state.
6. Success/failure feedback is visible.
7. Validation failure preserves option lists and selected values.
8. The route remains stable.
9. No business computation is implemented in JavaScript.

## 12. Implementation Priority

Highest priority:

1. Remove raw GUID blockers from Inventory Receipt/Issue/Adjustment and BOM selection.
2. Hide technical fields such as IdempotencyKey.
3. Preserve certified backend behavior.
4. Fix misleading status actions.
5. Fix permission leaks and sensitive fields.
6. Clean raw/inline scripts.
7. Apply modal/action-menu pattern where appropriate.

Do not prioritize visual polish over usability blockers.
