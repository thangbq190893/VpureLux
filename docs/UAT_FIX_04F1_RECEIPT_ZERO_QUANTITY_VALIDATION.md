# UAT Fix 04F.1 - Receipt Zero Quantity Validation

## Issue

UAT Snapshot 04A Pass 2 reported `G-val-zero` as a MEDIUM failure for Inventory Receipt validation. A receipt line with zero quantity was not blocked clearly with a friendly field-level message before posting.

## UAT Evidence

- Module: Inventory Receipt
- Scenario: `G-val-zero`
- Expected: zero quantity is blocked before posting with friendly validation
- Actual: zero quantity validation was weak or unclear
- Reference: `docs/UAT_SNAPSHOT_04A_PASS2_FULL_E2E_TEST.md`

## Root Cause

Receipt line quantity is a non-nullable decimal DTO property. Browser form binding can turn a blank or invalid quantity into `0`, and the page relied on default model validation/range messages instead of a receipt-specific Vietnamese field message. The application service also resolved blank receipt LotNo values before any explicit receipt-line quantity guard, so the write path did not clearly guarantee that invalid receipt quantities were rejected before LotNo generation.

## Fix

- Added explicit Receipt PageModel validation for each receipt line quantity.
- Added the friendly Vietnamese validation message: `Số lượng nhập phải lớn hơn 0.`
- Attached the error to the affected field key, for example `Input.Lines[0].Quantity`.
- Changed the receipt quantity input to a numeric positive input with `min="0.0001"` and `step="0.0001"`.
- Preserved posted quantity text from `ModelState` when rendering the page after validation failure.
- Added application-service validation in `PostReceiptAsync` before idempotency/hash processing and before blank LotNo resolution.

## Validation Behavior

- Zero quantity is blocked before posting.
- Negative quantity is blocked before posting.
- Missing quantity is blocked before posting.
- The Receipt page stays open with user input preserved where possible.
- The friendly validation message appears next to the quantity field and in the validation summary.
- No `InventoryTransaction` is created for invalid zero/non-positive quantity.
- No `InventoryLot` is created and no automatic LotNo sequence is generated for invalid zero/non-positive quantity.
- Valid positive receipt still posts successfully.
- Valid blank LotNo behavior is unchanged: a LotNo is still generated when quantity is valid.

## Intentionally Not Changed

- No FIFO or costing changes.
- No database schema or migration changes.
- No change to valid receipt posting behavior.
- No change to valid LotNo auto-generation.
- No Issue or Adjustment behavior changes.

## Tests Run

- `dotnet build VPureLux.slnx --no-restore -m:2` - passed.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Inventory" -m:1` - passed, 63 tests.
- `dotnet test test/VPureLux.EntityFrameworkCore.Tests/VPureLux.EntityFrameworkCore.Tests.csproj --no-build --filter "FullyQualifiedName~Inventory" -m:1` - passed, 24 tests.
- `dotnet test test/VPureLux.Application.Tests/VPureLux.Application.Tests.csproj --no-build --filter "FullyQualifiedName~Inventory" -m:1` - no matching tests.
- `git diff --check` - passed, with line-ending normalization warnings only.
- `git grep -n -i "linh kiện" -- src test docs BUSINESS_ARCHITECTURE_DECISIONS_V2.md UI_IMPLEMENTATION_DECISION_MATRIX.md UI_REFACTOR_SOURCE_OF_TRUTH.md UI_UX_ABP_GUIDE_V2.md` - returned existing audit/evidence references and prior fix-doc terminology-check text only; this fix did not introduce user-facing `Linh kiện` wording.

## Manual Smoke Checklist

Deferred/not run in this code pass:

- Open `/Inventory/Receipt`.
- Try posting quantity `0` and confirm the friendly Vietnamese validation message.
- Try posting a negative quantity and confirm the same message.
- Try posting a blank quantity and confirm the same message.
- Confirm no lot is created for invalid attempts.
- Post a valid positive quantity with blank LotNo and confirm LotNo is auto-generated.
