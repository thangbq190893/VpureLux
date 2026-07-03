# UAT Fix 03J.3 - Inventory Adjustment Count-First UX

## Reason

03J.1 found that `Ghi nhận điều chỉnh kho` was manual delta entry: users had to choose increase or decrease first and then type the adjustment quantity. That is safe mechanically, but it is not the natural stock-count workflow. Normal users should enter the physical counted quantity and let the system calculate the delta.

## Old behavior

- User selected warehouse.
- User selected adjustment direction: increase or decrease.
- Increase rows collected Vật tư, quantity, lot number, received date, and unit cost.
- Decrease rows collected Vật tư and quantity.
- The page posted directly to the existing adjustment increase/decrease mechanics.

## New count-first behavior

- `/Inventory/Adjustment` now shows one count-first line table by default.
- Each row shows Vật tư, current system quantity, counted physical quantity, calculated delta, direction, and positive-delta valuation fields.
- Current quantity is loaded from the existing inventory balance query. If no balance row exists for the selected warehouse/Vật tư, the UI and POST transformation treat it as zero on hand.
- Delta is calculated as counted physical quantity minus current system quantity.
- Direction is display-only: positive delta is adjustment increase, negative delta is adjustment decrease, and zero delta is no change.

## Mapping rules

- Positive delta maps to the existing `AdjustmentIncrease` posting path.
- Negative delta maps to the existing `AdjustmentDecrease` posting path.
- Zero delta rows are ignored.
- If all rows are zero delta, submission is blocked with a friendly validation message.
- Mixed positive and negative count rows are split by the Web PageModel into the existing increase/decrease posting calls with distinct idempotency keys.

## Unchanged FIFO, cost, and posting behavior

- No Domain rules changed.
- No Application posting behavior changed.
- No DB/schema/migration/index changed.
- Negative deltas continue to use the existing FIFO decrease behavior.
- Positive deltas continue to use the existing valued-lot increase behavior and therefore require lot number, received date, and unit cost.
- Receipt and issue behavior are unchanged.

## Validation behavior

Friendly Web/PageModel validation now covers:

- Missing warehouse.
- Missing Vật tư.
- Missing counted quantity.
- Negative counted quantity.
- Missing reason.
- All rows with zero delta.
- Positive delta missing lot number.
- Positive delta missing received date or invalid date format.
- Positive delta missing unit cost or unit cost not greater than zero.

Existing backend `BusinessException` handling remains in place, including insufficient inventory for decrease/FIFO failures.

## Deferred decisions

- Whether to keep a separate advanced manual increase/decrease mode. This batch replaces the visible normal flow with count-first UX.
- Whether count adjustment should become a first-class count document/session with approvals.
- Whether zero-delta rows should be stored for count audit.
- Whether positive count deltas should default unit cost from last cost, weighted average, or another policy. This batch keeps manual unit cost for positive deltas.
- Whether negative count deltas should preview exact FIFO lot allocations before posting.

## Tests Run

- `dotnet build VPureLux.slnx --no-restore -m:2` - passed with the existing Microsoft.NET.Test.Sdk generated entry-point warning.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Inventory" -m:1` - passed, 46/46.
- Repository grep for the legacy Vietnamese component wording - passed, no matches.

## Manual Smoke Checklist Deferred

- Open `/Inventory/Adjustment`.
- Select warehouse.
- Add count rows and select Vật tư.
- Verify current quantity updates from balance data.
- Enter counted quantity lower than current and verify direction becomes decrease.
- Enter counted quantity higher than current and verify direction becomes increase and valuation fields are shown.
- Submit positive-delta row with lot/date/unit cost.
- Submit negative-delta row where stock exists and verify FIFO behavior remains backend-driven.
- Try all-zero rows and verify friendly validation.
