# UAT Fix 03L.1 - Sales Payment and Receivable Audit

## 1. Executive summary

Current Sales Order behavior supports draft creation, line editing, confirmation, inventory issue posting, and revenue/cost/profit snapshots. It does not model customer payments or receivables yet.

There is no `PaidAmount`, `RemainingAmount`, `PaymentStatus`, `PaymentMethod`, `PaymentDate`, or payment history in the Sales domain, DTOs, EF mapping, Razor pages, customer history, audit events, or focused tests. The current UI can show what was sold and the confirmed financial result, but it cannot answer how much the dealer/customer already paid, how much remains due, or when later payments were made.

The recommended target is to add a separate `SalesOrderPayment` history model in a later batch. Payment state should be derived from successful payment rows:

- `PaidAmount = sum(successful payments)`
- `RemainingAmount = SalesOrder.TotalRevenueAmount - PaidAmount`
- `PaymentStatus = Unpaid / PartiallyPaid / Paid / Overpaid`

This should be implemented without changing Sales confirmation, inventory posting, FIFO allocation, or profit calculation.

## 2. Current Sales model behavior

`SalesOrder` is a full-audited aggregate with these business fields:

- order identity: `OrderNo`
- customer and customer group snapshots
- warehouse
- order date
- status: draft, confirmed, cancelled
- currency
- confirmation idempotency key and confirmation timestamp
- cancellation timestamp
- confirmed totals: `TotalRevenueAmount`, `TotalCostAmount`, `TotalProfitAmount`
- row version
- owned `SalesOrderLine` collection

Draft orders can add, update, and remove lines. Confirmation requires validated lines and customer group snapshot data, then stores confirmed totals from line snapshots.

Confirmed orders are effectively immutable for order-line business changes. Draft mutation methods call `EnsureDraft()`, and `EnsureDraft()` rejects confirmed and cancelled orders. Draft cancellation is supported; cancellation of confirmed orders is not part of the current behavior.

Confirmation posts inventory issues per sales line. Each line posts an `InventoryTransactionType.SalesIssue` transaction through inventory services using a deterministic idempotency key:

`sales-confirm:{order.Id}:line:{salesLine.Id}`

The inventory reference is stored as:

- `ReferenceType = "SalesOrderLine"`
- `ReferenceId = sales line id`
- optional BOM version id

After inventory issue posting, line confirmation snapshots store item, BOM, inventory transaction, cost, revenue, profit, and margin values. The order then stores total revenue, cost, and profit. This means payment work must be kept separate from inventory/FIFO/profit behavior.

## 3. Current Sales UI behavior

Sales Create/Edit pages collect customer, warehouse, order date, product lines, quantities, actual selling prices, and override reasons. They do not collect payment amount, payment method, payment date, or payment note.

Sales Details shows:

- order number and status
- customer
- draft estimated revenue
- confirmed revenue
- confirmed cost when permitted
- confirmed profit when permitted
- line quantity, suggested price, actual price, override reason
- confirmed cost/profit line values when permitted
- BOM snapshot items after confirmation
- confirm/cancel actions where allowed

Sales List supports customer/status filtering and shows order summary data, but not payment status or receivable balances.

Customer History shows purchase history by product for a selected customer. It summarizes product count, revenue, profit, and latest purchase date. It does not show receivable totals, paid totals, outstanding debt, payment history, due dates, or dealer credit status.

## 4. Existing total/status fields

Existing order-level total/status fields:

- `SalesOrder.Status`
- `SalesOrder.TotalRevenueAmount`
- `SalesOrder.TotalCostAmount`
- `SalesOrder.TotalProfitAmount`
- `SalesOrder.ConfirmedAt`
- `SalesOrder.CancelledAt`

Existing line-level financial fields:

- `SalesOrderLine.RevenueAmount`
- `SalesOrderLine.CostPriceSnapshot`
- `SalesOrderLine.CostAmountSnapshot`
- `SalesOrderLine.ProfitAmount`
- `SalesOrderLine.MarginPercent`

Existing payment-related fields:

- none found on the aggregate
- none found in Sales DTOs/inputs
- none found in EF configuration
- none found in Sales Razor PageModels/pages
- none found in customer history DTOs
- none found in Sales domain events or audit handler

## 5. Payment/receivable gaps

The current system cannot record or display:

- amount paid
- remaining debt
- payment status
- payment method
- payment date
- payment reference number
- payment note
- payment history
- later payment after order confirmation
- payment reversal/void/refund
- customer/dealer receivable summary
- overdue receivable state
- due date or credit terms

A user can confirm that goods were sold and inventory was issued, but cannot track whether the customer paid in full, paid partially, or still owes money.

## 6. Recommended domain model

Add a separate payment history model in a later implementation batch. Do not use only one mutable `PaidAmount` field on `SalesOrder`, because dealers/customers may pay in several installments and the business needs traceability.

Recommended entity:

`SalesOrderPayment`

Recommended core fields:

- `Id`
- `SalesOrderId`
- `CustomerId` snapshot or denormalized customer reference for receivable queries
- `Amount`
- `PaymentDate`
- `PaymentMethod`
- `ReferenceNo`
- `Note`
- `Status`
- `IdempotencyKey` if payment submission can be retried
- standard audit fields
- optional row version/concurrency token

Recommended status approach:

- payment rows represent history
- posted/successful rows contribute to paid amount
- voided/reversed/refunded rows do not contribute, or contribute through explicit negative reversal rows if that policy is chosen later
- order payment summary is computed from payment rows

Recommended Sales Order summary:

- `PaidAmount` should be derived from successful payment rows
- `RemainingAmount` should be derived from confirmed order revenue minus paid amount
- `PaymentStatus` should be derived from total and paid amount

If performance later requires denormalized summary columns on `SalesOrder`, they should be treated as cached read state and protected by application/domain consistency rules. They should not replace payment history.

## 7. Recommended payment status rules

Use confirmed `SalesOrder.TotalRevenueAmount` as the receivable amount once an order is confirmed.

Recommended derived rules:

- `PaidAmount = 0` -> `Unpaid`
- `0 < PaidAmount < TotalRevenueAmount` -> `PartiallyPaid`
- `PaidAmount = TotalRevenueAmount` -> `Paid`
- `PaidAmount > TotalRevenueAmount` -> `Overpaid`

Recommended default policy:

- do not allow overpayment unless business explicitly approves it
- if overpayment is allowed later, display it explicitly as `Overpaid` and keep it out of revenue/profit calculations
- do not hard-delete posted payments
- correct mistakes through void/reversal/refund flows with audit trail

Draft-order payment policy is a deferred decision. The safer first version is to add later payments from confirmed Sales Details, then optionally support initial payment at confirmation. If payment is captured during draft creation, the system must define whether it is a deposit, whether it can be edited before confirmation, and what happens if the draft is cancelled.

## 8. Recommended UI changes

Sales Create / Confirm:

- optionally support an initial payment amount at confirmation
- show order total, paid amount, remaining amount, and resulting status before confirming
- keep payment optional unless business requires a minimum upfront payment

Sales Details:

- add a payment summary section:
  - total order amount
  - paid amount
  - remaining amount
  - payment status
- add payment history:
  - date
  - amount
  - method
  - reference
  - note
  - created by/time if available
  - status
- add an "Add payment" action for allowed users
- keep confirmed order line/profit/inventory values read-only

Sales List:

- add paid amount, remaining amount, and payment status columns or compact badges
- add payment status filter after the read model exists

Customer History:

- add customer/dealer receivable summary:
  - confirmed sales total
  - paid total
  - remaining debt
  - unpaid/partially paid order count
- optionally link to filtered Sales List or a receivables detail screen

## 9. Required DB/schema changes

Required later schema work:

- create a sales payment table
- add FK to sales order
- add customer reference for efficient customer receivable queries
- add amount precision matching Sales money precision
- add payment date
- add method/reference/note/status fields
- add audit fields
- add indexes for:
  - `SalesOrderId`
  - `CustomerId, PaymentDate`
  - payment status if persisted
  - idempotency key if used

Possible later schema work:

- denormalized paid/remaining/payment status columns on `SalesOrders` if query performance needs it
- due date or credit terms fields
- accounting/cash account reference
- refund/reversal relationship fields

No DB changes are made in 03L.1.

## 10. Required Application/API changes

Recommended application additions:

- payment DTOs:
  - `SalesOrderPaymentDto`
  - `CreateSalesOrderPaymentDto`
  - payment summary DTO
- read-model fields on sales order list/detail DTOs:
  - `PaidAmount`
  - `RemainingAmount`
  - `PaymentStatus`
- app service methods:
  - add payment
  - list payments for an order
  - optionally void/reverse payment in a later permissioned batch
- customer receivable query:
  - confirmed sales total
  - paid total
  - remaining total
  - unpaid/partial order count
- permissions:
  - view payments/receivables
  - manage payments
  - void/reverse payments if implemented
- audit events:
  - payment added
  - payment voided/reversed/refunded

Payment application logic should not call inventory posting and should not alter FIFO allocation, inventory transaction cost, sales line cost, or profit snapshots.

## 11. Required tests

Domain tests:

- payment status derived from paid amount
- partial, full, zero, and overpayment policy
- confirmed order can accept separate payment records without changing line snapshots
- posted payment cannot be silently deleted
- reversal/void rules once designed

EF tests:

- payment persistence
- money precision
- FK behavior
- indexes/idempotency uniqueness if used
- receivable summary query correctness

Application/API tests:

- add payment authorization
- add payment to confirmed order
- draft-order payment behavior according to chosen policy
- no overpayment if disallowed
- payment history returns in expected order
- Sales List/Details include payment summary
- Customer History includes receivable summary
- payment operations do not change inventory transaction ids, FIFO allocations, cost, or profit

Web tests:

- Create/Confirm initial payment if enabled
- Details add-payment form and payment history
- Sales List payment status/remaining amount display
- Customer History receivable summary
- validation messages for negative/zero/overpayment cases
- terminology remains `Vật tư`

Audit tests:

- payment added event is captured
- reversal/void event is captured once implemented

## 12. Risks and deferred decisions

Risks:

- accidentally changing confirmed revenue, cost, profit, or inventory posting while adding receivables
- treating payment as revenue and double-counting financial results
- allowing partial payment edits that erase audit history
- accepting payment on drafts without defining cancellation/deposit behavior
- overpayment without clear refund or credit policy
- concurrency when two users add payments at the same time
- idempotency for retried payment submissions

Deferred business decisions:

- whether payment is allowed before confirmation
- whether confirmation should capture initial payment
- whether overpayment is allowed
- whether refunds, reversals, or voids are required in the first payment batch
- whether due date and credit terms are required
- whether dealer receivables need aging buckets
- whether receivables integrate with an accounting/cashbook module
- whether payment method should be an enum, lookup table, or free text in the first release

## 13. Proposed implementation batches

03L.2 Sales payment domain/entity/read model:

- add payment entity/table and read summaries
- add derived paid/remaining/status behavior
- keep inventory/FIFO/profit unchanged

03L.3 Sales Create/Confirm initial payment:

- decide whether initial payment is captured at create or confirm
- add UI validation and application handling for the chosen path

03L.4 Sales Details add payment and payment history:

- add payment summary and payment history
- add later payment form for confirmed orders
- add focused authorization and validation

03L.5 Sales List and Customer History receivable summary:

- show payment status and remaining debt in list/detail/customer views
- add customer/dealer receivable summary

03L.6 Payment permissions/audit/refund/reversal if needed:

- add stricter permission model
- add audit events
- add void/reversal/refund behavior if approved
