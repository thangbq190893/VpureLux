# UAT Fix 02G - Sales Create Stock Availability Preview

## 1. Business reason

Sales Create should show whether the selected warehouse has enough component stock for the selected product and quantity before the draft order is saved. This prevents creating drafts that are already known to fail Sales confirmation because confirmation consumes inventory through published BOM component requirements.

## 2. Current BOM-component inventory model

Sales confirmation still issues inventory by published BOM components. Product stock selling without BOM is not implemented in this batch, and product inventory remains disabled by current policy.

The 02G preview therefore calculates availability only for products with a published BOM. No-BOM products keep the existing limitation message and are not treated as product-stock sellable.

## 3. Availability formula

For each BOM-backed product line:

```text
availableToSell = min(floor(componentAvailableQuantity / requiredQuantityPerProduct))
```

The selected warehouse is used to read current component inventory balances. If a required component has no stock item or no balance in the selected warehouse, its available quantity is treated as `0`, making it the limiting component when applicable.

## 4. Aggregate multi-line stock validation

The preview batches all selected Sales Create rows and aggregates component demand across the whole draft order. If multiple rows consume the same component, the total component demand is compared with warehouse stock.

Rows using a component with aggregate shortage show a stock warning even when an individual row might appear sufficient in isolation.

## 5. Files changed

- `src/VPureLux.Web/Pages/Sales/Create.cshtml`
- `src/VPureLux.Web/Pages/Sales/Create.cshtml.cs`
- `src/VPureLux.Web/Pages/Sales/Create.css`
- `src/VPureLux.Web/Pages/Sales/SalesCreateLines.js`
- `src/VPureLux.Web/Pages/Sales/SalesProductContext.js`
- `src/VPureLux.Domain.Shared/Localization/VPureLux/vi-VN.json`
- `test/VPureLux.Web.Tests/Pages/SalesPagesTests.cs`
- `docs/UAT_FIX_02G_SALES_CREATE_STOCK_AVAILABILITY_PREVIEW.md`

No Domain behavior, Application service, inventory posting, FIFO, DTO/API contract, database schema, migration, or index changes were made.

## 6. UI behavior

Sales Create now renders a compact per-row stock preview in the existing status column.

Supported states:

- `Có thể bán tại kho này: X`
- `Không đủ tồn kho cho số lượng đã nhập. Thiếu: <component code/name>`
- `Chưa có định mức công bố, chưa thể kiểm tra tồn kho theo vật tư.`

The product column remains wide/readable, row layout remains a compact table, and the existing small aligned buttons are preserved.

## 7. Save validation behavior

The browser refreshes stock availability on page load, warehouse change, product change, quantity change, row add, and row remove.

Save is blocked when the latest known BOM-component availability says requested quantity exceeds available stock. The row shows a stock warning and the global alert shows `Không đủ tồn kho cho một hoặc nhiều dòng hàng.`

Existing validations remain in place:

- product required
- quantity greater than zero
- actual price required when suggested price is missing
- override reason required when actual price differs from an existing suggested price
- no-BOM draft creation remains blocked by the current published-BOM limitation
- no raw exception/stack trace is shown for known validation failures

## 8. Manual smoke result

Manual smoke was run against `https://localhost:44325/Sales/Create` as `admin`.

Results:

- Page loaded with the availability endpoint and one availability target per row.
- BOM product `CB-123 - Combo 123` in warehouse `UAT_E2E_WH_20260620023951 - UAT WH 20260620023951` showed shortage: `Không đủ tồn kho cho số lượng đã nhập. Thiếu: V3 - Lõi 3`.
- Saving the shortage row stayed on Create, showed the global stock alert, and showed no raw exception.
- No-BOM product `CB-1234 - CB-1234` showed the existing no-BOM limitation and the stock preview message `Chưa có định mức công bố, chưa thể kiểm tra tồn kho theo vật tư.`
- Changing warehouse refreshed the row stock preview.
- Adding two rows with high quantities produced aggregate stock warnings on both rows.
- Sufficient-stock candidate `UAT_UI_20260622_1809_P - UAT UI Test Product 20260622` showed `Có thể bán tại kho này: 7`.
- Quantity `1` saved successfully and redirected to Sales Details.
- Two lines of the same sufficient-stock product, quantity `1` each, both showed available stock and saved successfully to Details with two displayed lines.

## 9. Tests run

```text
node --check src/VPureLux.Web/Pages/Sales/SalesProductContext.js
node --check src/VPureLux.Web/Pages/Sales/SalesCreateLines.js
Result: passed.

dotnet build VPureLux.slnx --no-restore -m:2
Result: passed, 1 existing Test SDK warning.

dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Sales" -m:1
Result: passed, 42/42.

dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build -m:1 --logger "console;verbosity=detailed"
Result: passed, 155/155.
```

## 10. Deferred items

- No-BOM product-stock selling.
- Product stock enablement.
- Stock reservation at Create.
