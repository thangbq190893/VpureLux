# UAT Fix 04H.3 - Sales Revenue Report UI

## Goal

Add the first report UI page for:

- Báo cáo -> Doanh số bán hàng

This task only implements the Sales Revenue report UI. Profit report UI is deferred.

## Page Route And Menu

- Route: `/Reports/SalesRevenue`
- Menu: `Báo cáo` -> `Doanh số bán hàng`

The menu is registered as a Reports parent with a Sales Revenue child item.

## Permission

The page model requires:

- `VPureLuxPermissions.Reports.Sales.View`

The menu item is also guarded by `Reports.Sales.View`. The page does not use broad Sales permissions and does not require the Profit report permission.

## Filters

Implemented GET filters:

- Từ ngày
- Đến ngày
- Nhóm theo: Ngày / Tuần / Tháng / Quý / Năm
- Sản phẩm
- Khách hàng
- Kho
- Trạng thái thanh toán: Tất cả / Chưa thanh toán / Thanh toán một phần / Đã thanh toán / Trả dư

Default first load:

- current month
- group by Day
- no product/customer/warehouse/payment filters

Reset returns to `/Reports/SalesRevenue`.

Invalid `FromDate > ToDate` shows a friendly Vietnamese validation message and does not call the report service.

## KPI Cards

The page renders:

- Tổng doanh số
- Số đơn đã xác nhận
- Số lượng sản phẩm bán
- Giá trị đơn trung bình
- Đã thanh toán
- Còn nợ

Payment status counters are shown as compact badges for unpaid, partial, paid, and overpaid orders.

## Tables And Sections

Implemented sections:

- Doanh số theo thời gian
- Top sản phẩm theo doanh số
- Doanh số theo khách hàng
- Danh sách đơn hàng

Order numbers link to `/Sales/Details/{SalesOrderId}`.

The product section displays the top 10 rows from `Report.ByProduct` to keep the first UI version readable. Other sections display the returned report rows.

## Data Source

The page calls the existing backend from 04H.2:

- `ISalesReportsAppService.GetSalesRevenueAsync(SalesRevenueReportInput input)`

No report formulas, stored procedures, raw SQL repository, Sales, Payment, Inventory, FIFO, or BOM behavior were changed.

## UI Consistency Notes

The UI follows existing VPureLux inquiry/list pages:

- `vpl-page`
- `vpl-inquiry-page`
- `vpl-page-header`
- `vpl-inquiry-filter-card`
- `vpl-inquiry-results-card`
- `vpl-card-dense`
- `vpl-table-dense`

Lookup fields use native Bootstrap selects. No Select2, external JS/CSS, CDN, or charting dependency was added.

## Intentionally Not Changed

- No Profit report UI
- No export Excel/CSV
- No report backend rewrite
- No stored procedure changes
- No Sales/Payment/Inventory behavior changes
- No table schema/migration changes
- No external chart library

## Tests Run

- `dotnet build VPureLux.slnx --no-restore -m:2` - passed, 2 existing warnings (`Scriban` advisory and test SDK entrypoint).
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Report|FullyQualifiedName~Reports|FullyQualifiedName~SalesRevenue" -m:1` - passed, 10 tests.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Sales" -m:1` - passed, 85 tests.
- `dotnet test test/VPureLux.Application.Tests/VPureLux.Application.Tests.csproj --no-build --filter "FullyQualifiedName~Report|FullyQualifiedName~Reports|FullyQualifiedName~SalesRevenue|FullyQualifiedName~SalesProfit" -m:1` - passed, 2 tests.
- `dotnet test test/VPureLux.EntityFrameworkCore.Tests/VPureLux.EntityFrameworkCore.Tests.csproj --no-build --filter "FullyQualifiedName~Report|FullyQualifiedName~Reports|FullyQualifiedName~SalesRevenue|FullyQualifiedName~SalesProfit" -m:1` - passed, 4 tests.
- `git diff --check` - passed; Git reported line-ending normalization warnings only.
- Terminology grep for the legacy material wording - returned existing audit/evidence/prior fix-doc references only; this fix did not introduce user-facing legacy material wording.

## Manual Smoke Checklist Deferred

- Login as admin.
- Open `Báo cáo -> Doanh số bán hàng`.
- Verify default current-month data.
- Change date range.
- Filter by product/customer/warehouse/payment status.
- Click order link to Sales Details.
- Verify no raw errors.
- Verify empty state with a date range that has no data.

## Next Tasks

- 04H.4 Sales Profit Report UI
- 04H.5 Export
- 04H.6 Permission hardening
