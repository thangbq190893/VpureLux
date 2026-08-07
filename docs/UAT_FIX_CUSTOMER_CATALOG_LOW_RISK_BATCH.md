# UAT Fix - Customer/Catalog Low Risk Batch

## Issue

User reported multiple production issues and asked to fix from low-risk/simple items first.

Handled in this batch:

- Customer code should be auto-generated.
- Default customer groups should be standardized in Vietnamese.
- Product/material status actions must remain usable.

Already handled by prior deployed work:

- Inventory balances, stock summary, lots, and ledger list pagination.

Deferred for separate scoped work:

- Lot receipt price/quantity drilldown history.
- BOM material-cost rollup from inventory receipt costs.
- Pricing base cost from receipt costs and later selling price auto-fill.
- Expense category master data and expense entry flow.
- Sales/revenue/net-profit report definitions and expense integration.

## Root Cause

- Customer creation still treated `Code` as a required manual input.
- Customer seed data used English default group names (`Retail`, `Dealer`, `Distributor`, `Project`).
- Catalog status behavior was already present in service/UI after the DataTables refactor, but focused tests only checked deactivate, not activate.

## Fix

- Customer create now allows blank code and generates daily codes through the shared business code generator using `CUS-yyyyMMdd####`.
- Customer create full page and modal show `Tự động sinh khi lưu` instead of posting `Input.Code`.
- Customer repository can seed the generator from the current maximum same-day customer code suffix.
- Default customer group seed names are Vietnamese:
  - `RETAIL`: `Khách lẻ`
  - `DEALER`: `Đại lý`
  - `DISTRIBUTOR`: `Nhà phân phối`
  - `PROJECT`: `Khách dự án`
- Existing customized group names are preserved; only legacy English default names are normalized.
- Catalog service tests now verify both deactivate and activate for products and materials.

## Intentionally Not Changed

- No database schema or migration.
- No sales, inventory posting, FIFO, costing, BOM, or pricing behavior changed.
- Product code remains manual as previously requested.
- Existing custom customer group names are not overwritten.

## Tests Run

- `dotnet build VPureLux.slnx --no-restore -m:2`
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Customer|FullyQualifiedName~Catalog" -m:1`
- `dotnet test test/VPureLux.EntityFrameworkCore.Tests/VPureLux.EntityFrameworkCore.Tests.csproj --no-build --filter "FullyQualifiedName~Customer|FullyQualifiedName~Catalog" -m:1`
- `git diff --check`
- Terminology grep for the legacy material wording across `src`, `test`, and `docs`.

## Manual Smoke Deferred

- Create customer from UI and confirm generated code.
- Edit customer group name from UI.
- Deactivate and reactivate product/material from UI.
