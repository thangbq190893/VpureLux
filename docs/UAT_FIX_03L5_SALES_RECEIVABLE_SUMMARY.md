# UAT Fix 03L.5A - Sales Receivable Summary Read Models

## Reason

Sales payment history is now available as the source of truth. This phase prepares backend/read-model data so Sales List and Customer History can show receivable summaries in the next UI polish phase.

## Phase A scope

Implemented backend/read-model support for:

- Sales List payment summary values
- optional Sales List payment-status filtering
- neutral draft/cancelled receivable summary behavior
- Customer History customer/dealer receivable summary
- focused tests for read-model behavior

This phase intentionally avoids visual Razor polish. 03L.5B is reserved for Cursor Composer UI layout work.

## Sales List read-model behavior

`SalesOrderDto.PaymentSummary` remains the list/detail read model:

- `TotalAmount`
- `PaidAmount`
- `RemainingAmount`
- `PaymentStatus`

Confirmed orders derive the summary from posted payment rows. Draft and cancelled orders now return neutral receivable data:

- `TotalAmount = 0`
- `PaidAmount = 0`
- `RemainingAmount = 0`
- `PaymentStatus = NotApplicable`

This avoids showing draft orders as customer debt before Sales confirmation fixes the receivable amount.

## Customer History read-model behavior

Added `CustomerReceivableSummaryDto` and `GetCustomerReceivableSummaryAsync(Guid customerId)`.

The summary includes:

- confirmed sales total
- paid total
- remaining debt
- unpaid/partial order count

Only confirmed Sales Orders are included. Draft orders do not affect receivable totals.

## Payment status filter behavior

`GetSalesOrderListInput.PaymentStatus` supports filtering by:

- `NotApplicable`
- `Unpaid`
- `PartiallyPaid`
- `Paid`
- `Overpaid`

The filter is optional. Existing customer/status filters are preserved and applied before payment-status filtering. Payment-status filtering uses the computed read model rather than persisted Sales Order columns.

## Performance/N+1 assessment

The implementation avoids obvious N+1 payment queries.

For a page/list of orders, the application collects order ids and calls `GetPostedPaidAmountsAsync(orderIds)` once, then derives summaries in memory.

For Customer History receivables, the application loads confirmed orders for the customer and calls the same aggregate payment query once. This is correct and sufficient for current UAT size. If customer order volume becomes large, a future optimization can move the full receivable aggregation into a dedicated EF projection/repository query.

## Intentionally not changed

This phase does not change:

- Sales confirmation behavior
- inventory posting
- FIFO allocation
- costing
- Sales revenue/cost/profit snapshots
- add-payment command behavior
- Sales Order number generation
- Sales Create/Confirm initial payment
- payment void/refund/reversal
- database schema/migrations

## Deferred to 03L.5B

UI polish is deferred to 03L.5B:

- Sales List receivable columns/badges
- Sales List payment status filter control
- Customer History receivable summary display
- responsive layout/visual polish

## 03L.5B UI Polish

### Sales List UI behavior

- Sales List shows compact payment columns for Tổng đơn, Đã thanh toán, Còn nợ, and a badge for Trạng thái thanh toán.
- Draft and cancelled orders display `—` for payment amounts and badge `Chưa xác nhận` (`NotApplicable`) so they do not look like customer debt.
- Confirmed orders render `Chưa thanh toán`, `Thanh toán một phần`, `Đã thanh toán`, and `Trả dư` from the 03L.5A read model.
- Payment status filter control binds to `IndexModel.PaymentStatus` and passes through to `GetSalesOrderListInput`.
- Optional `CustomerId` query binding is preserved for safe links from Customer History.
- Existing revenue column, action routes, and cost/profit visibility rules are unchanged.

### Customer History UI behavior

- Receivable summary card shows Tổng doanh số đã xác nhận, Tổng đã thanh toán, Tổng còn nợ, and Số đơn còn nợ from `CustomerReceivableSummaryDto`.
- Existing product purchase summary cards and product table remain intact.
- Safe link to Sales List filtered by `CustomerId` when a customer is selected.
- Receivable summary renders even when purchase history is empty.

### Filter UI

- Sales List exposes a GET filter form for payment status with clear-filter support.
- Customer History customer selector is unchanged.

### Backend changes

No Application, Domain, EF, DB, payment command, confirmation, inventory, or snapshot logic changes in this phase.

### Tests run

Validation completed:

```text
dotnet build VPureLux.slnx --no-restore -m:2 -> passed
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Sales" -m:1 -> passed
git diff --check -> passed
legacy component wording grep -> no matches
```

Manual browser smoke was not run in this UI polish phase.
