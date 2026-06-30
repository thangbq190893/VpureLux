# VPureLux ERP Business Architecture Decisions V2

## Status

Approved business direction for the current development phase.

The project may be refactored or rebuilt to align with this V2 architecture.

## Vocabulary

| Term | Meaning | Vietnamese UI Term |
|---|---|---|
| Product / SKU | Anything that can be sold to a customer | Sản phẩm bán / Sản phẩm |
| Component | Physical part/material kept in inventory and used by BOM | Vật tư |
| BOM | Component composition of a Product/SKU | Định mức vật tư |
| Warehouse | Physical storage location | Kho |
| Inventory Lot | Received stock lot with actual unit cost | Lô hàng |
| Component Suggested Selling Price | Suggested customer selling price for a component | Giá bán đề xuất vật tư |
| Product Suggested Selling Price | Suggested customer selling price for a Product/SKU | Giá bán đề xuất sản phẩm |
| Component Build Price | Sum of component suggested prices in the BOM | Giá cấu thành vật tư |
| Actual Receipt Unit Cost | Actual input cost of a receipt lot | Đơn giá nhập thực tế |
| FIFO Cost | Actual cost consumed from inventory lots | Giá vốn FIFO |
| Actual Selling Price | Final selling price entered on a sales order | Giá bán thực tế |

## ADR-001: Everything sold is Product/SKU

All items sold to customers must be represented as `Product`.

A `Product` is a sellable SKU. It can be:

- A complete water purifier.
- A replacement filter.
- A loose component sold separately.
- A combo/kit.
- Any packaged item the business sells.

`Component` is not sold directly in Sales V2.

If a component is sold to customers, create a Product/SKU with a BOM containing exactly one Component line.

## ADR-002: Product must have published BOM before sale

A Product can be sold only when it has a published BOM version.

Even a Product representing a single loose component must have a BOM:

- Product: `LOI-PP-01-BAN` - Lõi PP bán rời.
- BOM:
  - Component: `LOI-PP-01`
  - Quantity: `1`

This provides one consistent Sales flow:

Product -> Published BOM -> Component requirement -> Inventory FIFO -> Sales snapshot.

## ADR-003: Product is master data plus image, not inventory stock

Product owns:

- Code.
- Name.
- Unit if applicable.
- Image.
- Status.
- Basic descriptive data.

Product does not own stock quantity or lot cost in phase 1.

Product stock items are not enabled in phase 1. Inventory is based on Component StockItems only.

## ADR-004: Component is the physical inventory item

Component owns:

- Component code.
- Component name.
- Unit.
- Status.
- Optional image.

Component is the object received into inventory, issued from inventory, and costed by FIFO.

## ADR-005: BOM is the product composition

BOM defines how many Components are needed to sell one Product/SKU.

BOM line should support at minimum:

- ComponentId.
- Quantity.
- Unit display.
- Optional usage label or note, such as `Lõi 1`, `Lõi 2`, `Tem`, `Vỏ hộp`, if the source model supports or is approved to extend.

Published BOMs are immutable.

Draft BOMs can be edited.

Sales uses only published BOM versions.

## ADR-006: Pricing owns suggested customer selling prices only

Pricing V2 owns these price version types:

1. Component Suggested Selling Price.
2. Product Suggested Selling Price.

Pricing V2 does not own:

- Actual purchase cost.
- Standard purchase price.
- FIFO cost.
- Profit.
- Customer-specific pricing.
- Customer group pricing.
- Actual selling price on orders.

## ADR-007: Remove/replace Component Purchase Price concept

The old `ComponentPurchasePriceVersion` concept does not match the clarified business need.

V2 should replace it with:

`ComponentSuggestedSellingPriceVersion`

Vietnamese UI: `Giá bán đề xuất vật tư`.

If old source code currently has `ComponentPurchasePriceVersion`, it must be treated as a model conflict and refactored in an approved backend migration step.

Do not merely relabel `ComponentPurchasePriceVersion` in UI while keeping incorrect backend semantics.

## ADR-008: Product suggested price is manually entered

The Product Suggested Selling Price is entered manually by the user.

The system may show `Giá cấu thành vật tư` for reference, but it must not automatically calculate or overwrite the Product Suggested Selling Price unless a future approved pricing formula exists.

## ADR-009: Component Build Price

`Giá cấu thành vật tư` is a read-side/reference value:

`SUM(BOMLine.Quantity * CurrentComponentSuggestedSellingPrice)`

It is shown on the Product Suggested Price screen to help users set product selling prices.

It is not:

- Actual purchase cost.
- FIFO cost.
- Product selling price.
- Profit.
- An inventory valuation.

If any component in the published BOM has no current Component Suggested Selling Price, the Product screen should show a clear warning such as `Thiếu giá vật tư`.

## ADR-010: Inventory Receipt stores actual input cost

Inventory Receipt `UnitCost` is the actual cost of a received lot.

This is the only source for future actual inventory cost and FIFO cost.

There is no need to maintain a standard input/purchase price in Pricing V2.

Future average purchase cost reports must be calculated from Inventory transactions/lots, not from Pricing.

## ADR-011: Sales uses Product/SKU only

Sales order lines reference Product/SKU.

Sales must not have direct Component sales lines in V2.

During sale confirmation:

1. Validate Product is active.
2. Resolve published BOM.
3. Validate required Components are active and inventory-enabled.
4. Validate stock availability.
5. Allocate FIFO lots.
6. Store actual cost snapshots.
7. Store price/revenue/profit snapshots.
8. Transition order status.

## ADR-012: Actual selling price belongs to Sales

Pricing provides a default suggested price.

Sales stores the actual selling price entered by the user.

If actual selling price differs from suggested price, the system may require override permission and reason depending on approved business rule.

## ADR-013: Activate/Deactivate must be symmetric for master data

For master data such as Product, Component, Warehouse, Customer, and CustomerGroup:

- Deactivate means stop using in new workflows.
- Existing historical references remain valid.
- Activate should be supported unless a business rule explicitly says deactivation is permanent.

If source code has Deactivate without Activate, this is a backend/business gap, not a UI-only issue.

## ADR-014: Operator UI must avoid technical identifiers

Operators must not type or interpret GUIDs in normal workflows.

Every selector should show business code/name and submit GUID internally.

Technical fields such as IdempotencyKey are hidden.

## ADR-015: ABP UI pattern

Use ABP ModalManager for simple CRUD:

- Product create/edit/detail.
- Component create/edit/detail.
- Customer create/edit/detail.
- Warehouse create/edit/detail.
- Price version create.
- Read-only detail modals where appropriate.

Use full-page workflows for:

- BOM editor.
- Inventory Receipt/Issue/Adjustment.
- Sales Order create/edit/details.
- Audit export/report pages.
