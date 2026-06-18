# VPureLux ERP Data Flow and UAT Scenarios V2

## Flow 1: Sellable Product Built from Many Components

Create Components: LOI-PP-01, LOI-CTO-02, TEM-BH, VO-HOP.

Create Product/SKU: RO-STD-01 - Máy lọc nước RO tiêu chuẩn.

Create and publish BOM:

| Component | Quantity | Usage |
|---|---:|---|
| LOI-PP-01 | 1 | Lõi 1 |
| LOI-CTO-02 | 1 | Lõi 2 |
| TEM-BH | 1 | Tem |
| VO-HOP | 1 | Vỏ hộp |

Create Component Suggested Selling Prices:

| Component | Price |
|---|---:|
| LOI-PP-01 | 120000 |
| LOI-CTO-02 | 150000 |
| TEM-BH | 10000 |
| VO-HOP | 20000 |

Expected `Giá cấu thành linh kiện`: 300000.

Create Product Suggested Selling Price for RO-STD-01: 500000.

Expected Product price screen: BOM published, Giá cấu thành linh kiện = 300000, Giá bán đề xuất sản phẩm = 500000, Chênh lệch = 200000.

## Flow 2: Sell Loose Component as Product/SKU

Existing Component: LOI-PP-01 - Lõi PP số 1.

Create Product/SKU: LOI-PP-01-BAN - Lõi PP số 1 bán rời.

Create and publish BOM with one line: LOI-PP-01 quantity 1.

Create Product Suggested Selling Price: 130000.

Expected: Sales sees this as Product/SKU; Inventory issues Component LOI-PP-01 by FIFO.

## Flow 3: Inventory Receipt Multi-Line

Create Warehouse WH-HN.

Receipt into WH-HN:

| Component StockItem | LotNo | Quantity | UnitCost |
|---|---|---:|---:|
| LOI-PP-01 | LOT-PP-001 | 10 | 30000 |
| LOI-CTO-02 | LOT-CTO-001 | 5 | 50000 |
| TEM-BH | LOT-TEM-001 | 100 | 2000 |

Expected: no raw GUID, IdempotencyKey hidden, lots/balance/ledger created.

## Flow 4: Sales Order for Product/SKU

Prerequisites: Product has published BOM, components have inventory lots, product has suggested selling price.

Create Sales Order line: Product RO-STD-01, quantity 1, actual price defaults 500000.

On confirmation: backend expands BOM, allocates FIFO, creates immutable snapshots.

## Flow 5: Missing BOM

Try to sell Product without published BOM.

Expected: Sales blocks selection or confirmation with a clear error.

## Flow 6: Missing Component Suggested Price

Create Product BOM but one component lacks Component Suggested Selling Price.

Expected: Product price screen shows missing component price warning. Product price can still be manually entered only if business allows.

## Flow 7: Insufficient Inventory

Confirm Sales Order when a required component has insufficient stock.

Expected: confirmation fails, no partial issue, order remains draft.
