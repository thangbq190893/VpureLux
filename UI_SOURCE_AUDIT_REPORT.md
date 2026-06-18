# VPureLux ERP UI Source Audit Report

## Executive Summary

Audit mode only. No production code, tests, migrations, DTOs, AppServices, API contracts, permissions, EF mappings, domain rules, or business behavior were changed.

The current UI source is partially aligned with the new UI refactor documents. Customer and CustomerGroups are the strongest reference implementation: both have action menus, ModalManager hooks, localized labels, full-page fallback routes, status confirmation, busy state, and success notification hooks. The largest operator blockers remain in Inventory and BOM:

- Inventory Receipt, Issue, and Adjustment expose raw `WarehouseId`, `StockItemId`, and `IdempotencyKey` inputs.
- Inventory Balances, Lots, and Ledger display raw warehouse/stock item IDs.
- BOM entry and detail pages expose raw `ProductId` and `ComponentId`, and BOM Create/Edit still use inline JavaScript.
- Sales still exposes raw item/BOM IDs in Create/Edit and raw `CustomerId` in CustomerHistory; Sales is high risk and should remain behind Inventory/BOM selector cleanup.
- Catalog image Create/Edit pages still register scripts using raw `<script src>`.
- Catalog Component reactivation is a confirmed backend/business gap; Product reactivation still needs source verification.

## Documentation Baseline

New UI refactor documents present and read:

| Document | Status |
|---|---|
| `CODEX_UI_REFACTOR_README.md` | Read first |
| `UI_REFACTOR_SOURCE_OF_TRUTH.md` | Read |
| `UI_IMPLEMENTATION_DECISION_MATRIX.md` | Read |
| `UI_ABP_IMPLEMENTATION_RULES.md` | Read |
| `UI_BACKEND_GAP_REGISTER.md` | Read |
| `UI_REFACTOR_ROADMAP_AND_VALIDATION.md` | Read |
| `UI_UAT_FLOW_TEST_PLAN.md` | Read |

Certified module specifications read:

| Document | Status |
|---|---|
| `CUSTOMER_MODULE_IMPLEMENTATION_SPECIFICATION.md` | Read |
| `INVENTORY_MODULE_IMPLEMENTATION_SPECIFICATION.md` | Read |
| `PRICING_MODULE_IMPLEMENTATION_SPECIFICATION.md` | Read |
| `SALES_MODULE_IMPLEMENTATION_SPECIFICATION.md` | Read |
| `CATALOG_IMAGE_EXTENSION_IMPLEMENTATION_SPECIFICATION.md` | Read |
| `AUDIT_MODULE_IMPLEMENTATION_SPECIFICATION.md` | Read |

Stale UI documents under `Deleted\` were not used.

## Current Source State Summary

Audited source areas:

- `src/VPureLux.Web/Pages`
- `src/VPureLux.Web/Menus`
- `src/VPureLux.Domain.Shared/Localization/VPureLux/vi-VN.json`
- `test/VPureLux.Web.Tests`
- `src/VPureLux.Application.Contracts`
- `src/VPureLux.Application` only for selector/data-source readiness

Page counts:

| Area | Count |
|---|---:|
| Razor `.cshtml` files under `Pages` | 55 |
| PageModel `.cshtml.cs` files under `Pages` | 54 |
| ERP module Razor pages audited | 49 |
| Framework/public shell pages scanned | 6 |

Mandatory search results:

| Search | Count | Findings |
|---|---:|---|
| `<abp-button ... href=` | 0 | No findings |
| `href="/` | 1 | `Pages/Index.cshtml` login link `/Account/Login`; not ERP internal module navigation |
| inline `<script>` | 2 | `Pages/Bom/Create.cshtml`, `Pages/Bom/Edit.cshtml` |
| raw `<script src>` | 4 | Catalog Product/Component Create/Edit image preview registrations |
| ABP JS APIs | 14 lines | Only Customer and CustomerGroups currently use ModalManager/confirm/notify/busy |

Menu audit:

- `VPureLuxMenuContributor` uses localized menu labels.
- ERP module menu routes point to current root pages: Catalog, BOM, Customers, CustomerGroups, Pricing, Inventory, Sales, Audit.
- Menu permissions are present and broadly match destination page view permissions.
- No full menu hierarchy redesign is recommended before operator blockers are fixed.

## Page Inventory Matrix

| Module | Page | Route | PageModel | Authorization | Handlers/App Services | Current UI Mode | Target UI Mode | Gap | Risk |
|---|---|---|---|---|---|---|---|---|---|
| Catalog | Components Index | `/Catalog/Components` | `Catalog.Components.IndexModel` | Catalog Components View plus action flags | `IComponentAppService.GetListAsync`, `DeactivateAsync` | List page | List with action menu/confirm | No confirm/notification; inactive activation disabled due backend gap | Medium |
| Catalog | Components Create | `/Catalog/Components/Create` | `CreateModel` | Components.Create | `CreateAsync`, optional `SetImageAsync` | Full page | Full-page cautious modal candidate later | raw script registration | Medium |
| Catalog | Components Edit | `/Catalog/Components/Edit/{id}` | `EditModel` | Components.Edit | `GetAsync`, `UpdateAsync`, image methods | Full page | Full page until image flow proven | raw script registration, remove image no confirm | Medium |
| Catalog | Products Index | `/Catalog/Products` | `Catalog.Products.IndexModel` | Catalog Products View plus action flags | `IProductAppService.GetListAsync`, `DeactivateAsync` | List page | List with action menu/confirm | deactivate always shown; product activate gap needs verification | Medium |
| Catalog | Products Create | `/Catalog/Products/Create` | `CreateModel` | Products.Create | `CreateAsync`, optional `SetImageAsync` | Full page | Full-page cautious modal candidate later | raw script registration | Medium |
| Catalog | Products Edit | `/Catalog/Products/Edit/{id}` | `EditModel` | Products.Edit | `GetAsync`, `UpdateAsync`, image methods | Full page | Full page until image flow proven | raw script registration, remove image no confirm | Medium |
| BOM | Index | `/Bom` | `Bom.IndexModel` | Bom.View | redirect handler only | Full page lookup | Full page with product selector | visible `ProductId` GUID input | Critical |
| BOM | Product | `/Bom/Product/{productId}` | `Bom.ProductModel` | Bom.View, action flags | `IBomAppService.GetListAsync`, Publish/Archive | Full page history | Full page history | visible `ProductId`; publish/archive no confirm | High |
| BOM | Create | `/Bom/Create/{productId}` | `Bom.CreateModel` | Bom.Create | `IBomAppService.CreateAsync` | Full-page editor | Full-page editor | visible `ComponentId`; inline JS | Critical |
| BOM | Edit | `/Bom/Edit/{id}` | `Bom.EditModel` | Bom.Create | `GetAsync`, `UpdateAsync` | Full-page editor | Full-page editor | visible `ComponentId`; inline JS; redirects non-draft | Critical |
| BOM | Details | `/Bom/Details/{id}` | `Bom.DetailsModel` | Bom.View | `GetAsync` | Full page detail | Full page/read-only modal later | displays ProductId and ComponentId GUIDs | High |
| BOM | Clone | `/Bom/Clone/{id}` | `Bom.CloneModel` | Bom.Create | `CloneAsync` | Full page | Full page/modal candidate later | no confirm/busy/notification | Low |
| Customer | Customers Index | `/Customers` | `Customers.IndexModel` | Customers.View | `GetListAsync`, Activate/Deactivate | List + modals | Reference pattern | needs browser UAT | Low |
| Customer | Customers Create | `/Customers/Create` | `CreateModel` | Customers.Create | `CreateAsync`, group list | Full-page fallback | Preserve fallback | OK; dropdown loaded | Low |
| Customer | Customers CreateModal | `/Customers/CreateModal` | `CreateModalModel` | Customers.Create | `CreateAsync`, group list | Modal | Modal | needs browser validation | Low |
| Customer | Customers Edit | `/Customers/Edit/{id}` | `EditModel` | Customers.Edit | `GetAsync`, `UpdateAsync`, group list | Full-page fallback | Preserve fallback | Code readonly; validate dropdown reload behavior | Low |
| Customer | Customers EditModal | `/Customers/EditModal?id=...` | `EditModalModel` | Customers.Edit | `GetAsync`, `UpdateAsync`, group list | Modal | Modal | needs browser validation | Low |
| Customer | Customers Details | `/Customers/Details/{id}` | `DetailsModel` | Customers.View | `GetAsync` | Full-page fallback | Preserve fallback | OK | Low |
| Customer | Customers DetailsModal | `/Customers/DetailsModal?id=...` | `DetailsModalModel` | Customers.View | `GetAsync` | Modal | Modal | needs browser validation | Low |
| CustomerGroups | Index | `/CustomerGroups` | `CustomerGroups.IndexModel` | CustomerGroups.View | `GetListAsync`, Activate/Deactivate | List + modals | Reference pattern | needs browser UAT | Low |
| CustomerGroups | Create | `/CustomerGroups/Create` | `CreateModel` | CustomerGroups.Create | `CreateAsync` | Full-page fallback | Preserve fallback | OK | Low |
| CustomerGroups | CreateModal | `/CustomerGroups/CreateModal` | `CreateModalModel` | CustomerGroups.Create | `CreateAsync` | Modal | Modal | needs browser validation | Low |
| CustomerGroups | Edit | `/CustomerGroups/Edit/{id}` | `EditModel` | CustomerGroups.Edit | `GetAsync`, `UpdateAsync` | Full-page fallback | Preserve fallback | Code readonly | Low |
| CustomerGroups | EditModal | `/CustomerGroups/EditModal?id=...` | `EditModalModel` | CustomerGroups.Edit | `GetAsync`, `UpdateAsync` | Modal | Modal | needs browser validation | Low |
| CustomerGroups | Details | `/CustomerGroups/Details/{id}` | `DetailsModel` | CustomerGroups.View | `GetAsync` | Full-page fallback | Preserve fallback | OK | Low |
| CustomerGroups | DetailsModal | `/CustomerGroups/DetailsModal?id=...` | `DetailsModalModel` | CustomerGroups.View | `GetAsync` | Modal | Modal | needs browser validation | Low |
| Pricing | Index | `/Pricing` | `Pricing.IndexModel` | Pricing.View | Catalog product/component list services | Inquiry list | Inquiry list | history links correctly guarded by `Pricing.History` | Low |
| Pricing | Component History | `/Pricing/Components/{componentId}` | `Pricing.Components.HistoryModel` | Pricing.History | history/current/date lookup | Full page | Full page | route id acceptable; title lacks item display context | Low |
| Pricing | Component Create | `/Pricing/Components/Create/{componentId}` | `CreateModel` | ComponentPurchasePrices.Create | `CreateAsync` | Full page | Modal candidate | no modal/notify; no item display context | Low |
| Pricing | Product History | `/Pricing/Products/{productId}` | `Pricing.Products.HistoryModel` | Pricing.History | history/current/date lookup | Full page | Full page | route id acceptable; title lacks item display context | Low |
| Pricing | Product Create | `/Pricing/Products/Create/{productId}` | `CreateModel` | ProductSuggestedPrices.Create | `CreateAsync` | Full page | Modal candidate | no modal/notify; no item display context | Low |
| Inventory | Index | `/Inventory` | `Inventory.IndexModel` | Inventory.View | authorization only | Hub page | Hub page | OK | Low |
| Inventory | Warehouses | `/Inventory/Warehouses` | `WarehousesModel` | ManageWarehouses | warehouse app service | List + inline create | List; modal candidate later | status action no confirm/notification | Medium |
| Inventory | Receipt | `/Inventory/Receipt` | `ReceiptModel` | Inventory.Receive | `PostReceiptAsync` | Full page workflow | Full-page operational workflow | visible `WarehouseId`, `StockItemId`, `IdempotencyKey`; no selector/confirm/busy | Critical |
| Inventory | Issue | `/Inventory/Issue` | `IssueModel` | Inventory.Issue | `PostIssueAsync` | Full page workflow | Full-page operational workflow | visible `WarehouseId`, `StockItemId`, `IdempotencyKey`; no selector/confirm/busy | Critical |
| Inventory | Adjustment | `/Inventory/Adjustment` | `AdjustmentModel` | Inventory.Adjust | `PostAdjustmentAsync` | Full page workflow | Full-page operational workflow | visible `WarehouseId`, `StockItemId`, `IdempotencyKey`; no selector/confirm/busy | Critical |
| Inventory | Balances | `/Inventory/Balances` | `BalancesModel` | implicit page currently not attributed in source snippet | `IInventoryQueryAppService.GetBalancesAsync` | Inquiry | Inquiry | displays raw warehouse/stock item IDs | High |
| Inventory | Lots | `/Inventory/Lots` | `LotsModel` | implicit page currently not attributed in source snippet | `GetLotsAsync` | Inquiry | Inquiry | displays raw stock item ID | High |
| Inventory | Ledger | `/Inventory/Ledger` | `LedgerModel` | implicit page currently not attributed in source snippet | `GetLedgerAsync` | Inquiry | Inquiry | displays raw warehouse ID; ledger app service has ViewLedger | High |
| Sales | Index | `/Sales` | `Sales.IndexModel` | Sales.View | `GetListAsync` | List | List | Customer fallback to raw CustomerId if snapshot missing | Medium |
| Sales | Create | `/Sales/Create` | `CreateModel` | Sales.Create | Customer/Warehouse lists, `CreateAsync` | Full page workflow | Full page workflow | item/BOM line IDs are raw inputs | High |
| Sales | Edit | `/Sales/Edit/{id}` | `EditModel` | Sales.Edit | `GetAsync`, line handlers | Full page workflow | Full page workflow | visible CatalogItemId, BomVersionId; remove no confirm | High |
| Sales | Details | `/Sales/Details/{id}` | `DetailsModel` | Sales.View | `GetAsync`, Confirm/Cancel | Full page detail | Full page detail | profit guarded; item fallback to CatalogItemId; confirm/cancel no confirm/busy | Medium |
| Sales | History | `/Sales/History` | `HistoryModel` | Sales.View | `GetListAsync` | Inquiry | Inquiry | profit guarded; OK | Low |
| Sales | CustomerHistory | `/Sales/CustomerHistory` | `CustomerHistoryModel` | ViewCustomerHistory + ViewProfit | `GetCustomerHistoryAsync` | Inquiry | Inquiry | visible CustomerId input and item CatalogItemId | High |
| Audit | Index | `/Audit` | `Audit.IndexModel` | Audit.View | `GetListAsync`, export flag | Search/list | Search/list | severity raw enum display | Medium |
| Audit | Details | `/Audit/Details/{id}` | `DetailsModel` | Audit.View | `GetAsync` | Full detail | Full detail/modal candidate | displays EntityId and raw JSON blocks; readability gap | Medium |
| Audit | Reports | `/Audit/Reports` | `ReportsModel` | Audit.View | static routes | Full page | Full page | OK | Low |
| Audit | Export | `/Audit/Export` | `ExportModel` | Audit.Export | `ExportAsync` | Full page workflow | Full page workflow | no confirm/busy/notification; export is correctly permissioned | Medium |

Framework/public shell pages scanned: `_ViewImports`, `Index`, `HostDashboard`, `TenantDashboard`, `PrivacyPolicy`, `CookiePolicy`. Only notable finding is `/Account/Login` hardcoded in `Index.cshtml`, not an ERP module route.

## Raw GUID Exposure Matrix

| Area | Field/Finding | Current Source | Classification | Required Direction |
|---|---|---|---|---|
| Inventory Receipt | `Input.WarehouseId` visible text input | `Pages/Inventory/Receipt.cshtml` | Existing app service usage | Use `IWarehouseAppService.GetListAsync` dropdown showing `Code - Name`; filter Active |
| Inventory Receipt | `Input.Lines[0].StockItemId` visible text input | `Receipt.cshtml` | Existing app service usage | Use `IStockItemAppService.GetListAsync` dropdown showing `CodeSnapshot - NameSnapshot`; filter Active, Component, inventory-enabled |
| Inventory Receipt | `Input.IdempotencyKey` visible input | `Receipt.cshtml` | UI-only fix | Render hidden input only |
| Inventory Issue | `Input.WarehouseId` visible text input | `Issue.cshtml` | Existing app service usage | Warehouse selector |
| Inventory Issue | `Input.Lines[0].StockItemId` visible text input | `Issue.cshtml` | Existing app service usage | StockItem selector |
| Inventory Issue | `Input.IdempotencyKey` visible input | `Issue.cshtml` | UI-only fix | Hidden input |
| Inventory Adjustment | `WarehouseId`, `StockItemId`, `IdempotencyKey` visible | `Adjustment.cshtml` | Existing app service usage + UI-only | Selectors and hidden idempotency |
| Inventory Balances | `WarehouseId`, `StockItemId` displayed | `Balances.cshtml` | Existing service usage or selector contract | Join display from existing warehouse/stock services; future read model may be cleaner |
| Inventory Lots | `StockItemId` displayed | `Lots.cshtml` | Existing service usage or selector contract | Display stock item code/name |
| Inventory Ledger | `WarehouseId` displayed; DTO also has `IdempotencyKey` but not visible | `Ledger.cshtml` | Existing service usage or selector contract | Display warehouse code/name; avoid exposing idempotency |
| BOM Index | `ProductId` visible input | `Bom/Index.cshtml` | Existing app service usage | Product selector using `IProductAppService.GetListAsync` |
| BOM Product | `ProductId` displayed | `Bom/Product.cshtml` | Existing app service usage | Display product code/name |
| BOM Create/Edit | `ComponentId` visible inputs | `Bom/Create.cshtml`, `Bom/Edit.cshtml` | Existing app service usage | Component selector rows using `IComponentAppService.GetListAsync` |
| BOM Details | `ProductId`, `ComponentId` displayed | `Bom/Details.cshtml` | Existing service usage or approved query contract | Display product/component code/name; DTO lacks names |
| Sales Create | `CatalogItemId`, `BomVersionId` visible inputs | `Sales/Create.cshtml` | Requires approved selector contract for rich selector | Defer until Inventory/BOM selectors stabilize |
| Sales Edit | `CatalogItemId`, `BomVersionId`; line item fallback uses ID | `Sales/Edit.cshtml` | Requires approved selector contract / UI-only fallback display | Defer; do not calculate business data |
| Sales CustomerHistory | `CustomerId` visible input; item `CatalogItemId` displayed | `Sales/CustomerHistory.cshtml` | Existing service usage for customer selector; selector contract may be needed for item display | Defer |
| Pricing routes | `componentId`, `productId` route parameters | Pricing History/Create pages | Acceptable route/internal value | Add display context, not a blocker |
| Catalog image URLs | `@product.Id`/`@component.Id` in thumbnail API URL | Catalog list/edit pages | Acceptable binary API URL | Keep as URL; do not render Base64 |
| Customer/CustomerGroups | `data-id` attributes and route IDs | Index/modal pages | Acceptable hidden/route value | Not visible to normal operator |

## Selector Readiness Matrix

| Selector Need | Existing Data Source | Display Fields Available | Classification | Notes |
|---|---|---|---|---|
| Warehouse selector | `IWarehouseAppService.GetListAsync` | `Code`, `Name`, `Status` | Existing app service usage | Can filter Active in PageModel without backend changes |
| StockItem selector | `IStockItemAppService.GetListAsync` | `CodeSnapshot`, `NameSnapshot`, `ItemType`, `IsInventoryEnabled`, `Status` | Existing app service usage | Can filter Active Component inventory-enabled for phase 1 |
| Inventory query display labels | Warehouse and StockItem services plus query DTO IDs | Labels available through separate services | Existing service usage, possible future query contract | UI join may be acceptable for small data; richer read DTO preferred later |
| BOM product selector | `IProductAppService.GetListAsync` | `Code`, `Name`, `Status` | Existing app service usage | Filter Active for create/open selector |
| BOM component selector | `IComponentAppService.GetListAsync` | `Code`, `Name`, `Unit`, `Status` | Existing app service usage | Filter Active for BOM editor |
| BOM details item display | BOM DTO has IDs only; Catalog services provide labels separately | Available through separate list services | Existing service usage or selector/query contract | Avoid per-row service calls |
| Customer selector | `ICustomerAppService.GetListAsync` | `Code`, `Name`, `Status`, group fields | Existing app service usage | Sales Create already uses this |
| CustomerGroup selector | `ICustomerGroupAppService.GetListAsync` | `Code`, `Name`, `Status` | Existing app service usage | Customer pages already use this |
| Sales item selector | Catalog Product/Component services plus BOM services | Partial | Requires approved selector/query contract for rich UX | Needs Product/Component/BOM coordination; defer |

## Inventory Blocker Findings

1. `/Inventory/Receipt` blocks operator UAT. It asks operators to type `WarehouseId`, `StockItemId`, and `IdempotencyKey`. This violates the raw GUID and technical field rules.
2. `/Inventory/Issue` has the same raw `WarehouseId`, `StockItemId`, and `IdempotencyKey` exposure.
3. `/Inventory/Adjustment` exposes top-level `WarehouseId`, `StockItemId`, and `IdempotencyKey`. It also has conditional fields for increase/decrease but no UI guidance.
4. Posting pages have no confirmation, busy state, or success/failure notification.
5. Validation failure currently has no selector option reload because no selectors exist yet.
6. `/Inventory/Balances`, `/Inventory/Lots`, and `/Inventory/Ledger` display raw IDs instead of warehouse/stock item labels.
7. Existing selector data likely exists through `IWarehouseAppService` and `IStockItemAppService`; no backend contract is immediately required for posting-page selectors.

## BOM Blocker Findings

1. `/Bom` requires raw `ProductId`.
2. `/Bom/Product/{productId}` displays raw product ID.
3. `/Bom/Create/{productId}` and `/Bom/Edit/{id}` require raw `ComponentId` entry.
4. `/Bom/Details/{id}` displays raw `ProductId` and raw `ComponentId`.
5. BOM Create/Edit have inline scripts for dynamic rows. This violates new JS rules.
6. Product/component selectors can likely use existing Catalog list services, but BOM DTOs do not include product/component display names. Details/history display can be fixed by UI-level lookup for small data or by an approved query contract later.

## Customer Reference Pattern Verification

Current implementation aligns well with the new docs:

- Index pages register lowercase `scripts` sections with `<abp-script>`.
- `Customers/Index.js` and `CustomerGroups/Index.js` use `abp.ModalManager`.
- Create/Edit/Details modal routes exist while full-page fallback routes remain.
- Status actions use normal forms and confirmation via `abp.message.confirm`.
- Success notifications use `abp.notify.success`.
- Busy state uses `abp.ui.setBusy`.
- Permission checks are present in PageModels and UI flags.
- Tests cover route rendering, localized labels, authorization policies, script registration, modal hooks, and status hooks.

Remaining risk: current Web tests do not execute browser JavaScript. Manual UAT or approved browser automation is still needed to confirm modal behavior, validation-in-modal behavior, and URL preservation.

## Catalog Findings

1. Component list now avoids the misleading inactive-row `Ngừng sử dụng` action and shows disabled `Kích hoạt` with explanatory text because Component activation is not available.
2. `GAP-001 Catalog Component Reactivation Missing` remains a backend/business gap, not a UI fix.
3. Product list still shows Deactivate for every editable product row; Product reactivation availability needs source verification.
4. Product/Component list thumbnails use API URLs and lazy loading; no Base64 is embedded in list HTML.
5. Product/Component Create/Edit use raw `<script src="/Pages/Catalog/catalog-image-preview.js">`; must move to lowercase `scripts` + `<abp-script>`.
6. Product/Component image remove actions lack confirmation/busy/notification.

## Pricing Findings

1. Pricing Index displays Product/Component code and name, not raw IDs.
2. History links are guarded by `Pricing.History`.
3. History and Create routes use product/component GUID route parameters, which are acceptable as route/internal values.
4. History/Create pages could improve display context by showing item code/name.
5. Create version pages are modal candidates, but not urgent compared with Inventory/BOM raw ID blockers.
6. No update/delete price version UI was found.

## Sales Findings

Sales was audited only for raw IDs and permission-sensitive cost/profit fields, per instruction.

1. Sales Details and History guard profit fields with `Sales.ViewProfit`.
2. Cost fields are not visibly displayed in the audited Razor pages; application DTOs already null cost/profit based on authorization.
3. Sales Create has Customer/Warehouse selectors, but item lines still expose raw `CatalogItemId` and `BomVersionId`.
4. Sales Edit displays line fallback as raw `CatalogItemId` and exposes raw line IDs in hidden values; visible line item display should prefer snapshots.
5. Sales CustomerHistory exposes visible `CustomerId` input and displays `CatalogItemId`.
6. Sales confirmation/cancel/remove actions lack confirmation/busy/notification.
7. Full Sales UX should remain deferred until Inventory and BOM selectors are usable.

## Audit Findings

1. Audit Index correctly restricts Export link with `Audit.Export`.
2. Audit Export PageModel is protected by `Audit.Export`.
3. Audit Detail displays `EntityId` and raw JSON blocks; readability should be improved without changing audit payloads.
4. Audit Index displays raw severity enum value rather than localized severity text.
5. Export has no confirmation, busy state, or post-download notification.
6. No mutation UI for audit records was found.

## Script/ABP Pattern Findings

| Finding | Files | Classification |
|---|---|---|
| Inline BOM editor script | `Pages/Bom/Create.cshtml`, `Pages/Bom/Edit.cshtml` | UI-only JS refactor, after selector cleanup |
| Raw Catalog image script registration | Catalog Product/Component Create/Edit | UI-only script registration fix |
| ABP ModalManager use only in Customer/CustomerGroups | Customer/CustomerGroups JS | Expected current reference scope |
| No confirmation/busy on Inventory posting | Inventory Receipt/Issue/Adjustment | UI-only once selectors are added |
| No confirmation/busy on BOM publish/archive | BOM Product page | UI-only |
| No confirmation/busy on Audit export | Audit Export page | UI-only |

## Permission Visibility Findings

| Area | Current Status | Required Fix |
|---|---|---|
| Menus | RequirePermissions present for module roots | No urgent fix |
| Pricing history | Link guarded with `Pricing.History`; destination page has `Pricing.History` | OK |
| Customer/CustomerGroups actions | UI flags match create/edit/manage-status permissions | OK; browser UAT needed |
| Inventory hub links | Action links guarded by receive/issue/adjust/manage/view-ledger flags | OK |
| Inventory inquiry pages | PageModel authorization not clearly attributed in compact source for Balances/Lots/Ledger; service enforces view/view-ledger | Verify page attributes before implementation |
| Sales profit | Guarded by `Sales.ViewProfit` where displayed | OK |
| Sales cost | Not visibly displayed in audited Razor pages | No immediate UI leak found |
| Audit export | Link and page protected by `Audit.Export` | OK |

## Localization/Terminology Findings

Vietnamese module/action/status keys exist for Catalog, BOM, Customer, Pricing, Inventory, Sales, and Audit. Important terms match new decisions:

- `Stock Item` = `Mặt hàng tồn kho`
- `Business Audit` = `Nhật ký nghiệp vụ`
- `BOM` menu/title = `Định mức linh kiện (BOM)`

Remaining terminology issues:

- `Bom:ProductId` and `Bom:ComponentId` are translated as `Mã sản phẩm` and `Mã linh kiện`, but the UI currently binds raw GUIDs. The label becomes misleading because operators expect a business code, not a GUID.
- `Sales:CustomerId` is translated as `Mã khách hàng`, but `Sales/CustomerHistory` currently binds a raw GUID.
- Audit severity uses enum rendering rather than localized severity labels.
- Catalog image tests still expect English `No image` in one test; this is a test/localization consistency concern, not a production blocker.

## Backend Gap vs UI-Only Gap

| Gap | Type | Notes |
|---|---|---|
| Catalog Component reactivation | Backend/business gap | No existing `ActivateAsync` or PageModel handler found; do not implement in UI refactor |
| Catalog Product reactivation | Needs source verification | Product list still deactivate-only; inspect before any UI change |
| Inventory posting selectors | Existing app service usage | Warehouse and StockItem list services exist with enough DTO fields for basic selectors |
| Inventory inquiry label enrichment | Existing service usage or query contract | Can join labels in PageModel for small data; richer read model may be approved later |
| BOM selectors | Existing app service usage | Product/Component list services exist |
| BOM detail names | Existing service usage or query contract | BOM DTOs lack display names |
| Sales item selector | Requires approved selector/query contract | Defer; needs Catalog/BOM/Pricing/Inventory context |
| Browser modal test coverage | Test gap | Existing rendered tests are not browser-executed |

## Recommended Immediate Next Step

Implement the next UI refactor batch as an explicit implementation task:

1. Inventory Receipt selector/technical-field cleanup only.
2. Use existing `IWarehouseAppService` and `IStockItemAppService`.
3. Hide `Input.IdempotencyKey`.
4. Preserve `PostReceiptAsync` behavior and route.
5. Add confirmation/busy/notification only if it can be done without changing backend behavior.

Do not start Sales, visual polish, or modal expansion until Inventory Receipt, Issue, Adjustment, and BOM selectors are usable.
