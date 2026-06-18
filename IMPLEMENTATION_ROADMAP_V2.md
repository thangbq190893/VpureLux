# VPureLux ERP Implementation Roadmap V2

## Phase 0: Source and Conflict Audit

Before implementation, Codex must read all V2 docs, inspect source, identify conflicts, and produce impact report.

Expected conflicts: old ComponentPurchasePriceVersion, direct Component sales support, missing Activate actions, raw GUID UI, old Pricing tabs.

## Phase 1: Pricing Model Realignment

Replace old Component Purchase Price semantics with Component Suggested Selling Price.

Likely changes: domain aggregate, contracts, app services, EF migration, UI, tests, audit events.

Add Product price screen `Giá cấu thành linh kiện` read-side calculation.

## Phase 2: Catalog Master Data and Activation

Product = sellable SKU. Component = inventory part.

Ensure Product and Component support Activate/Deactivate if approved.

Product/Component CRUD uses ABP modal pattern.

Product list has action `Định mức linh kiện`.

## Phase 3: BOM V2

Product selector, Component selector lines, BOM required for sales, BOM can have one or many components, external editor JS, price context.

## Phase 4: Inventory Operational UX

Receipt/Issue/Adjustment multi-line full-page workflows, selectors, hidden idempotency, confirmations, busy states, no Product inventory phase 1.

## Phase 5: Sales V2 Realignment

Sales sells Product/SKU only. Remove direct Component sales from UI and refactor backend if required.

Sales uses Product -> Published BOM -> Component FIFO -> snapshots.

## Phase 6: Audit Alignment

Audit V2 terminology and business events; no Base64; readable details.

## Phase 7: ABP UI Completion

Modal CRUD, history modals, action menus, notifications, responsive layout, Vietnamese terminology pass.

## Migration Strategy

Because the project is in development, destructive reset may be acceptable only after explicit user approval. Codex must never assume database can be dropped.

For any migration reset or rename: report affected migrations, data loss risk, ask approval, then validate EF model.

## Batch Rules

Work in small batches. After each batch run build, relevant tests, full tests for backend changes, and EF pending model check for EF changes.
