# UAT Fix 02E - Sales Details data consistency and confirm feedback

**Date:** 2026-06-29
**Scope:** Sales Details / Confirm Web UX and display consistency.

---

## 1. Root cause

Draft sales orders store `CustomerId` when created, but customer snapshots are applied during confirm. Details rendered only `CustomerCodeSnapshot - CustomerNameSnapshot`, so draft orders displayed `-` even though `CustomerId` was valid.

Draft order totals are intentionally finalized in `SalesOrder.Confirm()`. Line quantities and actual selling prices exist on drafts, but `TotalRevenueAmount`, `TotalCostAmount`, and `TotalProfitAmount` remain zero until confirmation. Showing those zero totals under generic labels made draft Details look incorrect.

Confirm already posted to `OnPostConfirmAsync`, but confirm failures were only represented through model validation. The page did not show a dedicated visible alert, and inventory failures did not include the underlying inventory reason.

---

## 2. Customer display fix

Details now resolves customer display in the PageModel:

- confirmed orders use the stored customer snapshot,
- draft orders with blank snapshots resolve `CustomerId` through the existing customer app service,
- if customer display cannot be resolved because of authorization or missing data, the page falls back to the localized unavailable message.

No Sales DTO/API contract or database schema change was needed.

---

## 3. Draft totals display decision

Domain totals remain confirmation totals only.

Details now shows:

- `Doanh thu dự kiến` for draft estimated revenue from `SUM(Quantity * ActualSellingPrice)`,
- `Doanh thu đã xác nhận`,
- `Giá vốn đã xác nhận`,
- `Lợi nhuận đã xác nhận`,
- a draft note explaining that cost and profit are calculated after confirmation.

No draft cost/profit is invented because FIFO cost is only known during confirm.

---

## 4. Confirm action behavior

The Confirm form still posts to `OnPostConfirmAsync` with the existing idempotency key and anti-forgery form behavior.

On success:

- the order is confirmed by the existing application service,
- Details redirects back to the order,
- status updates to confirmed,
- a localized success alert is visible and the existing ABP notification hook remains.

On failure:

- Details remains on the order,
- status stays Draft,
- a visible localized error alert is shown,
- no raw exception or stack trace is shown.

---

## 5. Known confirm error handling

`OnPostConfirmAsync` now handles targeted known confirm errors only, including:

- inventory validation failure,
- missing/stale published BOM,
- inactive component/customer/customer group/warehouse,
- missing stock item or unusable stock item,
- duplicate confirmation key,
- concurrent modification,
- access denied,
- validation failure.

Inventory validation failures now append the localized underlying inventory reason when present, for example:

```text
Kiểm tra tồn kho cho đơn bán hàng thất bại. Không đủ tồn kho.
```

---

## 6. Manual smoke result

Passed against the local app at `https://localhost:44325` using the seeded admin login.

- Created a Sales order with 2 valid product rows.
- Redirected to Details.
- Details showed the selected customer instead of `-`.
- Details showed both lines with correct product labels, quantities, suggested prices, and actual prices.
- Draft header showed estimated revenue `405.000 ₫`.
- Confirmed revenue/cost/profit remained `0 ₫` with labels indicating confirmed totals.
- Draft note explained that cost and profit are calculated after confirmation.
- Clicking Confirm invoked the confirm flow.
- Confirm was blocked by inventory for the selected runtime data.
- The page showed the visible friendly error: `Kiểm tra tồn kho cho đơn bán hàng thất bại. Không đủ tồn kho.`
- Status remained `Nháp`.
- Refresh kept the order state consistent as Draft.

Confirm success was covered by Web test using seeded in-memory test data with sufficient stock.

---

## 7. Tests run

```text
dotnet build VPureLux.slnx --no-restore -m:2
  -> Build succeeded, 3 warnings, 0 errors

dotnet build VPureLux.slnx --no-restore -m:2
  -> Build succeeded, 1 warning, 0 errors

dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Sales" -m:1
  -> First run: Failed 1 brittle source assertion in the new confirm-handler test

dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Sales" -m:1
  -> Passed: 34, Failed: 0

dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build -m:1
  -> Passed: 147, Failed: 0
```

---

## 8. Deferred items

- Optional PRG-style redirect for confirm failure if product owners want refresh to clear the one-time alert.
- Dedicated E2E seed data that guarantees both confirm-success and confirm-failure browser paths in the same runtime environment.
- Future no-BOM stock-based confirm behavior remains out of scope for this fix.
