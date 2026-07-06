# UAT Fix 03J.4 - Inventory Adjustment Reason Policy

## Reason

03J.3 changed `/Inventory/Adjustment` to a count-first workflow, and 03J.3.1 blocked mixed increase/decrease submissions until atomic count document support is designed. 03J.4 tightens the reason experience so adjustment entries are clearer for audit and Ledger review without changing posting, FIFO, costing, or schema behavior.

## Scope

- Improve `/Inventory/Adjustment` reason input and validation.
- Keep the existing persisted `Reason` field as the only saved reason/note data.
- Add UI-only category/detail inputs on the Web PageModel and Razor page.
- Compose the final posted `Reason` before calling the existing adjustment app service.

## Final Policy: Persisted Reason Only

No persisted `Note` column or `ReasonCategory` field was added. The page collects category/detail only as input helpers, then folds them into `PostAdjustmentDto.Reason`.

## UI-Only Category And Detail Behavior

The page now asks for a required reason category:

- Kiểm kê lệch tồn
- Hàng hỏng
- Hàng mất
- Sai thao tác nhập/xuất trước đó
- Điều chỉnh tồn đầu kỳ
- Khác

The page also shows a detail reason textarea. Detail is required only when the selected category is `Khác`; for other categories it is optional but available for audit context.

## Reason Composition Examples

- Category `Kiểm kê lệch tồn`, detail `Kiểm kê cuối tháng 07` posts reason `Kiểm kê lệch tồn - Kiểm kê cuối tháng 07`.
- Category `Hàng hỏng`, blank detail posts reason `Hàng hỏng`.
- Category `Khác`, detail `Đối chiếu lại thẻ kho` posts reason `Khác - Đối chiếu lại thẻ kho`.

## Validation Rules

- Warehouse is required.
- At least one count row is kept by the page.
- Vật tư is required per row.
- Counted quantity is required and cannot be negative.
- All-zero delta submission is blocked.
- Mixed positive/negative delta submission remains blocked.
- Reason category is required.
- Detail reason is required when category is `Khác`.
- Positive delta requires lot number, received date, and unit cost.
- Positive-delta unit cost must be greater than zero.
- Backend `BusinessException` handling remains for FIFO/stock errors.

## Intentionally Not Changed

- No Domain rule changes.
- No Application posting behavior changes.
- No DB/schema/migration/index changes.
- No persisted note/category fields.
- No FIFO/costing changes.
- No Ledger, Receipt, Issue, Sales, or BOM behavior changes.

## Deferred Decisions

- Whether a future count document/session should persist category and note separately.
- Whether reason categories should become configurable master data.
- Whether all-zero count rows should be stored for audit.
- Whether mixed increase/decrease count corrections should post atomically as one count document.

## Tests Run

- `git diff --check` - passed with existing CRLF normalization warnings.
- `dotnet build VPureLux.slnx --no-restore -m:2` - passed with the existing Microsoft.NET.Test.Sdk generated entry-point warning.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Inventory" -m:1` - passed, 52/52.
- Repository grep for the legacy Vietnamese component wording - passed, no matches.

## Manual Smoke Checklist Deferred

- Open `/Inventory/Adjustment`.
- Confirm reason category dropdown and detail textarea render.
- Submit without category and verify friendly validation.
- Select `Khác` without detail and verify friendly validation.
- Submit non-`Khác` category without detail and verify the Ledger reason is the category.
- Submit category with detail and verify the Ledger reason is `category - detail`.
- Confirm all-zero and mixed-direction guards still block.
