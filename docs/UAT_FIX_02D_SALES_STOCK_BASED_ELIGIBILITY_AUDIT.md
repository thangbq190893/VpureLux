# UAT Fix 02D - Sales Stock-Based Eligibility Audit

## 1. Business clarification

Sales order creation must not be blocked only because a product has no suggested selling price. A salesperson must be able to manually enter `ActualSellingPrice` when a suggested price is missing.

The clarified direction also says a product without a published BOM should not automatically be blocked at Sales Create if the product can be sold from stock. Stock availability should be enforced at the correct inventory stage, most likely confirmation/inventory issue, not only at draft creation.

This document is an audit/design note only. It does not implement behavior changes.

## 2. Current Create flow

Current Sales Create has three layers of eligibility behavior:

1. UI script: `SalesProductContext.js` marks any product with `hasPublishedBom !== true` as ineligible and blocks client submit through `validateAllRows`.
2. PageModel: `CreateModel.OnPostAsync` calls `ValidateLineEligibility()` before `_service.CreateAsync(Input)`. `TryAddLineEligibilityError()` adds a row error when `ProductContexts[productId].HasPublishedBom` is false or absent.
3. Application service: `SalesOrderAppService.CreateAsync()` loops `input.Lines` and calls `AddInputLineAsync()`. `AddInputLineAsync()` calls `EnsurePublishedBomAsync(product.Id)` before creating the line.

Suggested price behavior in Create is less strict:

- `AddInputLineAsync()` calls `_suggestedPrices.FindAtDateAsync(product.Id, order.OrderDate)`.
- That repository method returns nullable data.
- If no suggested price exists, `input.ActualSellingPrice` is required.
- If actual price is provided, the draft line can be created from the suggested-price perspective.

Current Create blocker summary:

- Missing suggested price alone: not an Application/Domain blocker if actual price is provided.
- Missing actual price when suggested price is missing: blocked by `COM_001` with data reason `Actual selling price is required when no suggested price exists.`
- Missing published BOM: blocked by UI/PageModel and by Application service `SALES_010`.

## 3. Current Confirm/FIFO flow

`SalesOrderAppService.ConfirmAsync()` confirms each draft line through `ConfirmLineAsync()`.

Current `ConfirmLineAsync()` behavior:

1. Loads active product with `EnsureActiveProductAsync(line.ProductId)`.
2. Loads published BOM with `EnsurePublishedBomAsync(line.ProductId)`.
3. Rejects the line if `line.BomVersionId != publishedBom.Id`.
4. Expands published BOM items into component requirements.
5. Ensures each component is active.
6. Calls `PostInventoryIssueAsync()` with component requirements.
7. `PostInventoryIssueAsync()` looks up stock items by `StockItemType.Component`.
8. FIFO allocation consumes component lots and applies component balance decreases.
9. Sales line confirmation snapshots store BOM/component snapshot data, cost, profit, and inventory transaction id.

Current confirmation is therefore BOM-component based. It does not currently issue finished product inventory by product stock item.

## 4. Suggested price rule

Suggested price is nullable in both contract and domain state:

- `CreateSalesOrderLineDto.ActualSellingPrice` is nullable input.
- `SalesOrderLine.SuggestedPriceVersionId` is nullable.
- `SalesOrderLine.SuggestedPriceSnapshot` is nullable.
- `SalesOrderLineDto.SuggestedPriceVersionId` and `SuggestedPriceSnapshot` are nullable.

`SalesOrderLine.SetActualSellingPrice()` requires override reason only when `SuggestedPriceSnapshot.HasValue && SuggestedPriceSnapshot.Value != actual`.

Answers:

- `ActualSellingPrice` can be provided without `SuggestedPrice`.
- If suggested price is null/missing, current domain rules do not require `OverrideReason`.
- If suggested price exists and actual price differs, current domain rule requires `OverrideReason`.
- If suggested price exists and actual price differs, `SalesOrderAppService.EnsureOverridePermissionAsync()` also requires `Sales.OverridePrice`; otherwise it throws `COM_003`.

Minimum safe change for draft creation with manual actual price:

- UI/PageModel: do not treat missing suggested price as ineligible.
- Application service: no pricing rule change appears necessary for missing suggested price when actual price is provided.
- Tests should explicitly cover create draft with no suggested price and manual actual price.

## 5. Published BOM rule

Published BOM is currently required at both draft creation and confirmation.

Current create-time requirements:

- `CreateModel.TryAddLineEligibilityError()` rejects products with no published BOM.
- `SalesProductContext.js` blocks submit for products with no published BOM.
- `SalesOrderAppService.AddInputLineAsync()` calls `EnsurePublishedBomAsync()` and passes `bom.Id` to `order.AddLine()`.
- `SalesOrderLine` constructor rejects empty `bomVersionId`.

Current confirm-time requirements:

- `ConfirmLineAsync()` calls `EnsurePublishedBomAsync(line.ProductId)`.
- If current published BOM id differs from `line.BomVersionId`, it throws `SALES_010`.
- Current cost/FIFO logic depends on BOM component requirements.

Minimum safe change for no-BOM draft creation:

- Not UI-only.
- Requires Application/Domain design because `SalesOrder.AddLine()` and `SalesOrderLine` currently require a non-empty `bomVersionId`.
- Could allow nullable `BomVersionId` for draft lines, but confirmation must then know whether to issue product stock or reject missing stock/BOM.

Minimum safe change for selling stocked products without published BOM:

- Requires Inventory confirm logic change to support direct product-stock issue.
- Requires enabling product stock items or otherwise deciding how product inventory becomes usable.
- Does not appear to require a new DB table because inventory transactions/lots/balances already reference generic `StockItemId`, and stock items already support `StockItemType.Product`.

## 6. Inventory stock model

Inventory stock is modeled through `StockItem`.

Key model facts:

- `StockItemType` includes both `Component = 1` and `Product = 2`.
- `StockItem` has `CatalogItemId`, `ItemType`, snapshots, status, and `IsInventoryEnabled`.
- `InventoryLot`, `InventoryBalance`, and `InventoryTransactionLine` reference `StockItemId`, not component id.
- FIFO allocation is generic over `StockItemId`.
- Receipts and issues are generic over `StockItemId`.

Current synchronization:

- `CatalogStockItemSynchronizationHandler` creates/synchronizes stock items for both components and products.
- `StockItemManager.GetOrCreateAsync()` creates new stock items with `isInventoryEnabled: itemType == StockItemType.Component`.
- Therefore component stock items are inventory-enabled by default.
- Product stock items exist, but are inventory-disabled by default.

Current tests confirm this policy:

- `InventoryDomainTests.StockItem_Should_Enforce_Component_And_Product_Policies()` expects product stock item disabled.
- `InventoryWorkflowTests.Should_Reject_Product_Inventory_Operations()` expects product receipt to fail with `INV_008`.

## 7. Product stock vs component stock capability

Does Inventory track stock for finished products, components, or both?

- Structurally both, because `StockItemType.Product` exists and stock tables use generic `StockItemId`.
- Operationally components only by default, because product stock items are created with `IsInventoryEnabled = false`.

Can a Product have stock balance independently from BOM?

- Structurally yes: a product has a `StockItemType.Product` stock item and balances/lots are keyed by stock item.
- Under current business rules/tests, product stock is disabled and inventory operations against product stock items are rejected.
- If product stock item inventory is enabled later, existing receipt/issue/FIFO code should be able to operate on it without new tables.

Does Sales confirmation currently issue inventory by ProductId or BOM component requirements?

- It issues by BOM component requirements.
- It looks up stock items using `StockItemType.Component` and each BOM component id.
- It does not currently look up `StockItemType.Product` for the sales product id.

## 8. Current exceptions and error codes

Published BOM:

- Method: `SalesOrderAppService.EnsurePublishedBomAsync(Guid productId)`
- Throws: `BusinessException(VPureLuxDomainErrorCodes.SalesBomMustBePublished)`
- Code: `SALES_010`
- Used by: `AddInputLineAsync()` during Create/AddLine and `ConfirmLineAsync()` during Confirm.

Actual price differs from suggested price and override reason is blank:

- Method: `SalesOrderLine.SetActualSellingPrice(decimal value, string? overrideReason)`
- Throws: `BusinessException(VPureLuxDomainErrorCodes.SalesOverrideReasonRequired)`
- Code: `SALES_009`

Actual price differs from suggested price without override permission:

- Method: `SalesOrderAppService.EnsureOverridePermissionAsync(decimal? suggested, decimal actual)`
- Throws: `BusinessException(VPureLuxDomainErrorCodes.AccessDenied)`
- Code: `COM_003`

Suggested price missing and actual price missing:

- Method: `SalesOrderAppService.AddInputLineAsync()`
- Throws: `BusinessException(VPureLuxDomainErrorCodes.ValidationFailed).WithData("Reason", "Actual selling price is required when no suggested price exists.")`
- Code: `COM_001`

Suggested price missing but actual price provided:

- No current pricing exception found.
- Draft creation can pass the pricing portion because `suggestedPrice` remains null and `ActualSellingPrice` is used.
- Published BOM may still block earlier/later in the same method.

Product stock item disabled:

- Method: `InventoryManager.EnsureWarehouseAndStockItemUsableAsync()`
- Throws: `BusinessException(VPureLuxDomainErrorCodes.StockItemInventoryDisabled)`
- Code: `INV_008`

Insufficient inventory:

- Method: `InventoryManager.AllocateFifoAsync()` or `InventoryBalance.ApplyMovement()`
- Throws: `BusinessException(VPureLuxDomainErrorCodes.InsufficientInventory)`
- Code: `INV_001`
- Sales wraps `INV_*` errors inside `SALES_006` in `PostInventoryIssueAsync()`.

## 9. Recommended rule changes

Recommended target rules:

1. Missing suggested product price should not block Sales Create if `ActualSellingPrice` is provided.
2. Missing suggested product price should not require override reason, because there is no suggested value to override.
3. If suggested price exists and actual price differs, keep current override reason and override permission behavior unless business explicitly changes it.
4. Draft creation should not require published BOM if the product is intended to be sold from finished-goods stock.
5. Confirmation should decide the inventory path:
   - If line has a valid published BOM snapshot/path, continue BOM component issue.
   - If line has no BOM but product stock is enabled and available, issue finished product stock.
   - If neither path is valid, block confirmation with a clear Sales-facing error.
6. Stock availability should be checked at confirmation/issue time, not at draft create time, to avoid stale inventory decisions.

Classification:

| Recommendation | Classification |
|---|---|
| Stop UI/PageModel from blocking missing suggested price | UI/PageModel-only change |
| Stop UI/PageModel from treating every no-BOM product as ineligible | UI/PageModel-only change for draft UX, but unsafe alone |
| Allow draft line without BOM id | Domain rule change + Application service change |
| Preserve override reason only when suggested exists and differs | Existing Domain rule; test coverage needed |
| Issue finished product stock when no BOM | Inventory confirm logic change + Application service change |
| Enable product stock operations | Domain/Application service change; maybe business decision |
| Add product stock DB tables | DB schema/migration not needed based on current generic stock model |

## 10. Minimum safe implementation batches

### Batch 02D.1 - Pricing-only create relaxation

Goal: prove missing suggested price does not block draft creation when actual price is supplied.

Likely changes:

- Tests for product with published BOM, no product suggested price, manual actual price.
- Verify `OverrideReason` optional when suggested price is null.
- UI text can keep showing `NoSuggestedPrice` but must allow manual price.

Expected classification:

- Mostly tests and possibly UI/PageModel validation if any pricing-based block is found.
- No DB/schema change.
- No DTO/API change expected.

### Batch 02D.2 - No-BOM draft design

Goal: allow draft creation for products that do not have a published BOM.

Likely changes:

- Application: `AddInputLineAsync()` must not unconditionally call `EnsurePublishedBomAsync()`.
- Domain: `SalesOrder.AddLine()` / `SalesOrderLine` must allow nullable `BomVersionId` for draft lines.
- Web: Sales Create context should show no-BOM as a warning/status, not a hard create blocker.
- Tests: create draft no-BOM line with manual actual price.

Expected classification:

- UI/PageModel change.
- Application service change.
- Domain rule change.
- No DB/schema change likely because `SalesOrderLine.BomVersionId` and DTO are already nullable, but the EF configuration should be verified before implementation.
- Needs business decision on whether all no-BOM products are draft-allowed or only product-stock-enabled products.

### Batch 02D.3 - Product stock enablement decision

Goal: define how finished-product stock becomes inventory-enabled.

Options:

- Enable product stock by default for new products.
- Add an explicit product inventory enable workflow.
- Keep product stock disabled unless an inventory role enables it.

Expected classification:

- Needs business decision.
- Domain/Application change.
- Existing tests intentionally expect product inventory disabled, so tests must be updated if policy changes.
- No DB/schema change expected.

### Batch 02D.4 - Confirm no-BOM stocked-product sale

Goal: confirm a no-BOM product line by issuing product stock through FIFO.

Likely changes:

- `ConfirmLineAsync()` chooses BOM-component issue when `BomVersionId` exists, otherwise direct product-stock issue.
- Direct product issue looks up `StockItemType.Product` by `line.ProductId`.
- `PostInventoryIssueAsync()` may need a more general requirement type, not only `(Component Component, decimal Quantity)`.
- Sales line snapshot must handle no BOM/component snapshot items and product stock cost.

Expected classification:

- Application service change.
- Inventory confirm logic change.
- Domain rule change may be needed for snapshots/cost semantics.
- No DB/schema change expected, but verify nullable `BomVersionId` persistence and Details rendering.

## 11. Risk assessment

Low risk:

- Allowing missing suggested price when actual price is provided, because current contracts/domain already support nullable suggested price and actual price is the required sale price.
- Keeping override reason required only when suggested exists and differs, because that is current domain behavior.

Medium risk:

- Allowing no-BOM draft lines, because current domain constructor requires a non-empty BOM id and current Web tests assert no-BOM Create rejection.
- UI language must distinguish warning/status from hard ineligibility.

High risk:

- Confirming no-BOM stocked products, because current confirmation, cost snapshot, idempotency hash, BOM snapshot, and Details display are BOM/component-based.
- Product inventory enablement changes existing tested policy (`Product` stock item disabled by default).

DB/schema risk:

- No new inventory tables appear necessary because stock is generic by `StockItemId`.
- Potential schema review needed if EF configuration currently makes `SalesOrderLine.BomVersionId` required despite nullable domain/DTO property.

## 12. Tests needed

Pricing/manual actual price:

- Create draft with published BOM, no suggested price, manual actual price succeeds.
- Create draft with published BOM, no suggested price, no actual price fails with friendly validation.
- Suggested price exists and actual differs without override reason still fails with `SALES_009`.
- Suggested price missing and manual actual price does not require override reason.
- Override permission behavior remains unchanged when suggested exists and differs.

No-BOM draft:

- Sales Create PageModel no longer rejects no-BOM product at draft create if business approves.
- `SalesOrderAppService.CreateAsync()` creates draft line with nullable `BomVersionId`.
- Details/Edit render no-BOM draft line without crashing.

Product stock:

- Product stock item policy test updated according to business decision.
- Product stock receipt/adjustment can create lots and balances if product inventory is enabled.
- Product stock issue uses FIFO and prevents negative stock.

Confirmation:

- BOM product still confirms through component FIFO exactly as today.
- No-BOM stocked product confirms by issuing `StockItemType.Product`.
- No-BOM product with no product stock fails at confirm with Sales-facing inventory error.
- Mixed order with BOM line and no-BOM product-stock line confirms both paths and calculates total cost/profit.
- Confirmation idempotency remains stable for both BOM and product-stock issue paths.

Regression:

- Existing Sales Edit/Details behavior for BOM-backed products remains unchanged.
- Existing inventory component receipt/issue/adjustment tests remain unchanged unless product-stock policy is intentionally broadened.
