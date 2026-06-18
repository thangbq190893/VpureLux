# VPureLux ERP Module Specifications V2

## 1. Catalog Module V2

### Purpose

Catalog owns current sellable Product/SKU master data and Component master data.

### Product

Product means sellable SKU.

Fields should include:

- Id.
- Code.
- Name.
- Unit if required.
- Status.
- Optional image metadata/image content through approved Catalog image extension.
- Description/notes if needed.

Business rules:

- Code is required, normalized, unique among non-deleted Products, and immutable after creation.
- Name is required.
- Product can be activated/deactivated.
- Inactive Product cannot be used for new BOM publication or Sales.
- Product must have a published BOM before it can be sold.

UI terminology:

- `Product.Code` = `Mã sản phẩm`.
- `Product.Name` = `Tên sản phẩm`.

### Component

Component means inventory-managed physical part.

Fields should include:

- Id.
- Code.
- Name.
- Unit.
- Status.
- Optional image metadata/image content.

Business rules:

- Code is required, normalized, unique among non-deleted Components, and immutable after creation.
- Name is required.
- Component can be activated/deactivated.
- Inactive Component cannot be selected for new BOM lines or new inventory postings.
- Component creation should create/synchronize an inventory StockItem if Inventory module is active.
- Component deactivation should deactivate the corresponding StockItem without deleting historical inventory.

UI terminology:

- `Component.Code` = `Mã linh kiện`.
- `Component.Name` = `Tên linh kiện`.

### Catalog UI V2

- Product and Component list pages should use action menu pattern.
- Product/Component Create/Edit/Details should use ABP modals with full-page fallback.
- Image upload/replace/remove may remain full page until image UX is stable, but script registration must be ABP-compliant.
- Activate/Deactivate actions must be permission-aware and confirmed.

## 2. BOM Module V2

### Purpose

BOM owns the component composition of a Product/SKU.

### Core Rule

Every Product/SKU that can be sold must have a published BOM version.

A Product can have a BOM with one or many Component lines.

Examples:

- Product `RO-01`: BOM has cores, tube, case, label, box.
- Product `LOI-PP-01-BAN`: BOM has one component `LOI-PP-01`, quantity 1.

### BOM Version

Fields:

- Id.
- ProductId.
- VersionNo.
- Status: Draft, Published, Archived.
- Lines.
- Creation metadata.

Rules:

- Draft BOM can be edited.
- Published BOM is immutable.
- Archived BOM remains historical.
- Only published BOM can be used for Sales.
- A Product should have at most one current published BOM unless versioning rules allow effective periods.

### BOM Line

Fields:

- ComponentId.
- Quantity.
- Optional usage label or note if approved, e.g. `Lõi 1`, `Tem`, `Vỏ hộp`.

Rules:

- Component must exist and be active when line is added to draft.
- Quantity must be greater than zero.
- ComponentId must be selected by business label, not typed as GUID.

### BOM UI V2

BOM index:

- Must not expose ProductId textbox.
- Must use Product selector showing `Mã sản phẩm - Tên sản phẩm`.
- Product list should have an action `Định mức linh kiện` linking to its BOM.

BOM product page:

- Show selected Product code/name/image if available.
- Show BOM versions and current status.
- Show price context if available:
  - Current Product Suggested Selling Price.
  - Giá cấu thành linh kiện.
  - Missing component price warnings.

BOM Create/Edit:

- Full-page editor.
- Component rows use Component selector showing `Mã linh kiện - Tên linh kiện`.
- Add/remove line JavaScript must be external and registered via `<abp-script>`.
- No inline script.
- No raw GUIDs visible.

## 3. Pricing Module V2

### Purpose

Pricing owns suggested selling prices used as sales defaults and quote references.

Pricing V2 does not own actual input cost or actual sales price.

### Price Types

#### Component Suggested Selling Price

Technical recommended aggregate name:

`ComponentSuggestedSellingPriceVersion`

UI name:

`Giá bán đề xuất linh kiện`

Fields:

- Id.
- ComponentId.
- PriceVersionNo.
- Money amount in VND.
- EffectiveFrom inclusive.
- EffectiveTo exclusive.
- Reason.
- Status.

Rules:

- Component must exist and be active.
- Price must be greater than zero.
- Backdated version creation is rejected unless explicitly approved.
- Only one current active version per Component.
- Creating a successor closes the current active version at successor EffectiveFrom.
- Existing versions are immutable except closing current active version.

#### Product Suggested Selling Price

Technical recommended aggregate name:

`ProductSuggestedSellingPriceVersion`

UI name:

`Giá bán đề xuất sản phẩm`

Fields:

- Id.
- ProductId.
- PriceVersionNo.
- Money amount in VND.
- EffectiveFrom inclusive.
- EffectiveTo exclusive.
- Reason.
- Status.

Rules:

- Product must exist and be active.
- Price must be greater than zero.
- Backdated version creation is rejected unless explicitly approved.
- Only one current active version per Product.
- Creating successor closes current active version.
- Existing versions are immutable except closing current active version.

### Component Build Price

UI name:

`Giá cấu thành linh kiện`

Formula:

`SUM(BOMLine.Quantity * CurrentComponentSuggestedSellingPrice)`

Scope:

- Read-side/reference only.
- Computed for Product's current published BOM.
- If no published BOM: show `Chưa có định mức`.
- If any component is missing current price: show `Thiếu giá linh kiện`.
- Do not persist as a business source of truth unless a separate reporting/cache design is approved.

### Pricing UI V2

Main Pricing page should focus on selling prices:

Tabs:

1. `Giá bán đề xuất linh kiện`.
2. `Giá bán đề xuất sản phẩm`.

Do not show old `Giá mua linh kiện`.

Component price tab:

- Component code.
- Component name.
- Current Component Suggested Selling Price.
- EffectiveFrom.
- Actions: history modal, create new price version modal.

Product price tab:

- Product code.
- Product name.
- BOM status.
- Giá cấu thành linh kiện.
- Current Product Suggested Selling Price.
- Difference = Product Suggested Price - Giá cấu thành linh kiện if both available.
- Actions: history modal, create new price version modal.

Modal behavior:

- History should open in modal or drawer for quick lookup.
- Create new price version should be modal with full-page fallback if route exists.

## 4. Inventory Module V2

### Purpose

Inventory owns physical Component stock by warehouse, lot, transaction, FIFO allocation, and balances.

### StockItem

In V2 phase 1, StockItem is created for Components.

Product StockItems are not inventory-enabled.

StockItem fields should include:

- Id.
- ItemType.
- CatalogItemId.
- CodeSnapshot.
- NameSnapshot.
- IsInventoryEnabled.
- Status.

Rules:

- Only active, component-type, inventory-enabled StockItems can be selected in receipt/issue/adjustment.
- Product inventory remains disabled until separately approved.

### Warehouse

Fields:

- Id.
- Code.
- Name.
- Status.

Rules:

- Code is immutable.
- Inactive warehouse cannot be used in new postings.

### Receipt

Receipt creates inventory lots and positive ledger facts.

Input model should support:

- WarehouseId at header level.
- ReceivedAt at header level.
- IdempotencyKey hidden.
- Lines:
  - StockItemId.
  - LotNo.
  - Quantity.
  - UnitCost.

Rules:

- UnitCost is actual input cost of the lot.
- UnitCost is not a suggested price.
- Receipt should support multiple lines if backend contract already has Lines collection.
- LotNo may be manually entered in phase 1.
- Official automatic LotNo generation requires backend atomic numbering and is a separate approved enhancement.

UI:

- Full-page operational workflow.
- Warehouse selector at header.
- Lines table with add/remove row.
- StockItem selector per row.
- No raw GUID visible.
- No IdempotencyKey visible.
- Confirmation/busy/notification after selectors are stable.

### Issue

Issue consumes Component lots by FIFO.

Input should support:

- WarehouseId at header.
- IdempotencyKey hidden.
- Lines:
  - StockItemId.
  - Quantity.

Rules:

- Cannot issue more than available.
- FIFO order: ReceivedAt ASC, CreationTime ASC, Id ASC.
- UI must not calculate FIFO; backend does.

### Adjustment

Adjustment can increase or decrease inventory.

Rules:

- Reason is required.
- Increase creates a new lot.
- Decrease consumes FIFO lots.
- UI must not calculate stock/cost.

## 5. Sales Module V2

### Purpose

Sales owns customer orders, actual selling price, confirmation snapshots, revenue, cost, profit, and customer purchase history.

### Core Rule

Sales order lines always sell Product/SKU.

No direct Component sales lines in V2.

### Sales Order Line

Fields should include:

- ProductId.
- PublishedBomVersionId or resolved BOM version snapshot.
- Quantity.
- SuggestedSellingPriceSnapshot.
- ActualSellingPrice.
- OverrideReason when applicable.
- Component snapshot details after confirmation.
- FIFO cost snapshot after confirmation.

Rules:

- Product must be active.
- Product must have a published BOM.
- ActualSellingPrice defaults from current Product Suggested Selling Price when available.
- User may override ActualSellingPrice subject to approved permission/reason rule.
- Confirmation validates stock and allocates FIFO.
- Confirmed/cancelled orders are immutable.

### Sale of loose components

To sell a loose component:

1. Create Product/SKU for that sale item.
2. Create BOM with one Component line.
3. Publish BOM.
4. Create Product Suggested Selling Price.
5. Sell as Product.

### Sales UI V2

Sales Create/Edit:

- Product selector showing code/name/image/status.
- No direct Component selector.
- Show selected Product's published BOM.
- Show component availability per required component.
- Show suggested product price.
- Show actual selling price input.
- Show cost/profit only when permissions allow.
- Do not calculate FIFO in JS; call backend preview service only if approved.

Sales Details:

- Show Product snapshot.
- Show BOM/component snapshot.
- Show FIFO allocations/cost only with `Sales.ViewCost`.
- Show profit only with `Sales.ViewProfit`.

## 6. Audit Module V2

Audit remains append-only business audit.

Audit must not store:

- Image Base64.
- Secrets.
- Full large payloads.
- Technical request/response bodies.

Audit UI should improve readability but must not change payload semantics.
