# UI Vietnamese Terminology

This glossary defines Vietnamese terms for the ERP operator UI. It does not
rename technical class names, API names, route names, or permission names.

## Core Terms

| English term | Vietnamese UI term | Notes |
|---|---|---|
| Product | Sản phẩm | Commercial catalog item sold to customers |
| Component | Linh kiện | Stocked inventory part used in products or sold directly |
| BOM | Định mức linh kiện | Accepted: use `Định mức linh kiện (BOM)` for menu/title; use `Định mức linh kiện` for short labels |
| BOM Version | Phiên bản định mức | Versioned BOM for a product |
| Customer | Khách hàng | Includes dealers and retail customers |
| Customer Group | Nhóm khách hàng | Configurable group such as đại lý or khách lẻ |
| Pricing | Giá | Module name may be `Quản lý giá` |
| Purchase Price | Giá mua | Component purchase price |
| Suggested Price | Giá bán đề xuất | Default suggested selling price, not final selling price |
| Inventory | Tồn kho | Module name may be `Kho hàng` where natural |
| Warehouse | Kho | Physical stock location |
| Stock Item | Mặt hàng tồn kho | Accepted |
| Lot | Lô hàng | Inventory lot |
| FIFO Cost | Giá vốn FIFO | Cost from oldest lot first |
| Sales Order | Đơn bán hàng | Sales transaction |
| Actual Selling Price | Giá bán thực tế | Final order-line price entered/confirmed by Sales |
| Revenue | Doanh thu | Calculated from actual selling price |
| Profit | Lợi nhuận | Revenue minus cost snapshot |
| Audit | Nhật ký | Generic audit/log term |
| Business Audit | Nhật ký nghiệp vụ | Accepted |

## Actions

| English action | Vietnamese UI term | Notes |
|---|---|---|
| Create | Tạo mới | Use for new records |
| Edit | Chỉnh sửa | Use for update pages/actions |
| Delete | Xóa | Avoid where backend only deactivates; use `Ngừng sử dụng` instead |
| Refresh | Làm mới | Reload data |
| Confirm | Xác nhận | Sales order confirmation |
| Cancel | Hủy | Cancel operation or cancel sales order depending context |
| Publish | Công bố | BOM publication |
| Archive | Lưu trữ | BOM archival |
| Activate | Kích hoạt | Status change to active |
| Deactivate | Ngừng sử dụng | Better than "vô hiệu hóa" for operators |
| Save | Lưu | Persist form changes |
| Search | Tìm kiếm | Filter/search |
| Export | Xuất dữ liệu | Audit export |
| Post Receipt | Ghi nhận nhập kho | Inventory receipt posting |
| Post Issue | Ghi nhận xuất kho | Inventory issue posting |
| Post Adjustment | Ghi nhận điều chỉnh kho | Inventory adjustment posting |

## Status Terms

| Status | Vietnamese UI term |
|---|---|
| Draft | Nháp |
| Published | Đã công bố |
| Archived | Đã lưu trữ |
| Active | Đang hoạt động |
| Inactive | Ngừng sử dụng |
| Confirmed | Đã xác nhận |
| Cancelled | Đã hủy |
| Closed | Đã đóng |
| Open-ended | Còn hiệu lực |

## Module Names

| Module | Vietnamese UI label |
|---|---|
| Catalog | Danh mục |
| BOM | Định mức linh kiện |
| Customer | Khách hàng |
| Customer Groups | Nhóm khách hàng |
| Pricing | Quản lý giá |
| Inventory | Kho hàng |
| Sales | Bán hàng |
| Audit | Nhật ký nghiệp vụ |

## Formatting Conventions

- Currency: `VNĐ`, display with thousands separators and no decimal places in
  normal operator screens unless precision is required.
- Quantity: display up to 4 decimal places only when needed.
- Dates: use Vietnamese date format consistently, preferably `dd/MM/yyyy`.
- Date-time: use `dd/MM/yyyy HH:mm`.
- Do not show raw GUIDs in normal workflows if a code/name is available.

## Accepted Terminology Decisions

- `Stock Item` = `Mặt hàng tồn kho`.
- `BOM` = `Định mức linh kiện (BOM)` for menu/title and `Định mức linh kiện`
  for short labels.
- `Business Audit` = `Nhật ký nghiệp vụ`.
