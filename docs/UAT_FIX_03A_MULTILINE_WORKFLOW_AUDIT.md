# UAT Fix 03A - Multi-line workflow audit

## 1. Executive summary

This audit reviewed the current multi-line workflow shape across Sales, Inventory, BOM, Pricing, Catalog, and Audit. No production code or tests were changed.

The main result is favorable: the core operational documents that naturally require multiple lines already have backend support and, in most cases, UI support:

- Sales Create supports multiple order lines and posts `CreateSalesOrderDto.Lines`.
- Sales Edit supports existing multiple lines plus add/remove line handlers.
- Sales Details and Confirm process all order lines.
- BOM Create/Edit support multiple BOM items.
- Inventory Receipt, Issue, and Adjustment support multiple posting lines.

The highest-value next work is not a DB/schema change. It is a Sales/Edit/Create/Details consistency pass and targeted UX regression coverage to ensure all multi-line behaviors stay aligned after recent UAT fixes.

## 2. Current frozen business rules

- Sales order creation may contain multiple product lines.
- Sales confirmation currently consumes BOM component requirements, not finished-product stock.
- A product sale currently depends on a published BOM in the Sales application service confirmation path.
- Missing suggested selling price must not block Sales Create when an actual selling price is entered.
- If a suggested price exists and actual selling price differs, override reason is required.
- Product-stock / no-BOM selling is intentionally deferred and not implemented in this audit.
- Inventory postings are stock-item based and support multiple transaction lines.
- Catalog product/component CRUD is one catalog entity per screen.
- Pricing creates one suggested price version for one component or one product at a time.
- Audit screens are inquiry/export screens, not business document entry screens.

## 3. Audit matrix table

| Module | Page/Workflow | Current UI cardinality | Expected cardinality | DTO support | Domain support | DB support | AppService processing | Details/Edit/Confirm consistency | Classification | Priority | Recommended fix batch | Recommended agent | Notes |
|---|---|---:|---:|---|---|---|---|---|---|---|---|---|---|
| Sales | Create order | Multi-line | Multi-line | `CreateSalesOrderDto.Lines` | `SalesOrder.Lines` | Sales lines + BOM snapshot items | Loops input lines | Redirects to Details after save; uses product context and stock preview | OK_MULTI_LINE | High coverage guard | 03B regression coverage | Codex | UI has template/reindex script and row-level validation. |
| Sales | Edit order | Multi-line existing list plus single add-line form | Multi-line | `AddLineAsync` / `RemoveLineAsync` per line | Add/remove/renumber supported | Sales lines | Per-line add/remove/update handlers | Consistent enough functionally; UX is less rich than Create | UI_SINGLE_BACKEND_MULTI | High polish | 03F Sales Edit consistency | Codex | Backend is multi-line; UI can manage lines but not with the same dynamic table affordance as Create. |
| Sales | Details / Confirm | Multi-line display | Multi-line | `ConfirmSalesOrderDto` is order-level | Confirms order with all lines | Sales lines and snapshots | Loops order lines, posts consolidated component issues | Details renders all lines and confirm feedback | OK_MULTI_LINE | High coverage guard | 03B/03E regression coverage | Codex | Confirm is component/BOM based today. |
| Sales | Customer history | Multi-row history | Multi-line summaries | Query DTOs | N/A | Sales order lines | Query projection | Shows product labels/history summaries | OK_MULTI_LINE | Medium | 03C history polish | Codex | Not an entry workflow. |
| BOM | Create version | Multi-item | Multi-item | `CreateBomVersionDto.Items` | `BomVersion.Items` | `AppBomItems` | Loops items | Details displays all items | OK_MULTI_LINE | Medium coverage guard | 03D BOM regression coverage | Codex | UI uses `BomItems.js` reindexing. |
| BOM | Edit version | Multi-item | Multi-item | `UpdateBomVersionDto.Items` | Add/remove/update items | `AppBomItems` | Reconciles input items | Details displays all items | OK_MULTI_LINE | Medium coverage guard | 03D BOM regression coverage | Codex | Existing update preserves component selections/quantities. |
| BOM | Publish / Clone | Whole BOM version | Whole BOM version | Clone/publish DTOs | Version-level | Items retained | Validates all items/components | Clone creates a new multi-item draft | OK_MULTI_LINE | Low | 03D optional polish | Codex | No line-entry gap found. |
| Inventory | Receipt | Multi-line | Multi-line | `PostReceiptDto.Lines` | `InventoryTransaction.Lines` | Transaction lines + lots/balances | Loops receipt lines | Ledger/balance output is stock-item based | OK_MULTI_LINE | Medium coverage guard | 03E Inventory posting coverage | Codex | UI has add/remove/reindex and idempotency key. |
| Inventory | Issue | Multi-line | Multi-line | `PostIssueDto.Lines` | `InventoryTransaction.Lines` | Transaction lines + allocations | Consolidates issue lines | Ledger/balance output is stock-item based | OK_MULTI_LINE | Medium coverage guard | 03E Inventory posting coverage | Codex | FIFO issue logic supports multiple stock items. |
| Inventory | Adjustment | Multi-line increase/decrease | Multi-line | `PostAdjustmentDto.IncreaseLines` / `DecreaseLines` | `InventoryTransaction.Lines` | Transaction lines + lots/allocations | Routes increase/decrease and loops lines | Ledger/balance output is stock-item based | OK_MULTI_LINE | Medium coverage guard | 03E Inventory posting coverage | Codex | UI separates increase/decrease line containers. |
| Inventory | Warehouses | Single warehouse CRUD | Single entity | Warehouse DTOs | Warehouse aggregate | Warehouse table | Single warehouse create/update | Not a line workflow | INTENTIONAL_SINGLE | Low | None | N/A | Master-data screen. |
| Inventory | Balances / Lots / Ledger | Inquiry tables | Inquiry tables | Query DTOs | N/A | Balances/lots/transactions | Query projections | Multi-row display only | OUT_OF_SCOPE_FOR_NOW | Low | None | N/A | Not entry workflows. |
| Pricing | Component suggested price create | Single component price version | Single version unless batch pricing is approved | Single create DTO | Price version aggregate | Price version table | One component per create | History per component | INTENTIONAL_SINGLE | Medium decision | 03G Pricing batch decision | Product owner + Codex | Batch price entry would be a new business feature. |
| Pricing | Product suggested price create | Single product price version | Single version unless batch pricing is approved | Single create DTO | Price version aggregate | Price version table | One product per create | History/context per product | INTENTIONAL_SINGLE | Medium decision | 03G Pricing batch decision | Product owner + Codex | Current product pricing context already lists many products. |
| Pricing | Pricing index/context | Multi-row inquiry | Multi-row inquiry | Context DTO list | N/A | Price/BOM reads | Batch lookup maps | Read-only consistency OK | OK_MULTI_LINE | Low | None | N/A | Read model was optimized in Batch 18. |
| Catalog | Product create/edit/image | Single product | Single entity | Product DTOs | Product aggregate | Product table/image fields | One product per operation | Details per product | INTENTIONAL_SINGLE | Low | None | N/A | Bulk catalog entry is separate future feature. |
| Catalog | Component create/edit/image | Single component | Single entity | Component DTOs | Component aggregate | Component table/image fields | One component per operation | Details per component | INTENTIONAL_SINGLE | Low | None | N/A | Bulk component entry is separate future feature. |
| Catalog | Product/component index | Multi-row list | Multi-row list | List DTOs | N/A | Catalog tables | Query list | Actions per entity | OK_MULTI_LINE | Low | None | N/A | Index list cardinality is correct. |
| Audit | Index/details/reports/export | Inquiry/export | Inquiry/export | Audit list/export DTOs | Audit log aggregate | Audit log table | Query/export only | Details show one event | OUT_OF_SCOPE_FOR_NOW | Low | None | N/A | Not a line-entry workflow. |

## 4. Module-by-module findings

### Sales

Sales is the most important module for follow-up because it is both multi-line and actively changing through UAT fixes.

Observed support:

- Contracts expose `CreateSalesOrderDto.Lines` and `SalesOrderDto.Lines`.
- Domain stores a private line collection and exposes `SalesOrder.Lines`.
- Application `CreateAsync` loops through input lines.
- Application `ConfirmAsync` loops through order lines and creates per-line BOM snapshots.
- Inventory issue during confirm is consolidated from BOM component requirements.
- Web Create uses dynamic rows, row templates, reindexing, product context, and stock availability preview.
- Web Details renders all lines and all BOM snapshot items.
- Web Edit renders all existing lines and supports add/remove handlers.

Finding:

- Sales Edit is functionally multi-line but has weaker UX parity with Sales Create. It uses a single add-line form and per-line postbacks rather than the richer dynamic table flow. This is not a schema or backend gap, but it is the most visible consistency issue.

### BOM

BOM is multi-line through the stack.

Observed support:

- Create/Update DTOs have `Items`.
- `BomVersion` owns `BomItem` children.
- EF maps items to `AppBomItems`.
- App service create/update loops/reconciles items.
- Razor Create/Edit loop over `Model.Items`.
- `BomItems.js` adds/removes/reindexes rows.
- Details displays all BOM items.

Finding:

- No core multi-line gap found. Keep regression tests focused on add/remove/reindex, preservation after validation errors, and publish after edited item sets.

### Inventory

Inventory posting is multi-line through the stack.

Observed support:

- Receipt DTO has `Lines`.
- Issue DTO has `Lines`.
- Adjustment DTO has `IncreaseLines` and `DecreaseLines`.
- `InventoryTransaction` owns multiple lines.
- EF maps transaction lines and allocations.
- App service loops receipt/increase lines and consolidates issue/decrease lines.
- Receipt, Issue, and Adjustment pages render multiple line rows and use `Posting.js` reindexing.

Finding:

- No core multi-line gap found. Product stock exists at stock item model level, but current Sales confirmation uses component stock requirements from BOM. Product-stock selling remains a separate business decision and implementation batch.

### Pricing

Pricing version entry is intentionally single-item.

Observed support:

- Component price creation is for one component.
- Product price creation is for one product.
- History pages are per component/product.
- Pricing index and product context lookup are multi-row read models.

Finding:

- Batch pricing entry could be useful, but it is not currently implied by DTO/domain contracts. Treat as a business decision before implementation.

### Catalog

Catalog create/edit/image management is intentionally single-entity.

Observed support:

- Product and component create/edit pages operate on one entity.
- Image upload/replace/remove is per entity.
- Index pages show multi-row lists and per-row actions.

Finding:

- No multi-line document-entry expectation. Bulk import/edit would be a future feature, not a UAT consistency bug.

### Audit

Audit screens are not document-entry workflows.

Observed support:

- Index is a filtered multi-row audit log.
- Details shows one audit event.
- Export is an export command.

Finding:

- Out of scope for multi-line entry behavior.

## 5. High-priority UX gaps

1. Sales Edit parity with Sales Create.
   - Classification: `UI_SINGLE_BACKEND_MULTI`.
   - Recommended batch: 03F.
   - Recommended agent: Codex.
   - Rationale: Backend supports line add/remove and existing UI works, but users moving from Create to Edit do not get the same dynamic row experience, inline stock/suggested-price context, and aggregate feedback pattern.

2. Sales multi-line regression coverage.
   - Classification: `OK_MULTI_LINE` with high guard priority.
   - Recommended batch: 03B.
   - Recommended agent: Codex.
   - Rationale: Create, Details, Confirm, and stock preview are now line-sensitive. Guard tests should prevent regressions in row reindexing, validation binding, and all-line rendering.

## 6. Medium-priority polish items

- Add explicit tests for BOM Create/Edit row add/remove/reindex and validation error preservation.
- Add explicit tests for Inventory posting row add/remove/reindex on Receipt, Issue, and Adjustment.
- Review Sales CustomerHistory labels/summaries after more real multi-line UAT data is available.
- Review Pricing batch entry only after product owner confirms that one-by-one price versioning is too slow for real operations.

## 7. Confirmed intentional single-item screens

- Catalog Product Create/Edit/Image.
- Catalog Component Create/Edit/Image.
- Inventory Warehouse Create/Edit.
- Pricing Component Suggested Selling Price Create.
- Pricing Product Suggested Price Create.
- Audit Details.
- Audit Export command.

These are not multi-line defects under the current business rules.

## 8. Deferred business decisions

- Product-stock/no-BOM selling at Sales Confirm.
- Whether product StockItems should be inventory-enabled by default.
- Whether Sales should issue finished-product stock, BOM components, or support both by selling mode.
- Whether stock reservation should occur at Sales Create, Draft, Confirm, or Inventory Issue.
- Whether Pricing needs batch version entry across many products/components.
- Whether Catalog needs bulk import/edit screens.

## 9. Recommended implementation batches

### 03B - Sales multi-line regression coverage

Classification: UI/PageModel and Web test coverage.

Scope:

- Sales Create row add/remove/reindex shape.
- Sales Create override validation per row.
- Sales Create stock preview aggregation across repeated products/components.
- Sales Details all-line rendering.
- Sales Confirm friendly errors for multi-line shortages.

### 03C - Sales history/details polish

Classification: UI/PageModel-only change, if needed.

Scope:

- CustomerHistory multi-product labels and summaries.
- Details table readability when many lines and BOM snapshot items exist.
- No domain/application/schema changes expected.

### 03D - BOM multi-line guard tests

Classification: Web tests and possible UI/PageModel-only polish.

Scope:

- Add/remove/reindex rows.
- Validation error preservation.
- Edit item reconciliation coverage.

### 03E - Inventory posting guard tests

Classification: Web tests and possible UI/PageModel-only polish.

Scope:

- Receipt, Issue, Adjustment add/remove/reindex.
- Date text binding for multiple receipt/increase lines.
- Consolidated issue behavior visibility.

### 03F - Sales Edit parity

Classification: UI/PageModel-only first; Application service changes only if a specific missing endpoint is found.

Scope:

- Bring Edit closer to Create for product context, suggested price display, row-level validation, and stock preview.
- Preserve existing backend add/remove semantics.
- Do not change confirmation/FIFO or product-stock/no-BOM rules in this batch.

### 03G - Pricing batch decision

Classification: Needs business decision.

Scope:

- Decide whether batch price version creation is required.
- If approved, design DTO/API/domain behavior explicitly.
- Likely not DB-schema heavy, but requires application contract changes.

## 10. Suggested test plan

- Web tests for Sales Create dynamic row markup, hidden template, reindexable field names, and line-level validation keys.
- Web tests for Sales Edit line add/remove errors and friendly validation display.
- Web tests for Sales Details rendering every order line and nested BOM snapshot items.
- App/EF tests for Sales Create and Confirm with two products sharing components to verify consolidation remains correct.
- Web tests for BOM Create/Edit dynamic rows and validation preservation.
- App/EF tests for BOM Update replacing, removing, and appending multiple items.
- Web tests for Inventory Receipt/Issue/Adjustment dynamic row helpers.
- App/EF tests for Inventory multi-line receipt, issue consolidation, FIFO allocation, and adjustment paths.
- Pricing tests should remain per item unless batch pricing is approved.
- Catalog and Audit should keep current single-entity/inquiry tests.

No build or test run is required for this audit because only documentation was changed.

## 11. Risks and guardrails

- Do not treat product-stock/no-BOM selling as a UI-only issue. Sales Confirm currently needs BOM components and component FIFO issue logic.
- Do not add schema or index changes for multi-line parity unless a future implementation proves the existing line tables cannot support it.
- Do not broaden DTO/API contracts for batch pricing or bulk catalog workflows without product owner approval.
- Keep Sales Create and Edit validation rules aligned so row-level errors bind to the same user-visible fields.
- Preserve known `BusinessException` friendly handling, and avoid broad exception filters that swallow `OperationCanceledException`.
- Keep confirmation idempotency intact when adding any future multi-line confirm behavior.
- Keep tests focused on visible workflow cardinality and binding, not cosmetic layout.
