# UAT Fix 03J.5 - Inventory Source Reference

## Reason

03J.2 made `/Inventory/Ledger` a line-level trace view, but the source/reference cell still exposed raw reference data or a blank fallback. 03J.5 makes the source easier to read so users can understand where each stock movement came from without changing posting, FIFO, cost, or stored data.

## Scope

- Improve `/Inventory/Ledger` source/reference display only.
- Use existing fields: transaction type, transaction id, `ReferenceType`, `ReferenceId`, `BomVersionId`, and reason.
- Keep Ledger read-only.
- Keep the 03J.2 trace columns intact.

## Source Labels Added

- Manual receipt without a known reference: `Nhập kho thủ công`.
- Manual issue without a known reference: `Xuất kho thủ công`.
- Adjustment increase/decrease: `Điều chỉnh kho`.
- Sales-origin issue with `ReferenceType = SalesOrderLine`: `Đơn bán hàng`.
- BOM/manufacturing-origin movement with `BomVersionId` or `AssemblyIssue`: `BOM / sản xuất`.
- Unsupported reference type: `Nguồn không xác định`, with the raw reference type/id still shown as detail.

## Source Links

- Added a safe BOM link when `BomVersionId` is present: `Mở BOM` links to `/Bom/Details/{id}`.
- Sales links are deferred because the current Ledger DTO has only the sales order line id, not the parent sales order id needed for the existing `/Sales/Details/{id}` route.
- Unknown/unsupported sources render text only; no guessed links are emitted.

## Filters Changed

The existing `SourceReference` filter now searches the friendly source label, source detail text, transaction id, raw `ReferenceType`, raw `ReferenceId`, and `BomVersionId`. Existing warehouse, Vật tư, transaction type, and date filters are preserved.

## Intentionally Not Changed

- No Inventory posting logic changed.
- No FIFO/costing behavior changed.
- No Domain rules changed.
- No DB/schema/migration/index changed.
- No new document number columns added.
- No Sales/BOM/Receipt/Issue/Adjustment behavior changed.
- No backend identifiers were renamed.

## Deferred Source/Document Decisions

- Add a stable sales-order source link when the Ledger read model exposes parent sales order id or a friendly sales document number.
- Resolve purchase/receipt document labels if purchase documents are introduced.
- Resolve FIFO decrease lot labels in Ledger without adding unsafe joins to this batch.
- Add user/actor labels when the read model exposes a user display value.

## Tests Run

- `git diff --check` - passed with existing CRLF normalization warnings.
- `dotnet build VPureLux.slnx --no-restore -m:2` - passed with the existing Microsoft.NET.Test.Sdk generated entry-point warning.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Inventory" -m:1` - passed, 55/55.
- Repository grep for the legacy Vietnamese component wording - passed, no matches.

## Manual Smoke Checklist Deferred

- Open `/Inventory/Ledger`.
- Verify manual receipt rows show `Nhập kho thủ công`.
- Verify manual issue rows show `Xuất kho thủ công`.
- Verify adjustment rows show `Điều chỉnh kho`.
- Verify sales-origin rows show `Đơn bán hàng`.
- Verify rows with `BomVersionId` show a `Mở BOM` link.
- Filter by a friendly source label such as `Nhập kho thủ công`.
- Confirm unknown source rows show fallback text and no broken source link.
