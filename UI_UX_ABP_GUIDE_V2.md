# VPureLux ERP UI/UX ABP Guide V2

## Global UI Rules

1. Do not show raw GUIDs to operators.
2. Use Code - Name selectors.
3. Technical fields such as IdempotencyKey are hidden.
4. Use `asp-page` and `asp-route-*` for navigation.
5. Never use `<abp-button href="...">`.
6. No hardcoded internal `href="/..."`.
7. No inline script.
8. No raw page-level `<script src>`.
9. Use lowercase `scripts` section and `<abp-script>`.
10. Use ABP ModalManager for simple CRUD.
11. Use full-page workflows for complex operational screens.
12. Never move business rules into Razor Pages or JavaScript.
13. Do not calculate FIFO, profit, stock availability, or business state in UI.
14. UI permission checks must match backend permissions.

## Modal Pattern

Use ABP modals for Product, Component, Customer, CustomerGroup, Warehouse, price version create/history, and safe read-only details.

A modal implementation is accepted only if:

1. Modal page uses `<abp-modal>`.
2. List page registers JS with `<abp-script>`.
3. JS creates `abp.ModalManager`.
4. Click handler calls `event.preventDefault()`.
5. Modal opens without changing browser URL.
6. Save closes modal, notifies, and refreshes list.
7. Validation errors render inside modal.
8. Full-page fallback route still works where applicable.

## Full-Page Workflow Pattern

Use full pages for BOM editor, Inventory Receipt/Issue/Adjustment, Sales Order create/edit/details, and Audit export/report.

Full-page workflows must use selectors, validation summary, confirmations, busy state, and notifications. They must not calculate business values in UI.

## Selector Pattern

Selectors submit IDs internally but display business labels:

| Field | UI |
|---|---|
| ProductId | `Mã sản phẩm - Tên sản phẩm` |
| ComponentId | `Mã vật tư - Tên vật tư` |
| WarehouseId | `Mã kho - Tên kho` |
| StockItemId | `Mã vật tư - Tên vật tư` |
| CustomerId | `Mã khách hàng - Tên khách hàng` |

If selector data is not available from existing app services, report missing contract and stop.

## Catalog UI

Product list columns: image, mã sản phẩm, tên sản phẩm, trạng thái, giá bán đề xuất hiện tại, định mức vật tư status, actions.

Product actions: Details, Edit, Định mức vật tư, Kích hoạt/Ngừng sử dụng.

Component list columns: image, mã vật tư, tên vật tư, đơn vị, trạng thái, giá bán đề xuất hiện tại, actions.

Component actions: Details, Edit, Kích hoạt/Ngừng sử dụng.

## BOM UI

BOM index must not expose ProductId textbox. Use Product selector/search.

BOM Product page shows Product image/code/name, BOM versions, current Product Suggested Price, Giá cấu thành vật tư, and missing component price warnings.

BOM Create/Edit remains full page with Component selector rows, quantity, optional usage label/note, add/remove row, external JS.

## Pricing UI

Main label: `Quản lý giá bán`.

Tabs:

1. `Giá bán đề xuất vật tư`.
2. `Giá bán đề xuất sản phẩm`.

Do not show old `Giá mua vật tư` in V2.

Product price tab columns: mã sản phẩm, tên sản phẩm, định mức, Giá cấu thành vật tư, giá bán đề xuất hiện tại, chênh lệch, actions.

## Inventory UI

Receipt header: Kho, Ngày nhập, IdempotencyKey hidden.

Receipt lines: Vật tư nhập kho, Số lô, Số lượng, Đơn giá nhập thực tế, remove line.

Issue and Adjustment follow the same selector principles.

## Sales UI

Use a single Product/SKU selector. Do not expose direct Component sale lines.

Sales line display: Product image/code/name, quantity, suggested product selling price, actual selling price, published BOM version, availability status, cost/profit only by permission.

## Audit UI

Audit should show localized severity, readable JSON, and export confirmation/busy/notification. It must never show Base64 image content.
