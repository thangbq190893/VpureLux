# UAT Fix 03L.3 - Sales Details Add Payment

## Reason

Dealers/customers may pay a confirmed Sales Order in installments. This batch adds the first write path so users can record payments from Sales Details and see the resulting paid amount, remaining debt, payment status, and payment history.

## Scope

Implemented:

- `AddPaymentAsync(Guid id, CreateSalesOrderPaymentDto input)` application command
- HTTP API endpoint: `POST /api/sales/orders/{id}/payments`
- Sales Details payment summary
- Sales Details payment history
- Sales Details add-payment form for confirmed orders
- focused application/EF, API, and Razor Page coverage

## Add-payment rules

The add-payment command:

- requires the Sales Order to exist
- requires the Sales Order to be `Confirmed`
- rejects draft and cancelled orders
- requires a positive amount
- requires a payment date
- requires a valid payment method
- requires an idempotency key
- creates a `SalesOrderPayment` with `Posted` status
- keeps payment history as the source of truth
- does not add denormalized paid/remaining columns to `SalesOrder`

Idempotency behavior:

- a new idempotency key creates one payment row
- replaying the same key for the same order returns the existing payment
- using an existing key for another order is rejected

## Overpayment policy

Overpayment is blocked by default.

Before creating a payment, the application computes:

- `RemainingAmount = SalesOrder.TotalRevenueAmount - sum(posted payments)`

If the new amount is greater than the remaining amount, the command returns a friendly business error and does not create a payment row.

The read model still defensively supports `Overpaid` from existing data, but this write path does not allow creating that state.

## Payment status behavior

Payment summary remains derived:

- no posted payments -> `Unpaid`
- posted total less than order total -> `PartiallyPaid`
- posted total equal to order total -> `Paid`
- posted total greater than order total -> `Overpaid` defensively only

Voided rows, when present, do not contribute to paid amount.

## UI behavior

Sales Details now shows:

- total order amount
- paid amount
- remaining amount
- payment status
- payment history with date, amount, method, reference, note, and row status

The add-payment form is shown only when:

- the order is confirmed
- the user has payment manage permission

Draft and cancelled orders do not show the add-payment form.

## Permissions decision

Added payment-specific permissions:

- `Sales.Payments.View`
- `Sales.Payments.Manage`

The write command uses `Sales.Payments.Manage`. Existing Sales detail/list read paths remain under Sales view permissions while showing the derived summary. The payment history read endpoint remains part of the Sales read surface for this batch so Sales Details can load consistently.

## Intentionally not changed

This batch does not change:

- Sales confirmation behavior
- Sales Order number generation
- inventory issue posting
- FIFO allocation
- inventory costing
- Sales revenue/cost/profit snapshots
- Sales Create/Confirm initial payment
- Customer History receivable summary
- payment void/reversal/refund behavior

## Deferred

- initial payment at Sales Create/Confirm
- Customer History receivable summary
- payment void/reversal/refund
- stricter payment read permission separation if business wants payment history hidden from general Sales viewers
- Cursor UI polish if needed

## Tests run

- `dotnet build VPureLux.slnx --no-restore -m:2` - passed
- `dotnet test test/VPureLux.Domain.Tests/VPureLux.Domain.Tests.csproj --no-build --filter "FullyQualifiedName~Sales" -m:1` - passed, 9 tests
- `dotnet test test/VPureLux.EntityFrameworkCore.Tests/VPureLux.EntityFrameworkCore.Tests.csproj --no-build --filter "FullyQualifiedName~SalesOrderPayment|FullyQualifiedName~Sales" -m:1` - passed, 21 tests
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Sales" -m:1` - passed, 62 tests

Manual browser smoke is deferred/not run.
