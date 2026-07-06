# UAT Fix 03L.2 - Sales Payment Domain and Read Model

## Reason

Dealers/customers may pay a Sales Order in multiple installments. The system needs payment history as the source of truth so later batches can show paid amount, remaining debt, and receivable status without changing Sales confirmation, inventory issue posting, FIFO, cost, or profit snapshots.

## Scope

This batch adds the backend foundation only:

- Sales payment history entity
- EF Core table mapping
- payment summary/read model
- additive Sales DTO read fields
- read-only application/API methods for payment summary and payment history
- focused domain and EF/application integration tests

No Sales UI is implemented in this batch.

## Entity/table design

New entity:

`SalesOrderPayment`

Fields:

- `Id`
- `SalesOrderId`
- `CustomerId`
- `Amount`
- `PaymentDate`
- `PaymentMethod`
- `ReferenceNo`
- `Note`
- `Status`
- `IdempotencyKey`
- audit fields from `FullAuditedAggregateRoot<Guid>`
- `RowVersion`

New table:

`AppSalesOrderPayments`

Indexes:

- `IX_SalesOrderPayments_SalesOrderId`
- `IX_SalesOrderPayments_CustomerId_PaymentDate`
- `UX_SalesOrderPayments_IdempotencyKey` filtered to non-null, non-deleted rows

Foreign keys:

- `SalesOrderId` -> `AppSalesOrders`
- `CustomerId` -> `AppCustomers`

Both use restricted delete behavior.

## Payment status rules

Payment rows have a row status:

- `Posted`
- `Voided`

Only `Posted` rows contribute to receivable summaries. Void/reversal behavior is intentionally deferred; the enum keeps the model extensible.

Receivable status is derived:

- `PaidAmount = sum(posted payment rows)`
- `RemainingAmount = TotalAmount - PaidAmount`
- `PaidAmount <= 0` -> `Unpaid`
- `0 < PaidAmount < TotalAmount` -> `PartiallyPaid`
- `PaidAmount = TotalAmount` -> `Paid`
- `PaidAmount > TotalAmount` -> `Overpaid`

03L.2 defensively derives `Overpaid` for read-model correctness. Later payment command batches should block overpayment by default unless business explicitly approves it.

## Read model behavior

Added DTOs:

- `SalesOrderPaymentDto`
- `SalesOrderPaymentSummaryDto`

Added `SalesOrderDto.PaymentSummary`:

- `TotalAmount`
- `PaidAmount`
- `RemainingAmount`
- `PaymentStatus`

Added read-only application/API methods:

- `GetPaymentSummaryAsync(Guid id)`
- `GetPaymentsAsync(Guid id)`

Sales list/detail DTOs now include payment summary data. Payment history remains separate and read-only in this batch.

## DB migration name

Migration name:

`AddSalesOrderPayments`

The migration creates the payment table and required FK/index metadata. It does not add denormalized paid/remaining columns to `AppSalesOrders`.

## Intentionally not changed

This batch does not change:

- Sales confirmation behavior
- Sales order number generation
- inventory issue posting
- FIFO allocation
- inventory cost behavior
- Sales revenue/cost/profit snapshots
- Sales Create/Edit/Confirm UI
- Sales Details payment entry UI
- Customer History receivable UI
- refund/reversal/void commands
- payment permissions beyond existing Sales view authorization

## Deferred batches

03L.3 Sales Create/Confirm initial payment:

- decide whether initial payment is captured at create or confirm
- add validation for initial payment amount/method/reference

03L.4 Sales Details add payment and payment history:

- add payment-entry command
- block overpayment by default
- show payment history in Sales Details

03L.5 Sales List and Customer History receivable summary:

- expose receivable columns/badges in Sales List
- show customer/dealer receivable totals

03L.6 permissions/audit/refund/reversal:

- add payment-specific permissions
- add payment audit events
- add void/reversal/refund behavior if approved

## Tests run

- `dotnet build VPureLux.slnx --no-restore -m:2` - passed
- `dotnet test test/VPureLux.Domain.Tests/VPureLux.Domain.Tests.csproj --no-build --filter "FullyQualifiedName~Sales" -m:1` - passed, 9 tests
- `dotnet test test/VPureLux.EntityFrameworkCore.Tests/VPureLux.EntityFrameworkCore.Tests.csproj --no-build --filter "FullyQualifiedName~Sales" -m:1` - passed, 16 tests
- `dotnet test test/VPureLux.Application.Tests/VPureLux.Application.Tests.csproj --no-build --filter "FullyQualifiedName~Sales" -m:1` - completed with no matching Sales tests in that project

Manual browser smoke is deferred/not run because this batch has no UI changes.

## 03L.2.1 Application coverage

Added focused integration coverage in `VPureLux.EntityFrameworkCore.Tests` because the `VPureLux.Application.Tests` project has no existing Sales application-service tests and uses the domain test module rather than the EF-backed Sales workflow setup.

Tests added:

- `SalesOrderPayment_Read_Model_Should_Derive_Unpaid_Partial_Paid_And_Ignore_Voided`
- `SalesOrderPayment_History_Should_Return_Expected_Order`

Coverage:

- `GetPaymentSummaryAsync` returns unpaid when no payment rows exist
- posted payment rows derive partially paid and paid statuses
- voided rows do not contribute to paid amount
- `GetPaymentsAsync` returns history newest first
- `SalesOrderDto.PaymentSummary` is populated in detail and list reads
- payment rows do not change revenue/cost/profit snapshots or inventory transaction references

HTTP API endpoint smoke coverage was added to the existing Sales API route test for:

- `/api/sales/orders/{id}/payment-summary`
- `/api/sales/orders/{id}/payments`

No production behavior was changed in 03L.2.1.

03L.2.1 validation:

- `dotnet build VPureLux.slnx --no-restore -m:2` - passed
- `dotnet test test/VPureLux.EntityFrameworkCore.Tests/VPureLux.EntityFrameworkCore.Tests.csproj --no-build --filter "FullyQualifiedName~SalesOrderPayment" -m:1` - passed, 2 tests
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~SalesOrderPayment" -m:1` - passed, 1 test
