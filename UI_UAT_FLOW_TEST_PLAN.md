# VPureLux UI UAT Flow Test Plan

## Purpose

This file defines practical operator UAT flows. These tests are manual/browser-oriented and complement automated tests.

## 1. Customer and CustomerGroup Modal Smoke Test

### CustomerGroups

1. Open `/CustomerGroups`.
2. Click `Tạo mới`.
3. Expected: popup modal opens; browser URL does not change to `/CustomerGroups/Create`.
4. Submit empty form.
5. Expected: modal stays open and validation messages appear.
6. Enter:
   - Code: `CG-DL1`
   - Name: `Đại lý cấp 1`
7. Save.
8. Expected: modal closes, list reloads, success notification appears.
9. Click Details.
10. Expected: read-only modal opens.
11. Click Edit.
12. Expected: edit modal opens, Code is read-only/disabled.
13. Deactivate.
14. Expected: confirmation appears, then status changes and notification appears.
15. Directly open `/CustomerGroups/Create`.
16. Expected: full-page fallback still works.

### Customers

1. Open `/Customers`.
2. Click `Tạo mới`.
3. Expected: popup modal opens; URL does not change.
4. Submit empty form.
5. Expected: modal stays open, validation appears, CustomerGroup dropdown still has options.
6. Enter:
   - Code: `CUS-DL001`
   - Name: `Đại lý Minh Anh`
   - CustomerGroup: `CG-DL1 - Đại lý cấp 1`
   - Phone: `0988000001`
7. Save.
8. Expected: modal closes, list reloads, success notification appears.
9. Details/Edit/Deactivate work as CustomerGroup does.
10. Direct fallback routes `/Customers/Create`, `/Customers/Edit/{id}`, `/Customers/Details/{id}` still work.

## 2. Inventory Receipt Flow

Goal: Verify nhập kho can be done without typing GUID.

### Setup

Create or confirm existing:

- Component: `COMP-PP1M - Lõi PP 1 Micron`, Active.
- Component: `COMP-CARBON - Lõi Carbon`, Active.
- Warehouse: `WH-HN - Kho Hà Nội`, Active.

### Test Receipt 1

1. Open `/Inventory/Receipt`.
2. Expected:
   - No visible `WarehouseId` raw GUID textbox.
   - No visible `StockItemId` raw GUID textbox.
   - No visible `IdempotencyKey`.
   - Warehouse selector displays `WH-HN - Kho Hà Nội`.
   - StockItem selector displays `COMP-PP1M - Lõi PP 1 Micron`.
3. Select Warehouse: `WH-HN - Kho Hà Nội`.
4. Select StockItem: `COMP-PP1M - Lõi PP 1 Micron`.
5. Quantity: `10`.
6. UnitCost: `30000`.
7. LotNo: `PP-LOT-001`.
8. ReceivedAt: `01/06/2026 09:00`.
9. Submit.
10. Expected: confirmation appears if implemented.
11. Expected: success notification or clear success result.
12. Check Balances: PP1M quantity = `10`.
13. Check Lots: Lot `PP-LOT-001`, qty `10`, cost `30000`.
14. Check Ledger: Receipt `+10`.

### Test Receipt 2 Same Component Different Cost

1. Open `/Inventory/Receipt`.
2. Select same Warehouse and StockItem.
3. Quantity: `10`.
4. UnitCost: `25000`.
5. LotNo: `PP-LOT-002`.
6. ReceivedAt: `10/06/2026 09:00`.
7. Submit.
8. Expected Balance: PP1M total `20`.
9. Expected Lots:
   - `PP-LOT-001`: qty `10`, unit cost `30000`.
   - `PP-LOT-002`: qty `10`, unit cost `25000`.

### Test Receipt 3 Other Component

1. Select `COMP-CARBON - Lõi Carbon`.
2. Quantity: `5`.
3. UnitCost: `50000`.
4. LotNo: `CARBON-LOT-001`.
5. Submit.
6. Expected Balance:
   - PP1M = `20`.
   - Carbon = `5`.

### Negative Receipt Tests

| Case | Input | Expected |
|---|---|---|
| Quantity zero | Quantity = `0` | Validation error; no lot/ledger |
| Unit cost zero/negative | UnitCost = `0` or `-1` | Validation or documented business behavior; no silent bad data |
| Inactive Warehouse | select inactive warehouse if visible | Should not be visible or should fail backend validation |
| Inactive StockItem | select inactive if visible | Should not be visible or should fail backend validation |
| Product StockItem | product stock item visible | Should not be selectable in phase 1 |
| Duplicate LotNo same warehouse/stock item | same LotNo | Should fail if unique lot rule applies |

## 3. BOM Selector Flow

Goal: Verify BOM can be created without typing ProductId/ComponentId GUIDs.

1. Open `/Bom`.
2. Expected: Product selector shows `PROD-... - Product Name`, not GUID.
3. Select a Product.
4. Open product BOM history.
5. Expected: title/header shows product code/name.
6. Create BOM draft.
7. Expected: component rows use component selector showing `COMP-... - Component Name`.
8. Add components and quantities.
9. Save draft.
10. Publish with confirmation.
11. Expected: Published status localized.
12. Edit should be hidden/disabled for Published BOM.

## 4. Catalog Image and Status Flow

1. Open `/Catalog/Components`.
2. Expected: list displays image thumbnails or placeholders.
3. Active rows show `Ngừng sử dụng` only when deactivate is supported.
4. Inactive rows must not show misleading `Ngừng sử dụng` again.
5. If Activate backend is missing, UI must clearly show action unavailable or hide it.
6. Open Create/Edit.
7. Select image file under 2 MB, JPEG/PNG/WEBP.
8. Expected: preview appears.
9. Save.
10. Expected: list thumbnail appears; no Base64 visible in HTML source.
11. Remove image.
12. Expected: confirmation appears; image removed; placeholder appears.

## 5. Pricing Flow

1. Open `/Pricing`.
2. Product/Component names should be readable, not raw IDs.
3. History link visible only with `Pricing.History`.
4. Create new Component Purchase Price Version.
5. Required:
   - Price > 0.
   - Reason required.
   - EffectiveFrom not backdated.
6. Expected: previous active version closed, new version active.
7. Repeat for Product Suggested Price.
8. Pricing must not show or calculate sales profit.

## 6. Sales Flow Deferred

Do not run full Sales UAT until:

- Inventory Receipt/Issue/Adjustment selectors work.
- BOM selectors work.
- Catalog products/components are usable.

When ready, Sales UAT must verify:

- Customer selector.
- Warehouse selector.
- Product/Component selector.
- Published BOM selection for Product lines.
- Actual Selling Price is revenue source.
- FIFO cost comes from Inventory, not UI calculation.
- Profit hidden without `Sales.ViewProfit`.
- Cost hidden without `Sales.ViewCost` if present.

## 7. Audit Flow

1. Perform business actions in Catalog/Customer/Inventory.
2. Open `/Audit`.
3. Search by module/action/date.
4. Open detail.
5. Expected: readable business event data.
6. No Base64 image content.
7. Export with `Audit.Export` permission only.
8. Export action should be audited.
