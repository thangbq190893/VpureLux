# VPureLux UI Backend Gap Register

## Purpose

This file separates UI-only issues from missing backend/business capabilities.

Codex must not implement items in this register unless the prompt explicitly approves backend changes and tests.

## Gap Status Values

| Status | Meaning |
|---|---|
| Open | Confirmed or suspected gap, not approved for implementation |
| Needs source verification | Must inspect source before final classification |
| Approved separately | Product owner/architect approved backend work in a separate task |
| Closed | Gap resolved and tested |

## Gap Type Values

| Type | Meaning |
|---|---|
| Business capability gap | Business operation does not exist |
| Selector/query contract gap | UI needs a read model/list method not currently available |
| Permission gap | Existing permission model is insufficient or inconsistent |
| Documentation conflict | Spec and source disagree |
| Test gap | Behavior exists but lacks sufficient validation |

## Current Known/Suspected Gaps

### GAP-001: Catalog Component Reactivation Missing

| Field | Value |
|---|---|
| Type | Business capability gap / documentation conflict |
| Status | Open |
| Area | Catalog Components |
| Symptom | Inactive component cannot be reactivated from UI because no existing `Activate` handler/AppService was found by Codex. |
| UI-safe handling | Do not show misleading `Ngừng sử dụng` for inactive rows. Either hide action or show disabled `Kích hoạt` with explanation. |
| Forbidden in UI refactor | Do not add `ActivateAsync`, domain behavior, event, API, migration, or permission. |
| Decision needed | Confirm whether Catalog Component must support reactivation as certified business behavior. |

### GAP-002: Catalog Product Reactivation May Be Missing

| Field | Value |
|---|---|
| Type | Needs source verification |
| Status | Needs source verification |
| Area | Catalog Products |
| Symptom | Component has deactivate-only behavior; Products may have the same issue. |
| UI-safe handling | Inspect source. If no activate exists, do not invent it. |
| Decision needed | Confirm Product activate/deactivate lifecycle. |

### GAP-003: Inventory Receipt Selector Data Source

| Field | Value |
|---|---|
| Type | Selector/query contract gap |
| Status | Needs source verification |
| Area | `/Inventory/Receipt` |
| Need | Warehouse dropdown and active Component StockItem dropdown. |
| UI-safe handling | Use existing app services/list methods if available. |
| Forbidden | Do not create new selector API by default. Do not use DbContext/repository from PageModel. |
| Decision needed | If no existing service exposes active warehouses and active component stock items with Id/Code/Name, approve a selector/query contract. |

### GAP-004: Inventory Issue/Adjustment Selector Data Source

| Field | Value |
|---|---|
| Type | Selector/query contract gap |
| Status | Needs source verification |
| Area | `/Inventory/Issue`, `/Inventory/Adjustment` |
| Need | Warehouse, StockItem, lot/availability selectors. |
| UI-safe handling | Use existing services if available. |
| Forbidden | Do not calculate availability/FIFO in UI. |

### GAP-005: BOM Product/Component Selector Data Source

| Field | Value |
|---|---|
| Type | Selector/query contract gap |
| Status | Needs source verification |
| Area | `/Bom`, `/Bom/Create`, `/Bom/Edit` |
| Need | Product selector and Component row selector showing Code/Name. |
| UI-safe handling | Use existing Catalog product/component list methods if available. |
| Forbidden | Do not add BOM/Catalog API contracts without approval. Do not change BOM rules. |

### GAP-006: Sales Rich Selector Data Source

| Field | Value |
|---|---|
| Type | Selector/query contract gap / future enhancement |
| Status | Open |
| Area | Sales Create/Edit |
| Need | Customer, Warehouse, Product/Component, Published BOM version, Catalog thumbnail context. |
| UI-safe handling | Defer until Inventory/BOM selectors are stable. |
| Forbidden | Do not implement Product Inventory, Bundle, Warranty, Serial, or Sales image snapshotting. |

### GAP-007: Auto-Generate Business Code

| Field | Value |
|---|---|
| Type | Business capability gap |
| Status | Open |
| Area | Catalog, Customer, Warehouse, etc. |
| Symptom | Operators may not understand required `Code` input. |
| UI-safe handling | Add help text/convention examples only. |
| Forbidden | Do not generate Code in JavaScript. Do not remove required Code validation. |
| Decision needed | Approve application/domain-level code generation if desired. |

### GAP-008: Browser-Level Modal Test Missing

| Field | Value |
|---|---|
| Type | Test gap |
| Status | Open |
| Area | Customer/CustomerGroups modal behavior |
| Symptom | Unit/rendered tests verify hooks, but not browser execution. |
| UI-safe handling | Manual browser UAT is required; Playwright can be added separately if approved. |

### GAP-009: Catalog Image Browser File Selection Test Missing

| Field | Value |
|---|---|
| Type | Test gap |
| Status | Open |
| Area | Catalog image upload/preview |
| Symptom | Certified tests cover contracts/rendering, but no browser-driven file selection. |
| UI-safe handling | Manual UAT required or browser test later. |

### GAP-010: Sales Profit Permission Field Guard

| Field | Value |
|---|---|
| Type | UI permission gap |
| Status | Needs source verification |
| Area | Sales Details/History |
| Need | Hide profit unless `Sales.ViewProfit`; cost fields require `Sales.ViewCost` if present. |
| UI-safe handling | Field-level Razor guards if properties already available. |
| Forbidden | Do not recalculate profit/cost in UI. |

## Rules For Codex

Before touching any gap:

1. Inspect source to verify whether the backend method/service/DTO already exists.
2. If it exists, use it without changing behavior.
3. If it does not exist, do not invent it.
4. Add a clear final-output note: `requires approved backend/query contract`.
5. Keep UI truthful. Do not render enabled actions that cannot work.
