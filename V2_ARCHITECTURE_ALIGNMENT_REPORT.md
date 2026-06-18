# V2 Architecture Alignment Report

## 1. V2 Docs Read

Read order followed from `CODEX_README_VPURELUX_V2.md`:

1. `CODEX_README_VPURELUX_V2.md`
2. `BUSINESS_ARCHITECTURE_DECISIONS_V2.md`
3. `MODULE_SPECIFICATIONS_V2.md`
4. `UI_UX_ABP_GUIDE_V2.md`
5. `DATA_FLOW_UAT_SCENARIOS_V2.md`
6. `IMPLEMENTATION_ROADMAP_V2.md`

V2 documents are treated as the current source of truth. Older specifications are historical references only where they do not conflict with V2.

## 2. Executive Summary

The current source is not V2-aligned yet. The largest architecture conflicts are in Pricing and Sales:

- Pricing still implements `ComponentPurchasePriceVersion`, while V2 requires Component Suggested Selling Price.
- Sales still supports direct Component sales through `SalesOrderLineType.Component`, while V2 requires all sales to be Product/SKU based.
- V2 loose component sales must be modeled as Product/SKU with a one-component BOM, but the current system bypasses this model.
- Product Suggested Selling Price exists, but the V2 Product pricing screen behavior is incomplete because it does not show Component Build Price or missing component price warnings.
- Catalog Product/Component domain supports activation, but the application/UI layers do not expose symmetric Product/Component Activate actions.
- Inventory phase 1 is broadly aligned with component-only stock, but operator UI still exposes raw GUIDs outside Receipt.

Recommended first implementation batch is Pricing V2 Realignment, preceded by an explicit migration/data decision because replacing `ComponentPurchasePriceVersion` may require destructive schema/data changes or a carefully mapped migration.

## 3. Source Conflicts With V2 Decisions

### 3.1 Product/SKU Sales Model

V2 decision:

- Everything sold is a Product/SKU.
- Sales must not sell Components directly.
- A loose component sale must be represented as a Product/SKU whose BOM contains exactly one Component.

Current implementation conflict:

- Sales domain has `SalesOrderLineType.Component`.
- Sales DTOs expose `LineType`, `CatalogItemId`, and optional `BomVersionId`.
- Sales application validates and confirms Component lines separately from Product lines.
- Sales UI allows selecting line type and entering raw Catalog/BOM identifiers.
- Sales tests rely heavily on direct Component sales.

Source evidence:

- `src/VPureLux.Domain/Sales/SalesOrderLine.cs`
- `src/VPureLux.Application.Contracts/Sales/SalesInputs.cs`
- `src/VPureLux.Application.Contracts/Sales/SalesDtos.cs`
- `src/VPureLux.Application/Sales/SalesOrderAppService.cs`
- `src/VPureLux.Web/Pages/Sales/Create.cshtml`
- `src/VPureLux.Web/Pages/Sales/Edit.cshtml`
- `test/VPureLux.Domain.Tests/Sales/SalesDomainTests.cs`
- `test/VPureLux.EntityFrameworkCore.Tests/EntityFrameworkCore/Sales/SalesWorkflowTests.cs`
- `test/VPureLux.Web.Tests/Api/SalesApiTests.cs`

Alignment action:

- Refactor Sales to Product/SKU-only order lines.
- Remove direct Component sale workflow from Sales UI and application orchestration.
- Preserve historical data only through an approved migration strategy.

### 3.2 Direct Component Sales Support

V2 decision:

- Direct Component sales are not allowed as Sales lines.
- A component sold loose must have a Catalog Product/SKU and a published BOM containing that Component.

Current implementation conflict:

- `SalesOrderLineType.Component` is a first-class domain option.
- `SalesOrderAppService` validates active Components directly and issues inventory directly for Component lines.
- Sales UI exposes Component line type.

Source evidence:

- `src/VPureLux.Application/Sales/SalesOrderAppService.cs` branches on `SalesOrderLineType.Component`.
- `src/VPureLux.Domain/Sales/SalesOrderLine.cs` permits Component lines without `BomVersionId`.
- `src/VPureLux.Web/Pages/Sales/Create.cshtml` renders `Input.Lines[0].LineType`.
- `src/VPureLux.Web/Pages/Sales/Edit.cshtml` renders `NewLine.LineType`.

Alignment action:

- Replace direct Component sale selection with Product/SKU selection.
- Inventory consumption must still issue Components, but only after Product BOM expansion.

### 3.3 ComponentPurchasePriceVersion Old Concept

V2 decision:

- Pricing manages suggested selling prices only.
- `ComponentPurchasePriceVersion` is obsolete.
- V2 requires Component Suggested Selling Price.

Current implementation conflict:

- Domain, application contracts, app services, EF mappings, API controllers, UI pages, tests, permissions, migrations, and Audit handlers still reference `ComponentPurchasePriceVersion`.

Source evidence:

- `src/VPureLux.Domain/Pricing/ComponentPurchasePriceVersion.cs`
- `src/VPureLux.Domain/Pricing/IComponentPurchasePriceVersionRepository.cs`
- `src/VPureLux.Domain/Pricing/PricingManager.cs`
- `src/VPureLux.Application.Contracts/Pricing/IComponentPurchasePriceAppService.cs`
- `src/VPureLux.Application.Contracts/Pricing/ComponentPurchasePriceVersionDto.cs`
- `src/VPureLux.Application.Contracts/Pricing/CreateComponentPurchasePriceVersionDto.cs`
- `src/VPureLux.Application/Pricing/ComponentPurchasePriceAppService.cs`
- `src/VPureLux.EntityFrameworkCore/Pricing/ComponentPurchasePriceVersionConfiguration.cs`
- `src/VPureLux.EntityFrameworkCore/Pricing/EfCoreComponentPurchasePriceVersionRepository.cs`
- `src/VPureLux.HttpApi/Pricing/ComponentPurchasePriceController.cs`
- `src/VPureLux.Web/Pages/Pricing/Components/Create.cshtml.cs`
- `src/VPureLux.Web/Pages/Pricing/Components/History.cshtml.cs`
- `src/VPureLux.Application/Audit/BusinessAuditEventHandler.cs`
- Pricing tests across Domain, EF, API, Web.

Alignment action:

- Replace the old component purchase price model with Component Suggested Selling Price.
- Do not merely relabel UI text; this is a model-level correction.

### 3.4 Missing Component Suggested Selling Price

V2 decision:

- Component Suggested Selling Price is required.
- Product Component Build Price is calculated from BOM quantities and current Component Suggested Selling Prices.

Current implementation conflict:

- The existing Component price aggregate represents purchase price, not suggested selling price.
- No V2 read model/service is visible for Product Component Build Price.
- Current Pricing UI has component purchase price history/create pages.

Alignment action:

- Introduce `ComponentSuggestedSellingPriceVersion` concept across Pricing layers.
- Add current/history lookup and create-only versioning behavior equivalent to existing price-version patterns.
- Add Product pricing view support for Component Build Price and missing component price indicators.

### 3.5 Product Suggested Selling Price Behavior

V2 decision:

- Product Suggested Selling Price is manually entered.
- It is not automatically equal to BOM cost or component build price.
- Component Build Price is a reference/read-side value.

Current implementation status:

- Product suggested price versioning exists and is broadly aligned at the aggregate level.
- Product pricing UI does not show the V2 comparison context: Component Build Price, Suggested Selling Price, and margin/price review context.

Source evidence:

- `src/VPureLux.Domain/Pricing/ProductSuggestedPriceVersion.cs`
- `src/VPureLux.Application/Pricing/ProductSuggestedPriceAppService.cs`
- `src/VPureLux.Web/Pages/Pricing/Products/History.cshtml.cs`
- `src/VPureLux.Web/Pages/Pricing/Products/Create.cshtml.cs`

Alignment action:

- Keep Product Suggested Selling Price as manual versioning.
- Add V2 read-side pricing context only after approval of required app/query contract.

### 3.6 BOM Required For Every Sellable Product/SKU

V2 decision:

- Every sellable Product/SKU must have a published BOM before sale.
- Loose component sales require a Product/SKU with one BOM component line.

Current implementation status:

- BOM aggregate shape is mostly aligned: Product-owned version, component lines, published status.
- Sales enforces BOM only for Product lines.
- Because Sales still supports Component lines, V2 BOM requirement is bypassed.
- Catalog/Product activation does not currently enforce published BOM readiness.

Source evidence:

- `src/VPureLux.Domain/Bom/BomVersion.cs`
- `src/VPureLux.Application/Sales/SalesOrderAppService.cs`
- `src/VPureLux.Web/Pages/Bom/Create.cshtml`
- `src/VPureLux.Web/Pages/Bom/Edit.cshtml`
- `src/VPureLux.Web/Pages/Bom/Details.cshtml`

Alignment action:

- Refactor Sales to Product/SKU-only lines.
- Decide whether "has published BOM" is enforced at Sales selection/confirmation only, or also at Product activation/readiness.

### 3.7 Product With BOM One Component For Loose Component Sales

V2 decision:

- A loose Component sold to customers must be represented as a Product/SKU.
- The Product/SKU must have a BOM with one Component.

Current implementation conflict:

- No workflow currently guides operators to create loose-component Product/SKUs.
- Sales uses direct Component lines instead.
- BOM UI still requires raw ComponentId entry, making the V2 loose-component setup difficult for operators.

Alignment action:

- Provide a Product/SKU setup workflow or documented operational path for loose component sale products.
- Fix BOM product/component selectors before UAT.

### 3.8 Inventory Component-Only Stock In Phase 1

V2 decision:

- Inventory stocks Components only in phase 1.
- Generic StockItem architecture may exist, but Product StockItems are disabled in phase 1.

Current implementation status:

- Generic `StockItem` architecture exists.
- Inventory phase 1 component-only policy appears broadly aligned.
- Receipt UI has been partially cleaned up with selectors and hidden idempotency key.
- Issue, Adjustment, Balances, Lots, and Ledger still expose raw IDs.

Source evidence:

- `src/VPureLux.Application.Contracts/Inventory/StockItemDto.cs`
- `src/VPureLux.Web/Pages/Inventory/Receipt.cshtml`
- `src/VPureLux.Web/Pages/Inventory/Issue.cshtml`
- `src/VPureLux.Web/Pages/Inventory/Adjustment.cshtml`
- `src/VPureLux.Web/Pages/Inventory/Balances.cshtml`
- `src/VPureLux.Web/Pages/Inventory/Lots.cshtml`
- `src/VPureLux.Web/Pages/Inventory/Ledger.cshtml`

Alignment action:

- Keep component-only stock policy.
- Complete operator UI selector/readability fixes for Issue, Adjustment, Balances, Lots, and Ledger.

### 3.9 Raw GUID UI Blockers

V2 decision:

- Operator UI must not require raw GUID typing.
- Idempotency keys must be hidden.
- Operators should select by business code/name.

Current blockers found:

Inventory:

- `src/VPureLux.Web/Pages/Inventory/Issue.cshtml` exposes WarehouseId, IdempotencyKey, StockItemId.
- `src/VPureLux.Web/Pages/Inventory/Adjustment.cshtml` exposes WarehouseId, IdempotencyKey, StockItemId.
- `src/VPureLux.Web/Pages/Inventory/Balances.cshtml` displays WarehouseId and StockItemId.
- `src/VPureLux.Web/Pages/Inventory/Lots.cshtml` displays StockItemId.
- `src/VPureLux.Web/Pages/Inventory/Ledger.cshtml` displays WarehouseId.
- `src/VPureLux.Web/Pages/Inventory/Receipt.cshtml` now uses selectors and hidden IdempotencyKey, but still requires review against V2 multi-line receipt expectations.

BOM:

- `src/VPureLux.Web/Pages/Bom/Index.cshtml` exposes ProductId entry.
- `src/VPureLux.Web/Pages/Bom/Product.cshtml` displays ProductId.
- `src/VPureLux.Web/Pages/Bom/Create.cshtml` exposes ComponentId entry.
- `src/VPureLux.Web/Pages/Bom/Edit.cshtml` exposes ComponentId entry.
- `src/VPureLux.Web/Pages/Bom/Details.cshtml` displays ProductId and ComponentId.

Sales:

- `src/VPureLux.Web/Pages/Sales/Create.cshtml` exposes CatalogItemId and BomVersionId.
- `src/VPureLux.Web/Pages/Sales/Edit.cshtml` exposes CatalogItemId and BomVersionId.
- `src/VPureLux.Web/Pages/Sales/CustomerHistory.cshtml` displays CatalogItemId.

Alignment action:

- Use existing application services where they already provide code/name values.
- Add selector contracts only with explicit approval where no suitable app service exists.

### 3.10 Missing Activate Actions

V2 decision:

- Product and Component status actions should be symmetric where business rules allow.
- Inactive rows should show the next available action, such as Activate/Use Again.

Current implementation status:

- Domain supports Product and Component activation.
- Domain events for Product/Component activation exist.
- Application contracts and app services expose only Deactivate for Product/Component.
- Catalog UI cannot truly reactivate Product/Component through existing app service methods.
- Components UI has been adjusted to avoid a misleading deactivate action, but backend activation is still unavailable.

Source evidence:

- `src/VPureLux.Domain/Catalog/Product.cs`
- `src/VPureLux.Domain/Catalog/Component.cs`
- `src/VPureLux.Domain/Catalog/Events/ProductActivatedEvent.cs`
- `src/VPureLux.Domain/Catalog/Events/ComponentActivatedEvent.cs`
- `src/VPureLux.Application.Contracts/Catalog/IProductAppService.cs`
- `src/VPureLux.Application.Contracts/Catalog/IComponentAppService.cs`
- `src/VPureLux.Application/Catalog/ProductAppService.cs`
- `src/VPureLux.Application/Catalog/ComponentAppService.cs`
- `src/VPureLux.Web/Pages/Catalog/Products/Index.cshtml.cs`
- `src/VPureLux.Web/Pages/Catalog/Components/Index.cshtml.cs`

Alignment action:

- Add approved Product/Component Activate contracts and app service methods in a future implementation batch.
- Wire UI actions only after backend contract approval.

## 4. Impact Matrix

| Area | V2 Alignment Impact | Severity | Notes |
| --- | --- | --- | --- |
| Domain | Pricing component price aggregate and Sales line model conflict with V2. | High | Requires replacing `ComponentPurchasePriceVersion` and removing direct Component sales. |
| Application Contracts | Pricing and Sales DTOs/interfaces expose obsolete concepts. | High | Public/internal contracts will change. Requires approval. |
| Application | Sales orchestration supports direct Component sale; Pricing app service names/semantics are wrong. | High | Business behavior must be realigned, not just UI text. |
| EF Core/Migrations | Existing `AppComponentPurchasePriceVersions` table conflicts with V2 naming/semantics. Sales persisted line type supports Component. | High | Data migration or destructive reset decision needed. |
| Web/Razor UI | Raw GUID blockers remain; Sales and Pricing UI expose obsolete workflows. | High | Blocks operator UAT. |
| Tests | Many tests assert old Pricing and Sales behavior. | High | V2 rewrite will require broad test realignment. |
| Audit | Audit ingests old component purchase price events. | Medium/High | Must track component suggested price events instead. |
| Permissions | Pricing permissions use `ComponentPurchasePrices`. | Medium/High | Need rename/replacement strategy and grant migration decision. |
| API | Pricing API exposes component purchase price routes. Sales API allows Component line payloads. | High | API contract update required. |

## 5. Migration And Data Risk Assessment

### Pricing Data Risk

Risk level: High.

Current table/model:

- `AppComponentPurchasePriceVersions`
- `ComponentPurchasePriceVersion`
- `ComponentPurchasePriceVersionCreatedEvent`

V2 target:

- Component Suggested Selling Price versioning.

Risk:

- Existing values may represent supplier purchase cost, not suggested selling price.
- Blindly renaming the table would preserve data but may corrupt business meaning.
- Replacing the table could lose existing data if not migrated deliberately.

Decision required:

- Treat existing component purchase prices as invalid historical dev data and reset.
- Or migrate existing rows into Component Suggested Selling Price with explicit business acceptance.
- Or keep a temporary compatibility table only for migration/export, then remove old behavior.

### Sales Data Risk

Risk level: High.

Current persisted order lines can represent direct Component sales. V2 requires Product/SKU lines only.

Risk:

- Existing SalesOrderLine rows with `LineType = Component` need mapping to Product/SKU.
- Mapping requires a Product/SKU and published one-component BOM for each sold Component.
- Historical snapshots must remain correct.

Decision required:

- If existing Sales data is not production data, destructive reset is simpler.
- If data must be preserved, create migration tooling to generate Product/SKU mappings or archive old orders.

### Permission/Data Grant Risk

Risk level: Medium.

Current permission names include Component Purchase Prices. V2 likely needs Component Suggested Prices.

Decision required:

- Rename permissions and migrate grants.
- Or keep old permission names temporarily while changing labels/semantics, which is less clean and risks confusion.

### Migration Reset Possibility

Destructive migration/data reset may be required or strongly preferred if this is still a development database. If production-like data must be preserved, destructive reset should not be used without explicit approval and a data mapping plan.

## 6. Recommended Implementation Phases

### Phase 0 - V2 Audit Baseline

Status: this report.

Output:

- Architecture conflicts identified.
- Migration/data decisions surfaced.

### Phase 1 - Pricing V2 Realignment

Goal:

- Replace `ComponentPurchasePriceVersion` with Component Suggested Selling Price.
- Keep Product Suggested Selling Price manual.
- Add V2 Product pricing reference behavior for Component Build Price after approved query/read model.

Scope:

- Domain, contracts, app services, EF, migrations, API, Razor UI, tests, audit event ingestion, permissions.

Exit criteria:

- No `ComponentPurchasePrice` production concept remains unless explicitly retained as migration-only historical code.
- Component Suggested Selling Price current/history/create flows pass tests.
- Product pricing can show V2-compliant context or has an approved deferred query.

### Phase 2 - Catalog Activation Symmetry

Goal:

- Add Product/Component Activate app service/API/UI support using existing domain behavior.

Scope:

- Application contracts, application services, API/UI, permissions/tests if approval is granted.

Exit criteria:

- Inactive Product/Component rows show Activate/Use Again when permitted.
- Active rows show Deactivate.
- No misleading disabled reactivation action remains.

### Phase 3 - BOM V2 Usability And Product/SKU Setup

Goal:

- Remove raw GUID entry from BOM pages.
- Support Product/SKU BOM setup for loose component sales.

Scope:

- BOM UI selectors, product context display, component selector, details readability.
- Optional usage/note fields only if explicitly approved.

Exit criteria:

- Operator can create a one-component BOM without typing ComponentId.
- BOM pages are usable for sellable Product/SKU setup.

### Phase 4 - Inventory UI V2 Completion

Goal:

- Preserve component-only inventory policy.
- Remove raw GUIDs from Issue, Adjustment, Balances, Lots, Ledger.
- Review Receipt against V2 UAT flow.

Scope:

- Razor/PageModel selectors using existing app services where available.
- No business logic movement into UI.

Exit criteria:

- Operators can post receipt/issue/adjustment without raw IDs.
- Query pages display business labels instead of GUIDs.

### Phase 5 - Sales V2 Refactor

Goal:

- Convert Sales to Product/SKU-only lines.
- Require published BOM for all sellable Products/SKUs.
- Consume component inventory through BOM expansion.

Scope:

- Domain model, DTOs, app orchestration, EF/migrations, API, UI, tests, audit updates.

Exit criteria:

- No direct Component line creation.
- Sales order confirmation always uses Product/SKU BOM expansion.
- Loose component sale works through Product/SKU with one-component BOM.

### Phase 6 - Audit V2 Realignment

Goal:

- Update audit event ingestion to V2 event names/concepts.
- Keep business audit readable and safe.

Scope:

- Audit event handlers, tests, UI detail readability.

Exit criteria:

- Audit no longer refers to obsolete component purchase price events.
- Pricing/Sales V2 events are audited.

### Phase 7 - ABP UI Completion

Goal:

- Finish ABP modal/action/menu/script patterns after business model alignment.

Scope:

- UI-only refactor under V2 docs.

Exit criteria:

- No raw GUID operator workflows.
- Modal/full-page pattern matches `UI_UX_ABP_GUIDE_V2.md`.

## 7. Recommended First Implementation Batch

Recommended first batch:

Pricing V2 Realignment Planning + Migration Decision.

Why first:

- `ComponentPurchasePriceVersion` is a core model conflict with V2.
- Sales V2 depends on correct suggested selling price behavior.
- Product pricing comparison depends on Component Suggested Selling Price.
- Audit and tests currently depend on the obsolete component purchase price event.

Proposed batch content:

1. Confirm migration strategy for `AppComponentPurchasePriceVersions`.
2. Confirm final names for aggregate, DTOs, permissions, API routes, and localization keys.
3. Confirm whether existing data is discarded, migrated, or preserved separately.
4. Implement Component Suggested Selling Price across layers after approval.
5. Update tests and audit ingestion for V2 pricing.

Do not start Sales V2 refactor until Pricing V2 is corrected, because Sales default price behavior depends on Pricing semantics.

## 8. Questions Requiring User Approval

1. Is destructive database reset allowed for the development environment?
2. Should existing `ComponentPurchasePriceVersion` rows be migrated to Component Suggested Selling Price, or discarded as obsolete dev data?
3. What final API route should replace component purchase price routes?
   Example: `/api/pricing/components/{componentId}/suggested-prices`.
4. Should permission names be renamed from `Pricing.ComponentPurchasePrices.*` to a V2 component suggested price permission set?
5. If permission names change, should existing grants be migrated automatically?
6. Should existing direct Component SalesOrderLine data be migrated to Product/SKU with one-component BOM, archived, or discarded?
7. Should Product activation require an existing published BOM, or should BOM requirement be enforced only when selecting/confirming Sales lines?
8. Should Product pricing screens include Component Build Price in the same batch as Pricing V2, or as a separate read-model/UI batch?
9. Should BOM optional usage/note fields be added now, or deferred until after core V2 alignment?
10. Should Inventory Receipt be upgraded to V2 multi-line/header-level received date before Sales V2 refactor?
11. Should Catalog image behavior remain unchanged under V2? Current V2 docs do not require image architecture changes.

## 9. Backend Gap Vs UI-Only Gap

Backend/business gaps:

- Replace `ComponentPurchasePriceVersion` with Component Suggested Selling Price.
- Remove direct Component sale from Sales domain/application/contracts.
- Add Product/Component Activate app service/API support if V2 symmetry is required.
- Add/read Product Component Build Price query behavior if required in Pricing UI.
- Decide migration strategy for obsolete Pricing and Sales data.

UI-only or mostly UI gaps:

- Inventory Issue/Adjustment selector cleanup.
- Inventory Balances/Lots/Ledger business-label display.
- BOM Product/Component selector cleanup.
- Sales selector cleanup after Sales V2 backend refactor.
- Audit detail readability.
- ABP modal/action/menu/script pattern completion.

Mixed gaps requiring approval:

- Pricing V2 UI cannot be completed correctly until component suggested price contracts exist.
- Sales V2 UI cannot be completed correctly until Sales contracts stop exposing Component lines.
- Product/Component Activate UI cannot be completed correctly until app services expose Activate.

## 10. Final Determination

Current source status:

- Catalog: partially V2-aligned; activation symmetry incomplete at app/UI level.
- BOM: structurally aligned; operator UI and loose-component Product/SKU setup need work.
- Customer: no major V2 business conflict found in this audit.
- Pricing: not V2-aligned due obsolete Component Purchase Price model.
- Inventory: mostly V2-aligned architecturally; UI still has raw GUID blockers.
- Sales: not V2-aligned due direct Component sales and raw technical selectors.
- Audit: partially aligned; Pricing/Sales event coverage must follow V2 concepts.

Certification-style conclusion:

VPureLux ERP requires V2 realignment before further UI polish or UAT completion. Pricing and Sales should be treated as architecture corrections, not cosmetic refactors.
