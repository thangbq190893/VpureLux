# UAT Fix 04C.1 - Sales Payment Recording Fix

## Issue

UAT Snapshot 04A Pass 2 reproduced `PAY-HIST-001`: submitting a partial payment from Sales Details for `SO-202607-000003` did not show a new payment-history row and did not update paid amount, remaining amount, or payment status.

## UAT Evidence

- Order: `SO-202607-000003`
- Details URL: `/Sales/Details/d772323f-d43c-4b14-fc1e-3a224b2697f2`
- Submitted amount: `500000`
- Reference: `UAT04A2_20260707_0233PAY1`
- Observed after submit:
  - `Da thanh toan` stayed `0 ₫`
  - `Con no` stayed `1.500.000 ₫`
  - payment status stayed `Chua thanh toan`
  - payment history stayed empty

## Root Cause

The application command and API payment endpoint already persisted `SalesOrderPayment` rows correctly, but the Sales Details browser form path was under-covered. The Razor form did not explicitly carry the route id in its generated post target, and the PageModel depended on default model binding for decimal/date/enum payment fields. If the browser form submitted with a route/culture binding mismatch, the handler could reload the page without creating a payment row, leaving the summary and history unchanged.

Existing tests called `OnPostAddPaymentAsync` directly or used the JSON API, so they bypassed the rendered HTML form action, hidden idempotency field, anti-forgery/form encoding, and browser-style amount/date/method values.

## Fix

- Made the Sales Details add-payment form post the current Sales Order id explicitly.
- Rendered the payment amount input with invariant numeric formatting for browser `type="number"` compatibility.
- Normalized posted payment form values in `OnPostAddPaymentAsync` before validation:
  - parses invariant browser values such as `500000` and `500000.50`
  - parses Vietnamese comma decimals such as `500000,50`
  - parses HTML date values such as `yyyy-MM-dd`
  - parses payment methods by enum name or numeric value
- Kept persistence in the existing `AddPaymentAsync` application command.
- Did not change confirmation, inventory posting, FIFO/costing, revenue/cost/profit snapshots, order number generation, or database schema.

## Validation Behavior

- Valid partial payment creates a `SalesOrderPayment` row and reloads Details with:
  - new payment-history row
  - updated paid amount
  - updated remaining amount
  - status `Thanh toán một phần`
  - success feedback
- Paying the remaining amount updates status to `Đã thanh toán`.
- Overpayment remains blocked by the application command.
- Invalid amount, date, payment method, or idempotency key returns the same page with friendly validation and no payment row.
- Payment recording remains financially neutral for revenue, cost, profit, and inventory movement.

## Tests Run

- `dotnet build VPureLux.slnx --no-restore -m:2` - passed with 1 existing test SDK warning.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Sales" -m:1` - passed, 77 tests.
- `dotnet test test/VPureLux.EntityFrameworkCore.Tests/VPureLux.EntityFrameworkCore.Tests.csproj --no-build --filter "FullyQualifiedName~SalesOrderPayment|FullyQualifiedName~Sales" -m:1` - passed, 23 tests.
- `git diff --check` - passed, with line-ending normalization warnings only.
- `git grep -n -i "linh kiện" -- src test docs BUSINESS_ARCHITECTURE_DECISIONS_V2.md UI_IMPLEMENTATION_DECISION_MATRIX.md UI_REFACTOR_SOURCE_OF_TRUTH.md UI_UX_ABP_GUIDE_V2.md` - returned existing audit/evidence references only; this fix did not introduce user-facing `Linh kiện` wording.

## Manual Smoke Checklist

Deferred/not run in this code pass:

- Open Sales Details for a confirmed order.
- Submit a partial payment with amount, date, method, reference, and note.
- Confirm success feedback, updated summary, and history row.
- Submit remaining amount and confirm status `Đã thanh toán`.
- Try overpayment and invalid amount/date/method and confirm friendly validation.
