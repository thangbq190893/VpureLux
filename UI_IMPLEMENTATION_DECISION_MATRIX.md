# VPureLux UI Implementation Decision Matrix

## 1. Classification Legend

| Classification | Meaning | Codex action |
|---|---|---|
| UI-only | Can be fixed in Razor/PageModel/JS using existing services and bindings | Implement when requested |
| Existing service usage | Can use an existing app service/list method from Web layer | Implement after inspecting service and DTO |
| Requires selector contract | Needs a new query/API/AppService method | Do not implement without approval |
| Backend/business gap | Missing business capability or app service behavior | Do not implement in UI refactor |
| Keep full page | Operational workflow; do not modalize | Improve selectors/feedback only |
| Modal candidate | Small CRUD/read-only page suitable for ABP ModalManager | Modal may be implemented with fallback route |

## 2. Global Field Decisions

| Field pattern | Target UI | Notes |
|---|---|---|
| `Id` | hidden/route value only | Do not display raw GUID when code/name is available |
| `*Id` pointing to business entity | dropdown/search selector showing `Code - Name` | option value can remain GUID |
| `IdempotencyKey` | hidden field or server-managed | Never operator-editable |
| `ConcurrencyStamp` | hidden | Technical field |
| `Code` | visible business field | Required/immutable where certified |
| `Status` | localized badge/text | No raw enum names |
| `Quantity` | numeric input/display | `Số lượng`, DECIMAL(18,4) where applicable |
| `UnitCost` | numeric/currency input | `Đơn giá nhập`; do not replace actual lot cost with Pricing |
| `ReceivedAt` | operator-friendly date/time input | `Ngày nhập`, no milliseconds in visible UI |
| `Reason` | textarea/input | Required for adjustment and price version rules where certified |

## 3. Page-Level Decisions

### Customer

| Page | Current/Target mode | Required behavior | Classification |
|---|---|---|---|
| `/Customers` | List full page | Action menu, status confirm, create/edit/details modal launch, fallback links preserved | UI-only / modal candidate |
| `/Customers/Create` | Full-page fallback | Keep route; use existing `CreateCustomerDto`; CustomerGroup selector; validation reloads dropdown | Preserve |
| `/Customers/CreateModal` | Modal | Existing create service only; validation stays in modal | Modal candidate |
| `/Customers/Edit/{id}` | Full-page fallback | Code readonly/disabled; CustomerGroup selector reload on validation | Preserve |
| `/Customers/EditModal` | Modal | Existing update service only; code immutable | Modal candidate |
| `/Customers/Details/{id}` | Full-page fallback | Read-only code/name/group/status/details | Preserve |
| `/Customers/DetailsModal` | Modal | Read-only, no extra sensitive fields | Modal candidate |

### CustomerGroups

| Page | Current/Target mode | Required behavior | Classification |
|---|---|---|---|
| `/CustomerGroups` | List full page | Action menu, status confirm, create/edit/details modal launch, fallback links preserved | UI-only / modal candidate |
| `/CustomerGroups/Create` | Full-page fallback | Existing `CreateCustomerGroupDto`; validation remains backend/app-service source | Preserve |
| `/CustomerGroups/CreateModal` | Modal | Existing create service only | Modal candidate |
| `/CustomerGroups/Edit/{id}` | Full-page fallback | Code readonly/disabled | Preserve |
| `/CustomerGroups/EditModal` | Modal | Existing update service only; code immutable | Modal candidate |
| `/CustomerGroups/Details/{id}` | Full-page fallback | Read-only | Preserve |
| `/CustomerGroups/DetailsModal` | Modal | Read-only | Modal candidate |

### Catalog Components

| Page/Action | Target decision | Notes | Classification |
|---|---|---|---|
| `/Catalog/Components` | List full page | Thumbnail, code/name/unit/status, action menu | UI-only |
| Create Component | Full-page first; modal only after image upload is verified safe | Image upload/preview adds risk | Cautious modal candidate |
| Edit Component | Full-page first; modal only after image upload is verified safe | Code immutable if certified by source | Cautious modal candidate |
| Deactivate Component | Confirm + notification | Use existing handler only | UI-only |
| Activate Component | Do not implement if no existing backend method | If missing, show no active action or disabled explanatory action | Backend/business gap |
| Image upload/replace/remove | Keep backend unchanged; add client feedback/confirmation only | No Base64 in HTML/logs/events/general DTOs | UI-only if endpoints exist |
| Image preview JS | Register with `<abp-script>` | No raw `<script src>` | UI-only |

### Catalog Products

| Page/Action | Target decision | Notes | Classification |
|---|---|---|---|
| `/Catalog/Products` | List full page | Thumbnail, code/name/unit/status, action menu | UI-only |
| Create Product | Full-page first; modal only after image upload is verified safe | Product inventory disabled in phase 1 | Cautious modal candidate |
| Edit Product | Full-page first; modal only after image upload is verified safe | Image replace/remove feedback | Cautious modal candidate |
| Deactivate Product | Confirm + notification | Use existing handler only | UI-only |
| Activate Product | Implement only if existing backend method exists | Otherwise backend/business gap | Inspect first |
| Image upload/replace/remove | Keep certified image behavior unchanged | Supported formats JPEG/PNG/WEBP, decoded max 2MB | UI-only if endpoints exist |

### Inventory

| Page | Target decision | Field decisions | Classification |
|---|---|---|---|
| `/Inventory` | Hub/list full page | Links use tag helpers/menu, permission-aware | UI-only |
| `/Inventory/Warehouses` | List full page; simple create may be modal | Code/name/status, no raw GUID, status actions if supported | UI-only / modal candidate |
| `/Inventory/Receipt` | Full-page operational workflow | WarehouseId selector; StockItemId selector; IdempotencyKey hidden; ReceivedAt operator-friendly; confirm/busy/notify | Highest-priority blocker |
| `/Inventory/Issue` | Full-page operational workflow | WarehouseId selector; StockItemId selector; Lot selector if applicable; IdempotencyKey hidden; confirm/busy/notify | High-priority blocker |
| `/Inventory/Adjustment` | Full-page operational workflow | WarehouseId selector; StockItemId selector; Reason required; IdempotencyKey hidden; confirm/busy/notify | High-priority blocker |
| `/Inventory/Balances` | Inquiry full page | Display warehouse/stock item code/name, not raw IDs | UI-only if DTO has display fields; otherwise selector contract |
| `/Inventory/Lots` | Inquiry full page | Display warehouse/stock item code/name, lot no, qty, unit cost | UI-only if DTO has display fields; otherwise selector contract |
| `/Inventory/Ledger` | Inquiry full page | Display references readably; no raw IDs unless no DTO support | UI-only or selector contract |

Inventory selector constraints:

- Receipt/Issue/Adjustment must only allow active Warehouses.
- Receipt/Issue/Adjustment must only allow active, inventory-enabled Component StockItems in phase 1.
- Do not enable Product inventory operations.
- Do not calculate FIFO in UI.

### BOM

| Page | Target decision | Notes | Classification |
|---|---|---|---|
| `/Bom` | Full-page lookup | Product selector instead of raw ProductId | High-priority blocker |
| `/Bom/Product/{productId}` | Product BOM history full page | Display product code/name; no raw product GUID in primary UI | UI-only if DTO/source supports display |
| `/Bom/Create/{productId}` | Full-page editor | Component selector rows; no raw ComponentId; external JS | Keep full page |
| `/Bom/Edit/{id}` | Full-page editor | Component selector rows; no inline JS; draft-only edit | Keep full page |
| `/Bom/Details/{id}` | Full page or read-only modal later | Display product/component names | Read-only modal candidate later |
| Publish/Archive | Confirmation + notification | Use existing handlers/services only | UI-only |
| Clone | Full page or modal later | Do not change versioning rules | Cautious modal candidate |

### Pricing

| Page | Target decision | Notes | Classification |
|---|---|---|---|
| `/Pricing` | Inquiry/list full page | Show Product/Component code/name; history links require `Pricing.History` | UI-only |
| Component price history | Full page | Create version action can be modal | Modal candidate |
| Product price history | Full page | Create version action can be modal | Modal candidate |
| Create price version | Modal candidate | Must preserve Reason, VND, no backdated rule | Modal candidate using existing app service only |

Do not add Customer-specific pricing or CustomerGroup pricing.

### Sales

| Page | Target decision | Notes | Classification |
|---|---|---|---|
| `/Sales` | Full-page list | Action menu, details links, permission fields | UI-only |
| `/Sales/Create` | Full-page operational workflow | Customer/Warehouse/item/BOM selectors; no raw IDs | High-risk; defer until Inventory/BOM selectors |
| `/Sales/Edit/{id}` | Full-page operational workflow | Draft-only; line selectors; remove-line confirm | High-risk |
| `/Sales/Details/{id}` | Full-page operational detail | Profit only with `Sales.ViewProfit`; cost with `Sales.ViewCost` if shown | Permission fix required |
| `/Sales/History` | Inquiry | Profit hidden unless `Sales.ViewProfit` | Permission fix required |
| `/Sales/CustomerHistory` | Inquiry | Respect `Sales.ViewCustomerHistory` and profit permission | Existing behavior must be preserved |

Sales UI must not calculate profit or FIFO cost in JavaScript.

### Audit

| Page | Target decision | Notes | Classification |
|---|---|---|---|
| `/Audit` | Search/list full page | Filters readable; detail can open read-only modal | UI-only |
| `/Audit/Details/{id}` | Full-page fallback | JSON must be readable and safe | Preserve |
| `/Audit/DetailsModal` | Read-only modal candidate | No extra sensitive fields | Modal candidate |
| `/Audit/Reports` | Full page | Large filters remain full page | Keep full page |
| `/Audit/Export` | Full page | `Audit.Export`, busy state, notification | UI-only |

## 4. Immediate Priority Matrix

| Priority | Area | Reason |
|---:|---|---|
| 1 | Inventory Receipt raw GUIDs and IdempotencyKey | Blocks nhập kho UAT |
| 2 | Inventory Issue/Adjustment raw GUIDs | Blocks inventory operation UAT |
| 3 | BOM Product/Component selectors | Blocks BOM UAT |
| 4 | Catalog image script registration and status UI | Visible UX/script compliance issue |
| 5 | Pricing history/create version UI | Lower risk, after selectors |
| 6 | BOM inline JavaScript externalization | Important but after selector blockers |
| 7 | Sales permission/cost/profit UI | High risk, after Inventory/BOM usable |
| 8 | Audit detail/export UX | Lower risk, after operational workflows |

## 5. Field Label Map

| Technical name | Vietnamese label | UI behavior |
|---|---|---|
| `WarehouseId` | Kho | selector/dropdown |
| `StockItemId` | Mặt hàng tồn kho | selector/dropdown; phase 1 Component stock items only |
| `ProductId` | Sản phẩm | selector/dropdown/search |
| `ComponentId` | Linh kiện | selector/dropdown/search |
| `CustomerId` | Khách hàng | selector/dropdown/search |
| `CustomerGroupId` | Nhóm khách hàng | selector/dropdown/search |
| `BomId` / `BomVersionId` | Phiên bản định mức | selector/display |
| `PricingVersionId` | Phiên bản giá | display/route only |
| `IdempotencyKey` | n/a | hidden |
| `LotNo` | Số lô | visible business field |
| `ReceivedAt` | Ngày nhập | date/time input |
| `UnitCost` | Đơn giá nhập | numeric/currency input |
| `Quantity` | Số lượng | numeric input |
| `Reason` | Lý do | visible when business rule requires |
