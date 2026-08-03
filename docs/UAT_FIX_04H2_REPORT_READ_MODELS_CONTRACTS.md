# UAT Fix 04H.2 - Sales Revenue and Sales Profit Report Read Models

## Goal

Add backend contracts and read models for:

- Báo cáo Doanh số bán hàng
- Báo cáo Lợi nhuận bán hàng / Lợi nhuận theo giá vốn

No Razor report UI is included in this task. UI work is deferred to 04H.3 and 04H.4.

## Report Definitions

Revenue is the value of confirmed sales order lines. Draft and cancelled orders are excluded. Payments are not revenue.

Profit is revenue minus stored cost snapshot. The report reads `SalesOrderLine.CostAmountSnapshot`, `SalesOrderLine.ProfitAmount`, and `SalesOrderLine.MarginPercent` captured at sales confirmation time. It does not recalculate historical cost from current BOM, current inventory, FIFO, or current material pricing.

## Data Sources

- `AppSalesOrders`
- `AppSalesOrderLines`
- `AppSalesOrderPayments`
- `AppWarehouses`

Only `SalesOrderStatus.Confirmed` orders are included. Only `SalesOrderPaymentStatus.Posted` payments are included in paid and remaining receivable amounts.

## Contracts and Services Added

- `ReportPeriodGroup`
- `SalesRevenueReportInput`
- `SalesRevenueReportDto`
- `SalesProfitReportInput`
- `SalesProfitReportDto`
- `ISalesReportsAppService`
- `ISalesReportReadRepository`
- `SalesReportsAppService`

The report payment filter and order row use the existing `SalesOrderReceivableStatus` enum because the required values are `Unpaid`, `PartiallyPaid`, `Paid`, and `Overpaid`. This matches existing Sales payment summary semantics.

## Permissions

Added:

- `VPureLux.Reports.Sales.View`
- `VPureLux.Reports.Profit.View`
- `VPureLux.Reports.Export`

Revenue report requires `Reports.Sales.View`. Profit report requires `Reports.Profit.View`.

## Query Approach

The Application service only handles authorization, input validation, default date handling, and detail row limit normalization. Heavy aggregation is behind `ISalesReportReadRepository` and implemented in the EntityFrameworkCore project using the current ABP DbContext connection and current transaction when available.

Complex multi-join/group LINQ was avoided in the Application layer.

## Stored Procedures

Migration `AddSalesReportStoredProcedures` creates or alters SQL Server stored procedures only:

- `dbo.sp_VP_ReportSalesRevenue`
- `dbo.sp_VP_ReportSalesProfit`

Each procedure returns five predictable result sets.

Revenue result sets:

1. Summary
2. ByPeriod
3. ByProduct
4. ByCustomer
5. Orders

Profit result sets:

1. Summary
2. ByPeriod
3. ByProduct
4. ByCustomer
5. Lines

The EF test provider is SQLite in-memory and cannot execute SQL Server stored procedures, so the EF implementation has a provider-aware SQLite raw SQL fallback with the same report semantics for integration coverage.

## Date Filter Strategy

If both dates are omitted, the Application service defaults to the current month. User `ToDate` is inclusive and is converted to an exclusive next-day boundary:

`ConfirmedAt >= @FromDate AND ConfirmedAt < @ToDateExclusive`

The query does not apply date functions to `ConfirmedAt` in the WHERE clause.

## Missing Cost Behavior

Current Sales schema stores confirmation cost/profit snapshots as non-null decimals and confirmed lines normally have an inventory transaction snapshot. The profit report marks missing cost when a confirmed line has no inventory transaction snapshot.

Missing-cost rows:

- remain explicit via `MissingCost = true`
- use `0` for cost/profit/margin DTO decimals
- are counted in `MissingCostLineCount`
- are not treated as loss rows

SQLite tests document the current schema behavior by asserting `MissingCostOnly` returns no rows for normal confirmed sales.

## Performance Notes

The stored procedures:

- aggregate in SQL
- use payment totals grouped once
- avoid N+1 reads
- use parameterized filters
- avoid `SELECT *`
- avoid functions on indexed confirmation dates in WHERE filters

Existing indexes on sales status/date, line product, payment sales order, and payment customer/date support the current report filters. No new indexes were added because this task allowed stored procedure changes only.

## Intentionally Not Changed

- No Sales confirmation behavior changes
- No payment behavior changes
- No inventory/FIFO/costing changes
- No current BOM or material price recalculation
- No report tables
- No triggers
- No Razor report UI
- No schema/table/index changes

## Tests Run

- `dotnet build VPureLux.slnx --no-restore -m:2` - passed, 1 existing warning (`Scriban` advisory) on final build.
- `dotnet test test/VPureLux.Application.Tests/VPureLux.Application.Tests.csproj --no-build --filter "FullyQualifiedName~Report|FullyQualifiedName~Reports|FullyQualifiedName~SalesRevenue|FullyQualifiedName~SalesProfit" -m:1` - passed, 2 tests.
- `dotnet test test/VPureLux.EntityFrameworkCore.Tests/VPureLux.EntityFrameworkCore.Tests.csproj --no-build --filter "FullyQualifiedName~Report|FullyQualifiedName~Reports|FullyQualifiedName~SalesRevenue|FullyQualifiedName~SalesProfit" -m:1` - passed, 4 tests.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Sales" -m:1` - passed, 77 tests.
- `git diff --check` - passed; Git reported line-ending normalization warnings only.
- Terminology grep for the legacy material wording - returned existing audit/evidence/prior fix-doc references only; this fix did not introduce user-facing legacy material wording.

## Manual Smoke Checklist Deferred

- Open future Sales Revenue report UI and verify default current-month filters
- Verify Day/Month grouping display labels
- Verify payment status filter labels
- Verify Profit report loss and missing-cost toggles
- Verify report permissions against non-admin roles

## Next Tasks

- 04H.3 Sales Revenue Report UI
- 04H.4 Sales Profit Report UI
- 04H.5 Export
- 04H.6 Permission hardening
