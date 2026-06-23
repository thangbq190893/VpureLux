# VPureLux ERP UI Fix Backlog

This backlog follows the new UI refactor documentation. It prioritizes operator usability blockers before visual polish. No backend/business change is implied unless explicitly marked as requiring approval.

## Batch 1 - Inventory Receipt selector/technical-field cleanup

Priority: Critical

Scope:

- `src/VPureLux.Web/Pages/Inventory/Receipt.cshtml`
- `src/VPureLux.Web/Pages/Inventory/Receipt.cshtml.cs`
- optional page JS: `src/VPureLux.Web/Pages/Inventory/Receipt.js`
- `vi-VN.json`
- lightweight Web UI tests if requested

Required fixes:

- Replace visible `Input.WarehouseId` with Warehouse selector showing `Code - Name`.
- Replace visible `Input.Lines[0].StockItemId` with StockItem selector showing `CodeSnapshot - NameSnapshot`.
- Filter StockItem options to Active, Component, inventory-enabled phase 1 items.
- Render `Input.IdempotencyKey` as hidden.
- Keep `PostReceiptAsync` unchanged.
- Reload selector options after validation failure.
- Add confirmation/busy/notification if safe.

Classification:

- Existing app service usage plus UI-only fixes.

Exit criteria:

- No visible raw WarehouseId, StockItemId, or IdempotencyKey.
- No business behavior change.
- Receipt still posts through existing handler/service.

## Batch 2 - Inventory Issue selector/technical-field cleanup

Priority: Critical

Scope:

- `Pages/Inventory/Issue.cshtml`
- `Pages/Inventory/Issue.cshtml.cs`
- optional `Issue.js`
- localization and tests if requested

Required fixes:

- Warehouse selector.
- StockItem selector filtered to active Component inventory-enabled items.
- Hidden `Input.IdempotencyKey`.
- Preserve existing `PostIssueAsync`.
- Add confirmation/busy/notification if safe.

Classification:

- Existing app service usage plus UI-only fixes.

## Batch 3 - Inventory Adjustment selector/technical-field cleanup

Priority: Critical

Scope:

- `Pages/Inventory/Adjustment.cshtml`
- `Pages/Inventory/Adjustment.cshtml.cs`
- optional `Adjustment.js`

Required fixes:

- Warehouse selector.
- StockItem selector.
- Hidden `IdempotencyKey`.
- Keep Reason visible and required.
- Make increase/decrease fields understandable without moving rules to UI.
- Preserve existing `PostAdjustmentAsync`.
- Add confirmation/busy/notification if safe.

Classification:

- Existing app service usage plus UI-only fixes.

## Batch 4 - BOM Product/Component selector cleanup

Priority: Critical

Scope:

- `Pages/Bom/Index.cshtml`
- `Pages/Bom/Index.cshtml.cs`
- `Pages/Bom/Product.cshtml`
- `Pages/Bom/Product.cshtml.cs`
- `Pages/Bom/Create.cshtml`
- `Pages/Bom/Create.cshtml.cs`
- `Pages/Bom/Edit.cshtml`
- `Pages/Bom/Edit.cshtml.cs`
- `Pages/Bom/Details.cshtml`
- `Pages/Bom/Details.cshtml.cs`

Required fixes:

- Replace raw ProductId input with Product selector using existing Catalog Product list service.
- Replace ComponentId row inputs with Component selectors using existing Catalog Component list service.
- Display Product and Component labels as `Code - Name` where possible.
- Keep BOM editor full page.
- Preserve route templates and existing BOM service calls.
- Do not change BOM versioning, publishing, archiving, or clone behavior.

Classification:

- Existing app service usage; details display may need approved query/read DTO if UI lookup is insufficient.

## Batch 5 - Customer/CustomerGroups modal verification fixes if needed

Priority: Medium

Scope:

- `Pages/Customers/*`
- `Pages/CustomerGroups/*`
- `Pages/Customers/Index.js`
- `Pages/CustomerGroups/Index.js`

Required verification:

- Browser UAT confirms modal opens without URL navigation.
- Validation errors stay inside modal.
- Successful save closes modal, notifies, and reloads list.
- Full-page fallback routes still work.
- Code remains read-only on edit.

Classification:

- Test/manual UAT gap first; UI-only fixes only if browser UAT exposes issues.

## Batch 6 - Catalog image script cleanup

Priority: Medium

Scope:

- `Pages/Catalog/Products/Create.cshtml`
- `Pages/Catalog/Products/Edit.cshtml`
- `Pages/Catalog/Components/Create.cshtml`
- `Pages/Catalog/Components/Edit.cshtml`
- `Pages/Catalog/catalog-image-preview.js`

Required fixes:

- Replace raw `<script src>` with lowercase `scripts` section and `<abp-script>`.
- Ensure preview object URLs are safely revoked if applicable.
- Add remove-image confirmation if safe.
- Do not change image validation, image endpoints, storage, DTOs, domain events, or audit behavior.

Classification:

- UI-only.

## Batch 7 - Catalog action/status cleanup

Priority: Medium

Scope:

- `Pages/Catalog/Products/Index.cshtml`
- `Pages/Catalog/Products/Index.cshtml.cs`
- `Pages/Catalog/Components/Index.cshtml`
- `Pages/Catalog/Components/Index.cshtml.cs`

Required fixes:

- Convert actions to action-menu pattern.
- Add Deactivate confirmation/notification.
- Verify Product activation backend availability before rendering any Activate action.
- Keep Component Activate disabled/unavailable unless backend capability is approved.

Classification:

- UI-only for menu/confirmation; Product/Component activation is backend/business gap unless existing service exists.

## Batch 13 - UI Brand Polish / VPURELUX visual alignment

Priority: Deferred after functional UAT blockers

Scope:

- Global LeptonX theme overrides
- Navigation, typography, color palette, spacing
- Alignment with public site branding at `https://vpurelux.com`

Required fixes:

- Audit current admin UI against VPURELUX brand guidelines from the public site.
- Apply consistent primary/secondary colors, logo placement, and operator-facing typography.
- Do not change business logic, routes, permissions, or backend behavior.

Classification:

- UI-only visual polish. No backend/business change.

Exit criteria:

- Operator screens visually align with VPURELUX brand direction.
- No functional regressions in Catalog, Inventory, Sales, or Audit workflows.

## Batch 8 - Pricing display/action cleanup

Priority: Medium-Low

Scope:

- `Pages/Pricing/Index.cshtml`
- `Pages/Pricing/Components/History.cshtml`
- `Pages/Pricing/Components/Create.cshtml`
- `Pages/Pricing/Products/History.cshtml`
- `Pages/Pricing/Products/Create.cshtml`

Required fixes:

- Keep history links guarded by `Pricing.History`.
- Add Product/Component display context on history/create pages.
- Consider Create version modal only with full-page fallback preserved.
- Keep no update/delete routes.
- Do not add customer-specific pricing.

Classification:

- UI-only or existing Catalog service usage.

## Batch 9 - BOM inline script cleanup

Priority: Medium-Low after BOM selectors

Scope:

- `Pages/Bom/Create.cshtml`
- `Pages/Bom/Edit.cshtml`
- new or existing page JS if approved

Required fixes:

- Move inline row JavaScript to page-specific JS.
- Register with lowercase `scripts` section and `<abp-script>`.
- Preserve binding names and indexes.
- Do not change BOM item rules.

Classification:

- UI-only, but should happen after selector cleanup so the JS is not rewritten twice.

## Batch 10 - Inventory posting confirmations/busy/notification

Priority: Medium after selector cleanup

Scope:

- `Receipt`, `Issue`, `Adjustment` pages and page JS

Required fixes:

- `abp.message.confirm` before posting.
- `abp.ui.setBusy/clearBusy` around forms where safe.
- Success/failure feedback after redirect/post.
- No JavaScript FIFO, cost, or stock calculation.

Classification:

- UI-only.

## Batch 11 - Sales permission/selector review

Priority: Deferred until Inventory/BOM selectors are stable

Scope:

- `Pages/Sales/Create.cshtml`
- `Pages/Sales/Edit.cshtml`
- `Pages/Sales/Details.cshtml`
- `Pages/Sales/CustomerHistory.cshtml`

Required fixes:

- Replace visible CustomerId input in CustomerHistory with Customer selector.
- Replace CatalogItemId/BomVersionId inputs with approved item/BOM selector strategy.
- Keep profit guarded by `Sales.ViewProfit`.
- Guard cost fields with `Sales.ViewCost` if displayed.
- Add confirmations for confirm/cancel/remove-line.
- Do not calculate FIFO cost, profit, margin, or pricing in UI.

Classification:

- Customer selector can use existing service.
- Rich item/BOM selector likely requires approved selector/query contract.

## Batch 12 - Audit detail/export cleanup

Priority: Deferred after operational workflows

Scope:

- `Pages/Audit/Index.cshtml`
- `Pages/Audit/Details.cshtml`
- `Pages/Audit/Export.cshtml`
- optional page JS

Required fixes:

- Localize severity display.
- Improve details readability for entity display and JSON blocks.
- Keep payloads selective; do not show Base64 or full snapshots.
- Add export confirmation/busy/notification.
- Preserve `Audit.Export` protection.

Classification:

- UI-only.

## Cross-Batch Rules

- Preserve all route templates.
- Use `asp-page` and `asp-route-*`.
- Do not add `<abp-button href>`.
- Do not add hardcoded internal `href="/..."`.
- Do not add inline scripts or raw page-level `<script src>`.
- Do not touch Domain, Application behavior, Infrastructure, EF, migrations, DTOs, AppService methods, API contracts, permissions, or business rules without explicit approval.
