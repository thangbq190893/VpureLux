# UAT Fix 04H.4-04H.6 - Report UI, CSV Export, And Permission Batch

## Goal

Finish the remaining report UX work so the user can test reports end to end:

- 04H.4 Sales Profit Report UI
- 04H.5 CSV export for Sales Revenue and Sales Profit reports
- 04H.6 Report permission/menu hardening

## Pages, Routes, And Menu

- `/Reports/SalesRevenue` - Báo cáo doanh số bán hàng
- `/Reports/SalesProfit` - Báo cáo lợi nhuận bán hàng

Menu:

- `Báo cáo`
- `Doanh số bán hàng`
- `Lợi nhuận bán hàng`

The Reports parent is a neutral container. Each child is protected by its own report permission so a role with only one report permission can still see the report group and its allowed report.

## Permissions

- Revenue page: `Reports.Sales.View`
- Profit page: `Reports.Profit.View`
- Revenue export: `Reports.Sales.View` from the page plus server-side `Reports.Export`
- Profit export: `Reports.Profit.View` from the page plus server-side `Reports.Export`

Export buttons are hidden when `Reports.Export` is not granted, and export handlers return forbidden when that permission is missing.

## Profit Report UI Design

The Profit report follows the existing Sales Revenue report style and VPureLux inquiry page conventions:

- native GET filters
- compact filter card
- KPI cards
- dense ABP tables
- no Select2
- no chart library
- no external JS/CSS dependency

Filters:

- Từ ngày
- Đến ngày
- Nhóm theo
- Sản phẩm
- Khách hàng
- Kho
- Chỉ hiển thị dòng lỗ
- Chỉ hiển thị dòng chưa có giá vốn

Sections:

- Lợi nhuận theo thời gian
- Lợi nhuận theo sản phẩm
- Lợi nhuận theo khách hàng
- Chi tiết dòng bán hàng

Order numbers link to Sales Details.

## Export Behavior

Both report pages include `Xuất CSV` when the user has `Reports.Export`.

Exports respect the current filters:

- date range
- group by
- product/customer/warehouse
- revenue payment status
- profit LossOnly/MissingCostOnly

CSV file names:

- `bao-cao-doanh-so-ban-hang-yyyyMMdd-HHmm.csv`
- `bao-cao-loi-nhuan-ban-hang-yyyyMMdd-HHmm.csv`

## CSV Format

CSV is UTF-8 with BOM for Excel-friendly Vietnamese.

Values are comma-separated with standard CSV escaping for quotes, commas, and newlines.

Money and quantity values are exported as plain numeric values without currency symbols, so Excel can calculate with them. Dates use `yyyy-MM-dd HH:mm`. Enum/status values are exported as Vietnamese labels.

Revenue CSV sections:

- Tổng quan
- Doanh số theo thời gian
- Top sản phẩm theo doanh số
- Doanh số theo khách hàng
- Danh sách đơn hàng

Profit CSV sections:

- Tổng quan
- Lợi nhuận theo thời gian
- Lợi nhuận theo sản phẩm
- Lợi nhuận theo khách hàng
- Chi tiết dòng bán hàng

## Missing-Cost Rendering

If a Profit detail line has `MissingCost = true`, the UI displays:

- Cost: `Chưa có giá vốn`
- Profit: `—`
- Margin: `—`
- Note: `Thiếu giá vốn`

Missing-cost lines do not show a fake 100% margin. A warning is shown when the summary contains missing-cost lines.

## Permission/Menu Hardening

The Profit report is not exposed by Sales Revenue permission. Revenue and Profit children have separate menu permissions. Export is separate from report viewing.

The parent menu does not require both report permissions, avoiding a restricted-role case where a user with only Revenue or only Profit permission would lose the whole report group. Page authorization still blocks unauthorized direct access.

## Intentionally Not Changed

- No Sales confirmation behavior changes
- No payment behavior changes
- No Inventory/FIFO/costing changes
- No report formula changes
- No stored procedure changes
- No DB schema/table/trigger changes
- No complex LINQ report aggregation added
- No external chart/export dependency added

## Tests Run

- `dotnet build VPureLux.slnx --no-restore -m:2` - passed, 0 errors, existing Scriban advisory warning.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Report|FullyQualifiedName~Reports|FullyQualifiedName~SalesRevenue|FullyQualifiedName~SalesProfit" -m:1` - passed, 20 tests.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Sales" -m:1` - passed, 91 tests.
- `dotnet test test/VPureLux.Application.Tests/VPureLux.Application.Tests.csproj --no-build --filter "FullyQualifiedName~Report|FullyQualifiedName~Reports|FullyQualifiedName~SalesRevenue|FullyQualifiedName~SalesProfit" -m:1` - passed, 2 tests.
- `dotnet test test/VPureLux.EntityFrameworkCore.Tests/VPureLux.EntityFrameworkCore.Tests.csproj --no-build --filter "FullyQualifiedName~Report|FullyQualifiedName~Reports|FullyQualifiedName~SalesRevenue|FullyQualifiedName~SalesProfit" -m:1` - passed, 4 tests.
- `git diff --check` - passed.
- `git grep -n -i "linh kiện" -- src test docs BUSINESS_ARCHITECTURE_DECISIONS_V2.md UI_IMPLEMENTATION_DECISION_MATRIX.md UI_REFACTOR_SOURCE_OF_TRUTH.md UI_UX_ABP_GUIDE_V2.md` - returned existing UAT/audit evidence references only; no new report UI/source wording was introduced.

## Manual Smoke Checklist Deferred

- Login admin.
- Open `Báo cáo -> Doanh số bán hàng`.
- Export Revenue CSV.
- Open `Báo cáo -> Lợi nhuận bán hàng`.
- Filter by date/product/customer/warehouse.
- Toggle LossOnly and MissingCostOnly.
- Click Sales Order link.
- Export Profit CSV.
- Verify CSV opens with Vietnamese headers.
- Verify report menu visibility with restricted role later.
- Verify no raw errors.

## Future Improvements

- 04H.5 can later be extended to Excel if a project-standard lightweight exporter is approved.
- 04H.6 can later add role-level browser smoke tests once restricted-role test fixtures exist.
