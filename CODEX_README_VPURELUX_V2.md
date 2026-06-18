# VPureLux ERP V2 Documentation Readme for Codex

## Purpose

This document is the first file Codex must read before doing any new VPureLux ERP work.

The project is still in development. The business flow has been clarified and the system may be rebuilt or refactored deeply when needed. Existing implementation and older documents may conflict with the V2 decisions below. In any conflict, this V2 documentation set wins.

## Required Read Order

Read these files in this exact order:

1. `CODEX_README_VPURELUX_V2.md`
2. `BUSINESS_ARCHITECTURE_DECISIONS_V2.md`
3. `MODULE_SPECIFICATIONS_V2.md`
4. `UI_UX_ABP_GUIDE_V2.md`
5. `DATA_FLOW_UAT_SCENARIOS_V2.md`
6. `IMPLEMENTATION_ROADMAP_V2.md`
7. Existing source code.

Older documents may be used only as historical references. They must not override the V2 decisions.

## Precedence

When documents conflict, use this order:

1. V2 business architecture decisions.
2. V2 module specifications.
3. V2 UI/UX ABP guide.
4. V2 data-flow/UAT scenarios.
5. Existing source code.
6. Older implementation/certification/specification documents.

## Core V2 Decisions

### Sales scope

Everything sold to customers is a **Product/SKU**.

A physical component can be sold only by creating a Product/SKU whose BOM contains that one component.

Examples:

- Selling a complete water purifier: Product/SKU has a BOM with many components.
- Selling a filter core separately: Product/SKU has a BOM with one component.
- Selling a kit/combo: Product/SKU has a BOM with multiple components.

Sales must not sell `Component` directly in V2.

### Product and Component meaning

- Product = sellable SKU.
- Component = physical inventory part used by BOM and stored in Inventory.
- Product is not directly inventoried in phase 1.
- Component is inventoried by lot, warehouse, and actual receipt cost.
- Product must have a published BOM before it can be sold.

### Pricing meaning

Pricing V2 owns suggested selling prices only:

- Component Suggested Selling Price = `Giá bán đề xuất linh kiện`.
- Product Suggested Selling Price = `Giá bán đề xuất sản phẩm`.
- Component Purchase Price / standard input price is not needed in V2.

Inventory Receipt owns actual purchase/input cost by lot:

- Receipt UnitCost = `Đơn giá nhập thực tế`.

Product price screen should show:

- Current product suggested selling price.
- `Giá cấu thành linh kiện` = sum of BOM quantities multiplied by current component suggested selling prices.

### Inventory meaning

Inventory owns physical component stock:

- Warehouse.
- Component StockItem.
- InventoryLot.
- InventoryTransaction.
- FIFO cost.
- Balance read models.

Receipt/Issue/Adjustment should be full-page operational workflows. They must be operator-friendly and must not expose raw GUIDs.

### UI meaning

Use ABP Razor Pages conventions:

- Small CRUD Create/Edit/Details should use ABP ModalManager with full-page fallback.
- Complex operational workflows remain full-page.
- Never show raw GUID fields to operators when code/name can be shown.
- Technical fields such as IdempotencyKey must be hidden.
- All row actions must be permission-aware.
- Sensitive actions must use confirmations and notifications.

## Coding Protocol

Before modifying code:

1. Read the V2 docs.
2. Inspect current source for conflicts.
3. Produce a short impact analysis.
4. Classify each change as:
   - UI-only.
   - Application contract change.
   - Domain/business change.
   - EF/migration change.
   - Test change.
5. Ask for approval before performing broad backend refactor or destructive database/migration changes.

## Forbidden Without Explicit Approval

- Deleting production data.
- Dropping migrations or resetting database against production-like data.
- Removing tests without replacing coverage.
- Adding business shortcuts in Razor Pages or JavaScript.
- Calculating FIFO cost, profit, or stock availability in UI.
- Bypassing application services.
- Accessing DbContext/repositories from Razor Pages.
- Creating fake UI actions where backend capability does not exist.

## Expected Codex Behavior

Codex must not blindly follow old docs or old source patterns. Codex must align the system to V2 decisions and explicitly report any conflicts.

When implementing, work in small batches with build and tests after each batch.
