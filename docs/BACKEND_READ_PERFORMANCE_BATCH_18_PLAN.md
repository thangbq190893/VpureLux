# Batch 18.1 - Backend Read Performance Audit Plan

## 1. Executive summary

This audit confirms high-priority read-performance risk in Catalog/Pricing current-price and product-pricing-context paths. The highest-confidence issues are:

- Catalog Components and Pricing Index list pages call `IComponentSuggestedSellingPriceAppService.GetCurrentAsync` once per listed component.
- `ProductPricingContextAppService.GetListAsync` performs per-product product price lookups, per-product BOM version lookups, and per-BOM-item component price lookups.
- Catalog Products, Pricing Index, Sales Create/Edit/Details, and BOM Product pages consume the same product pricing context service, so the risk is shared across multiple UI read paths.

The recommended first fix is a small backend batch that introduces list-safe batch current-price lookup methods and a batch product-pricing-context read path while preserving strict single-item `GetCurrentAsync` behavior and existing DTO semantics. Inventory and audit findings are real read-shape risks, but they are lower priority unless UAT or data volume shows them as hot paths.

## 2. Branch/base branch

- Current branch: `refactor/performance`
- Upstream branch: none configured
- Base branch: `main`
- Merge base with `main`: `c0eb98fbbe49b659ffdd514482da76229ecb8583`

## 3. Audit scope

Folders inspected:

- `src/VPureLux.Web/Pages`
- `src/VPureLux.Application`
- `src/VPureLux.Application.Contracts`
- `src/VPureLux.Domain`
- `src/VPureLux.EntityFrameworkCore`
- `test/VPureLux.*.Tests`

Modules inspected:

- Catalog
- Pricing
- BOM
- Inventory
- Sales
- Customers / CustomerGroups
- Audit

## 4. Search commands used

Commands run:

```powershell
rg "foreach|foreach await" src/VPureLux.Web/Pages src/VPureLux.Application
rg "GetCurrentAsync|GetAtDateAsync|EnsureFound|FindAtDateAsync|TryGet|GetListAsync" src/VPureLux.Web/Pages src/VPureLux.Application
rg "GetAsync\(|FindAsync\(|FirstOrDefaultAsync\(|SingleOrDefaultAsync\(" src/VPureLux.Application src/VPureLux.Web/Pages
rg "ToListAsync\(" src/VPureLux.Application src/VPureLux.EntityFrameworkCore src/VPureLux.Web/Pages
rg "Include\(|ThenInclude\(" src/VPureLux.Application src/VPureLux.EntityFrameworkCore
rg "AsNoTracking" src/VPureLux.Application src/VPureLux.EntityFrameworkCore
rg "IsGrantedAsync" src/VPureLux.Web/Pages src/VPureLux.Application
rg "ObjectMapper|Map<|ProjectTo|Select\(" src/VPureLux.Application src/VPureLux.Web/Pages
rg "PageModel|OnGetAsync" src/VPureLux.Web/Pages
rg "PricingContext|Suggested|GetCurrent|FindAtDate|NoPrice|PriceVersion|Bom|Sales|Catalog|Inventory|Audit" test
rg -n "Product_Pricing_Context|GetProductContextAsync|missing|Should_Allow_Today|GetCurrentAsync" test/VPureLux.EntityFrameworkCore.Tests/EntityFrameworkCore/Pricing/PricingAppServiceTests.cs
rg -n "Catalog|Components|Product|CurrentSuggested|suggested|Price" test/VPureLux.Web.Tests/Pages test/VPureLux.Web.Tests/Api
git branch --show-current
git rev-parse --abbrev-ref --symbolic-full-name "@{u}"
git merge-base --fork-point main
git status --short
```

One Windows glob attempt failed and was replaced by the `rg ... test` command above:

```powershell
rg "PricingContext|Suggested|GetCurrent|FindAtDate|NoPrice|PriceVersion|Bom|Sales|Catalog|Inventory|Audit" test/VPureLux.*.Tests
```

## 5. High-risk findings table

| Severity | Module | File path | Method/Page | Finding | Suspected query count / risk | Recommended fix | Behavior risk |
|---|---|---|---|---|---|---|---|
| Should Fix | Catalog / Pricing | `src/VPureLux.Web/Pages/Catalog/Components/Index.cshtml.cs` | `OnGetAsync` | Component list loops over `result.Items` and calls `GetCurrentAsync` per row when pricing context is visible. UAT already observed slow Catalog Components load. | 1 component list query + 3 permission checks + up to N current-price queries; N is page size, currently 100. | Add nullable batch current component price lookup such as `FindCurrentMapAsync(componentIds, date)` in pricing application/repository layer. Keep strict `GetCurrentAsync` unchanged for detail/API contracts. | Low if missing prices remain nullable in list rows and strict single-item behavior remains unchanged. |
| Should Fix | Pricing | `src/VPureLux.Web/Pages/Pricing/Index.cshtml.cs` | `OnGetAsync` | Pricing component tab repeats the same per-component current-price lookup and filters active components in memory after loading a page. | Up to N current-price queries plus possible page-size mismatch because inactive rows are removed after paging. | Use active filter at query layer if contract supports it, and use the same batch current component price map. | Low to medium; active filtering must preserve intended list ordering and count behavior. |
| Should Fix | Pricing / BOM / Sales | `src/VPureLux.Application/Pricing/ProductPricingContextAppService.cs` | `GetListAsync` | Product pricing context fetches all products, then for each product calls product current price lookup and BOM list lookup; for each published BOM item it calls component current price lookup. | Roughly 1 product query + P product price queries + P BOM queries + sum(BOM items) component price queries. This can grow quickly and affects Product list, Pricing Index, Sales Create/Edit/Details, and BOM Product. | Add batch product pricing context query: page/filter product IDs first, batch product prices, batch published BOMs with items, batch distinct component prices, then compose DTOs in memory. | Medium; must preserve no-BOM, missing-component-price, current-product-price, difference, and date semantics. |
| Should Fix | Catalog / Products | `src/VPureLux.Web/Pages/Catalog/Products/Index.cshtml.cs` | `OnGetAsync` | Product list loads only the current 100 products, but then calls `GetListAsync()` for pricing contexts for all products. | Product list query plus full product pricing context for all products, which inherits the P + P + I lookup shape above. | Introduce product-ID-scoped pricing context query or page-aware application method. | Low if the UI still shows context only for products on the page. |
| Should Fix | Sales | `src/VPureLux.Web/Pages/Sales/Create.cshtml.cs`, `Edit.cshtml.cs`, `Details.cshtml.cs` | `LoadProductContextsAsync` | Sales pages call full product pricing context even when only one selected product, active products, or order line product IDs are needed. | Full product pricing context load on page render or AJAX handler; high when products/BOMs grow. | Add product-ID-scoped or active-product-scoped pricing context read method; AJAX handler should fetch one product context or use a bounded batch. | Medium; sales price/BOM status presentation must match current semantics. |
| Nice-to-have | Inventory | `src/VPureLux.Application/Inventory/InventoryQueryAppService.cs`; EF repositories | `GetBalancesAsync`, `GetLotsAsync`, `GetLedgerAsync` | Inventory balance, lot, and ledger read methods return unpaged lists. Ledger includes lines and allocations for every matched posted transaction. | Potentially unbounded rows and wide include graph. | Add paged query contracts/read models for list pages if real data volume warrants it. | Medium if API contracts change; should be a separately approved batch. |
| Nice-to-have | Sales | `src/VPureLux.EntityFrameworkCore/Sales/EfCoreSalesOrderRepository.cs` | `GetListAsync`, `GetCustomerPurchaseHistoryAsync` | Sales order list always includes lines and BOM snapshot items; customer purchase history loads all confirmed orders and groups in memory. | Wide include on list pages; customer history may grow unbounded per customer. | Project list summaries where line detail is not needed; push customer history grouping to query/projection if volume grows. | Medium; financial visibility and snapshot display must remain intact. |
| Nice-to-have | Inventory | `src/VPureLux.Web/Pages/Inventory/Balances.cshtml.cs`, `Lots.cshtml.cs`, `Ledger.cshtml.cs` | `OnGetAsync` and helpers | Pages load options and labels through repeated warehouse/stock item list calls with `MaxMaxResultCount`. | 4 supporting list calls on Balances/Lots plus main query; manageable now, potentially wasteful with many stock items. | Consolidate label/option loading or add lightweight option/label endpoints if page load becomes slow. | Low. |
| Defer | Audit | `src/VPureLux.Application/Audit/BusinessAuditAppService.cs`; `src/VPureLux.EntityFrameworkCore/Audit/EfCoreBusinessAuditLogRepository.cs` | `GetListAsync`, `ExportAsync` | Audit list is paged and `AsNoTracking`; export caps at 5000 rows. No N+1 found. | Bounded export; query may need DBA index decisions later for date/module filters. | Defer unless audit pages become slow at production volume; index changes are separate DBA/migration decisions. | Low. |

## 6. N+1 findings table

| Caller | Callee | Loop source | Expected data volume | Proposed batch/query alternative |
|---|---|---|---|---|
| `Catalog.Components.IndexModel.OnGetAsync` | `IComponentSuggestedSellingPriceAppService.GetCurrentAsync` -> `FindAtDateAsync` | `result.Items` from component page, max 100 | Component catalog page size; UAT hot path | `FindCurrentMapAsync(IEnumerable<Guid> componentIds, DateTime date)` returning nullable map for listed component IDs. |
| `Pricing.IndexModel.OnGetAsync` | `IComponentSuggestedSellingPriceAppService.GetCurrentAsync` -> `FindAtDateAsync` | Active components after list load, max 100 before in-memory filtering | Pricing component tab | Same component current-price batch map; filter active components in the query where possible. |
| `ProductPricingContextAppService.GetListAsync` | `_productPriceRepository.FindAtDateAsync` | All products | Product count | Batch product current prices for all requested product IDs. |
| `ProductPricingContextAppService.GetListAsync` | `_bomVersionRepository.GetListByProductIdAsync` | All products | Product count, with BOM items included | Batch published BOMs and items for requested product IDs, not all versions per product. |
| `ProductPricingContextAppService.GetListAsync` | `_componentPriceRepository.FindAtDateAsync` | Each item in each published BOM | Sum of BOM item counts | Batch distinct component current prices once per context request. |
| `SalesOrderAppService.ConfirmAsync` | `ConfirmLineAsync` -> `EnsureActiveProductAsync`, `EnsurePublishedBomAsync`, `EnsureActiveComponentAsync` | Order lines and BOM items | Write path, bounded by a single order | Not part of immediate read-performance batch. If needed later, cache products/BOMs/components within a confirmation transaction without changing FIFO semantics. |
| `InventoryTransactionAppService` / `SalesOrderAppService.PostInventoryIssueAsync` | Lot and balance repository calls inside allocation loops | Inventory allocations | Write path, bounded by posting operation | Defer; preserve FIFO and posting semantics. |

## 7. Current price/version lookup findings

Component suggested selling price:

- Confirmed high priority on `Catalog.Components.IndexModel.OnGetAsync` and `Pricing.IndexModel.OnGetAsync`.
- Current behavior treats missing component price as a nullable list value by catching `PriceVersionNotFound`.
- Recommended fix: add nullable batch lookup while keeping `IComponentSuggestedSellingPriceAppService.GetCurrentAsync` strict for current API/detail behavior.

Product suggested price:

- Confirmed high priority inside `ProductPricingContextAppService.GetListAsync`, one `FindAtDateAsync` per product.
- Recommended fix: batch by product IDs and date, returning at most one current version per product.

BOM current/published version:

- Confirmed high priority inside `ProductPricingContextAppService.GetListAsync`, one `GetListByProductIdAsync` per product with `Include(x => x.Items)` and all versions loaded before selecting published.
- Recommended fix: add a published-BOM batch read for requested product IDs that returns only published versions and items needed for context calculation.

Sales pricing/profit context:

- Sales Create/Edit/Details consume full product pricing context for product status/BOM/suggested-price display.
- `SalesOrderAppService.GetListAsync` and `GetAsync` perform only two permission checks per call for cost/profit visibility; no permission N+1 found there.
- Recommended fix: after product pricing context batching exists, expose product-ID-scoped usage for Sales pages so details/AJAX do not compute all products.

## 8. EF query findings

Over-fetching:

- `ProductPricingContextAppService.GetListAsync` uses `_productRepository.GetListAsync()` without paging/filtering and computes contexts for all products.
- `EfCoreBomVersionRepository.GetListByProductIdAsync` includes items and loads all BOM versions for a product when the caller only needs the published version.
- `EfCoreSalesOrderRepository.GetListAsync` includes lines and BOM snapshot items for list pages. This is useful for current DTOs but may be wider than needed for order lists.
- Inventory ledger includes lines and allocations for every matched posted transaction and is unpaged.

Missing projection:

- Catalog component/product `GetListAsync` already project to DTOs before materialization.
- Inventory balances/lots/ledger materialize entities then map to DTOs; acceptable for small sets but risky because contracts are unpaged.
- Sales order list materializes aggregate entities with details, then maps in application memory.

Missing `AsNoTracking`:

- Audit repository and sales customer history use `AsNoTracking`.
- Many repository/app-service read paths rely on ABP repository queryables without explicit `AsNoTracking`. This is a theoretical read-only optimization opportunity, not the first fix, because ABP unit-of-work behavior and write/detail methods need care.

`Include` risk:

- BOM versions include items for all versions in `GetListByProductIdAsync`.
- Sales orders include lines and BOM snapshot items on list/detail queries.
- Inventory transactions include lines and allocations for ledger and idempotency reads.

Paging risk:

- Catalog product/component list methods page correctly before DTO materialization.
- Pricing Index loads max 100 components then filters active in memory, which can produce fewer rows than expected and still performs per-row price calls.
- Product pricing context has no paging or product-ID filter.
- Inventory query app service returns unpaged lists.

In-memory filtering risk:

- Pricing Index filters active components after `GetListAsync`.
- Catalog Products filters product pricing context dictionary after computing all product contexts.
- Sales Details filters full product pricing context to order-line products after computing all product contexts.
- Sales customer purchase history groups confirmed orders in memory after loading all confirmed orders for a customer.

## 9. Permission lookup findings

Repeated permission checks:

- Catalog component/product list pages each perform three permission checks once per request. This is acceptable and not inside row loops.
- Sales Details performs three action permission checks once per request after loading the order. This is acceptable.
- Sales application service checks cost/profit visibility once per list/detail call.

Permission checks inside loops:

- No `IsGrantedAsync` or `AuthorizeAsync` inside backend read loops was found.
- `SalesOrderAppService.EnsureOverridePermissionAsync` can run during create/update write workflows, including per input line on create, but this is a write-path business rule and not a read-performance priority.

Permission checks already cached/precomputed:

- PageModels generally store booleans such as `CanCreate`, `CanEdit`, `CanViewPricingContext`, `CanConfirm`, and `CanCancel` before rendering rows.
- Product pricing context authorization is enforced by the application service policy, and Sales pages catch `AbpAuthorizationException` when pricing context is unavailable.

## 10. Test coverage findings

Existing tests:

- Pricing service tests cover strict current-price behavior, no-price `PRICE_003`, no published BOM, missing component prices, component build price, product suggested price, and difference calculation.
- Pricing page tests cover component tab current price display, no-current-price friendly empty state, and product pricing context rendering.
- Catalog page tests cover current suggested price display on product/component list pages.
- Sales page tests cover product context hooks, published BOM status, product labels, BOM badges, and profit/cost display behavior.
- Audit, inventory, BOM, customer, catalog, and sales repository/workflow tests exist for functional behavior and permissions.

Missing regression tests:

- No test asserts that Catalog Components list obtains current component prices with a bounded number of repository/service calls.
- No test asserts that Pricing Index component tab uses a batch current-price path.
- No test asserts that product pricing context calculation avoids per-product/per-BOM-item queries.
- No test covers multi-product/multi-BOM data volume large enough to expose the current N+1 shape.
- No test verifies product-ID-scoped pricing context for Sales Details/AJAX once added.
- No test covers no-price/no-current-version behavior for batch lookup methods because those methods do not exist yet.

Tests that make N+1 invisible:

- Pricing context tests usually create one product or one BOM with one or two components.
- Page render tests validate HTML strings and behavior, not query count or data access shape.
- SQLite in-memory functional tests are fast enough that per-row query patterns remain hidden.

Recommended test additions for implementation batches:

- Add repository/app-service tests for batch current-price maps including priced and no-price entities.
- Add product pricing context tests with several products, published/no-published BOM combinations, and repeated shared components.
- Add source-shape or mock interaction tests where practical to prevent `GetCurrentAsync` from returning to list loops.

## 11. Proposed implementation batches

Batch 18.2 - Catalog/Pricing current price batch lookup:

- Add nullable batch current component price lookup.
- Replace per-row component current-price calls in Catalog Components and Pricing Index.
- Add regression tests for no-price rows and multiple components.
- Keep strict `GetCurrentAsync` and existing API behavior unchanged.

Batch 18.3 - Product pricing context batch optimization:

- Add batch product current-price lookup.
- Add published-BOM-by-product batch read including needed items.
- Add distinct component current-price batch lookup for BOM components.
- Allow product-ID-scoped context calls for Catalog Products, Sales pages, and BOM Product where appropriate.
- Preserve DTO contracts unless separately approved.

Batch 18.4 - Inventory and Sales read query optimization, only if justified:

- Add paged inventory balance/lot/ledger read contracts or view models if UAT/prod-sized data shows slowness.
- Consider sales list summary projection if wide include chains become a measured bottleneck.
- Do not change FIFO, inventory posting, sales lifecycle, or financial visibility semantics.

Batch 18.5 - Final validation and PR summary:

- Run focused backend/page tests plus UAT smoke on Catalog Components, Pricing, Catalog Products, BOM Product, and Sales Create/Edit/Details.
- Summarize behavior invariants preserved and any deferred DBA/index decisions.

## 12. Non-goals

- No DB schema/index changes in this phase unless separately approved.
- No business-rule changes.
- No permission behavior changes.
- No API contract changes unless separately approved.
- No DTO contract changes unless separately approved.
- No UI redesign.
- No Razor/CSS/JS/test changes in this audit.
- No production code changes in this audit.
- No raw SQL unless EF cannot express a future query safely.
- No inventory posting/FIFO changes.
- No sales order lifecycle changes.

## 13. Recommended first fix batch

Recommended first implementation batch: Batch 18.2 - Catalog/Pricing current price batch lookup.

This is the smallest safe first fix because it directly addresses the UAT-observed Catalog Components slowness, removes confirmed per-row current-price calls from two list pages, and can preserve strict single-entity price APIs by introducing a separate nullable batch read path. Batch 18.3 should follow because `ProductPricingContextAppService.GetListAsync` is the broader shared hot path behind Catalog Products, Pricing product context, BOM Product, and Sales context views.
