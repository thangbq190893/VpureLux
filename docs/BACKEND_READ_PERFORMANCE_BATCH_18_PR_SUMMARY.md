# Batch 18: Backend Read Performance - Pricing Context Batching

## 1. Branch/base branch

- Working branch: `refactor/performance`
- Base branch: `main`
- Suggested PR/MR title: `Batch 18: Backend read performance - pricing context batching`
- Batch 18.5 status: focused EF tests, focused Web tests, and full Web.Tests passed after restoring ignored ABP client-library artifacts with `abp install-libs`. Runtime smoke remains separate and was not completed in this test-failure triage.

## 2. Scope summary

Batch 18 closed the highest-priority backend read-performance findings from the Batch 18.1 audit. The implemented scope focused on pricing-context reads that were shared by Catalog, Pricing, BOM, and Sales pages:

- Batch 18.1 documented the backend read-performance audit and prioritized pricing-context N+1 risks.
- Batch 18.2 replaced per-row component current-price reads on list pages with a list-safe batch lookup.
- Batch 18.3 replaced nested product pricing context reads with batch product price, published BOM, and component price maps, and scoped consumers where applicable.
- Batch 18.5 test-failure triage found the Web test failures were caused by missing ignored ABP client-library artifacts under `wwwroot/libs`, not by a Batch 18 pricing regression.

## 3. Commit list

Named Batch 18 commits:

```text
d143a7b docs: add Batch 18 backend read performance plan
0395b22 fix(pricing): batch load component current prices
a6cc5df fix(pricing): batch product pricing context reads
```

Current branch history also shows:

```text
f396e79 Optimize ProductPricingContextAppService
9c3faab Merge pull request #18 from thangbq190893/refactor/performance
```

## 4. Branch diff summary from main

`git diff --name-status main...HEAD` reports:

- Added files: 29
- Modified files: 44
- Total changed files: 73
- Stat summary: 13,193 insertions, 1,007 deletions

Important scope note: the `main...HEAD` diff is broader than Batch 18 only. It also includes earlier UI plan/summary documents, UAT smoke artifacts, and UI/Razor/CSS changes from prior batches. Batch 18.5 did not modify production code.

Batch 18-specific production/doc areas in the diff include:

- `docs/BACKEND_READ_PERFORMANCE_BATCH_18_PLAN.md`
- `docs/BACKEND_READ_PERFORMANCE_BATCH_18_2_COMPONENT_PRICE_BATCH_LOOKUP.md`
- `docs/BACKEND_READ_PERFORMANCE_BATCH_18_3_PRODUCT_PRICING_CONTEXT_BATCHING.md`
- `src/VPureLux.Application/Pricing/ComponentSuggestedSellingPriceLookupService.cs`
- `src/VPureLux.Application/Pricing/ProductPricingContextAppService.cs`
- `src/VPureLux.Application/Pricing/ProductPricingContextLookupService.cs`
- Pricing and BOM repository interfaces/EF implementations
- Catalog, Pricing, BOM Product, and Sales PageModel consumers
- Focused EF/Web regression tests for pricing-context behavior and source shape

## 5. Audit conclusion from Batch 18.1

The audit found high-confidence read-performance risk in pricing-related list and context pages:

- Catalog Components and Pricing Index called strict component current-price lookup once per listed component.
- Product pricing context used a `1 + P + P + I` read shape: one product list query, one product current-price lookup per product, one BOM lookup per product, and one component current-price lookup per BOM item.
- Catalog Products, Pricing Index, BOM Product, and Sales Create/Edit/Details consumed that shared product pricing context path.
- Inventory/Sales broader read tuning and Audit query/index tuning were real but lower-priority and deferred unless UAT or production evidence shows them as hot paths.

## 6. Batch 18.2 summary

Root cause:

- Catalog Components and Pricing Index loaded component rows, then called strict `GetCurrentAsync` per row.
- Missing current prices were valid for list pages but flowed through exception-based fallback.

Old read shape:

- One component list query.
- Up to one current-price query per listed component.
- Missing-price exceptions handled per row for fallback display.

New batch lookup:

- Added a list-safe component current-price map lookup for requested component IDs and date.
- Components without a current price are omitted from the map and still render the existing fallback.
- Strict single-item APIs remain unchanged.

Files/areas changed:

- Component suggested selling price repository interface and EF implementation.
- `ComponentSuggestedSellingPriceLookupService`.
- Catalog Components PageModel.
- Pricing Index PageModel.
- Focused EF and Web tests.
- Batch 18.2 documentation.

Behavior preserved:

- Strict `GetCurrentAsync` / `GetAtDateAsync` behavior preserved.
- Missing-price fallback behavior preserved.
- Pricing permission behavior preserved.
- Component list display semantics preserved.
- No route, handler, form binding, CSS, JavaScript, database, API, DTO, or business-rule changes.

## 7. Batch 18.3 summary

Root cause:

- `ProductPricingContextAppService.GetListAsync` calculated pricing context by iterating through products, BOMs, and BOM items with nested repository lookups.
- Several pages requested all product contexts and then filtered to the products they already knew they needed.

Old read shape:

```text
1 + P + P + I
```

Where:

- `1` = product query
- `P` = current product suggested price queries
- `P` = BOM version list queries
- `I` = current component suggested price queries for BOM items

New read shape:

- Product query for all products or requested product IDs.
- Batch product price map.
- Batch published BOM map with items.
- Batch component price map for distinct BOM component IDs.
- In-memory composition of the existing DTO shape.

Files/areas changed:

- `ProductPricingContextAppService`.
- `ProductPricingContextLookupService`.
- Product suggested price repository interface and EF implementation.
- BOM version repository interface and EF implementation.
- Catalog Products PageModel.
- Pricing Index continues through the existing app-service contract but benefits from batched internals.
- BOM Product PageModel.
- Sales Create/Edit/Details PageModels.
- Focused EF and Web tests.
- Batch 18.3 documentation.

Consumers covered:

- Catalog Products.
- Pricing Index product context.
- BOM Product.
- Sales Create/Edit initial loads.
- Sales Create/Edit AJAX product-context handlers.
- Sales Details order-line product contexts.

Behavior preserved:

- Product suggested price semantics preserved.
- Published-BOM and no-BOM semantics preserved.
- Missing component suggested price semantics preserved.
- Missing component prices are not treated as zero.
- Component build price and difference calculation semantics preserved.
- Existing DTO fields and meanings preserved.
- Pricing permission behavior preserved.
- Sales, BOM, Inventory, and Audit business/write behavior unchanged.

## 8. What explicitly did not change

- No database schema changes.
- No migrations.
- No indexes.
- No API contract changes.
- No DTO contract changes.
- No business-rule changes.
- No pricing semantic changes.
- Strict `GetCurrentAsync` / `GetAtDateAsync` behavior preserved.
- Missing-price fallback behavior preserved.
- No permission behavior changes.
- No route/handler/form binding changes.
- No UI layout/CSS/JS changes.
- No Inventory/FIFO/posting changes.
- No Sales lifecycle/write-path changes.
- No BOM publish/archive/domain behavior changes.
- No Audit export behavior changes.

## 9. API/DTO/DB/schema/business-rule safety notes

- The batch lookups are internal application/repository read-path improvements, not public contract changes.
- Existing app-service DTOs and meanings are preserved.
- No migrations or schema changes were added.
- No DB indexes were added; any future index proposal remains a separate approval and migration decision.
- The implementation preserves existing date-validity logic for current/at-date price resolution.
- Sales write paths, inventory posting, FIFO allocation, BOM publish/archive behavior, and audit export behavior are outside the Batch 18 changes.

## 10. Permission behavior notes

- Pricing context remains protected by existing pricing permissions.
- Catalog Products checks pricing-context visibility before querying pricing context.
- Sales pages continue to tolerate `AbpAuthorizationException` and hide pricing context when the user cannot view it.
- No permission checks were moved into per-row loops.
- No permission names, policies, grants, or UI action visibility rules were changed.

## 11. Test results

Batch 18.5 test-failure triage was performed on 2026-06-26 from `C:\SourceCode\VpureLux`.

The user reported that build was already passing before this triage. This pass focused on the failing test commands.

```powershell
dotnet test test/VPureLux.EntityFrameworkCore.Tests/VPureLux.EntityFrameworkCore.Tests.csproj --no-build --filter "FullyQualifiedName~Pricing|FullyQualifiedName~Bom" -m:1 --logger "console;verbosity=detailed"
```

Result:

- Passed: 47 passed, 0 failed.

```powershell
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Catalog|FullyQualifiedName~Pricing|FullyQualifiedName~Bom|FullyQualifiedName~Sales" -m:1 --logger "console;verbosity=detailed"
```

Initial result:

- Failed: 34 passed, 30 failed.
- First failing test: `VPureLux.Pages.SalesPagesTests.Sales_Create_Should_Render_Product_Context_Hooks_And_External_Script`.
- Root cause: `Volo.Abp.AbpException : Could not find file '/libs/abp/core/abp.css'`.
- Classification: environment/build artifact issue. `src/VPureLux.Web/wwwroot/libs` was missing, and `.gitignore` explicitly marks `**/wwwroot/libs/` as regenerated ABP client libraries.

Environment repair command:

```powershell
yarn config set ignore-engines true
abp install-libs
```

Rerun result:

- Passed: 64 passed, 0 failed.

Full Web.Tests:

```powershell
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build -m:1 --logger "console;verbosity=detailed"
```

Result:

- Passed: 126 passed, 0 failed.

## 12. Runtime smoke results

Runtime smoke was attempted with:

```powershell
dotnet run --project src/VPureLux.Web/VPureLux.Web.csproj --no-build
```

Result:

- BLOCKED.
- The app could not start from existing binaries.
- Error: `VPureLux.Web.exe` was not found under `src\VPureLux.Web\bin\Debug\net10.0`.

Page smoke status:

| Page | Status | Notes |
|---|---|---|
| `/Catalog/Components` | BLOCKED | Local app could not start. |
| `/Catalog/Products` | BLOCKED | Local app could not start. |
| `/Pricing` | BLOCKED | Local app could not start. |
| `/Bom/Product/{existingProductId}` | BLOCKED | Local app could not start. |
| `/Sales/Create` | BLOCKED | Local app could not start. |
| `/Sales/Edit/{existingOrderId}` | BLOCKED | Local app could not start. |
| `/Sales/Details/{existingOrderId}` | BLOCKED | Local app could not start. |

No authenticated page-load assertions were performed. No runtime exceptions from those pages were observed because the app never started.

## 13. Deferred items

- Inventory Balances/Lots/Ledger read tuning.
- Sales order repository/list projection tuning.
- Audit query/index tuning as a separate DBA/migration decision if ever needed.
- HealthChecks UI SSL noise.
- Catalog Components visual width if not already handled separately.
- Runtime query-count instrumentation if desired later.
- Any DB index proposal as separate approval.

## 14. Suggested PR/MR description text

```text
## Summary

Batch 18 closes the highest-priority backend read-performance findings from the pricing-context audit.

- Added the Batch 18.1 backend read-performance audit and plan.
- Batched component current-price lookup for Catalog Components and Pricing Index list pages.
- Batched product pricing context reads by loading product prices, published BOMs, and component prices as maps.
- Scoped Catalog Products, BOM Product, and Sales Create/Edit/Details consumers to request only the product contexts they need.

## Safety

- No database schema changes, migrations, or indexes.
- No API or DTO contract changes.
- No business-rule, pricing semantic, permission, route, handler, form binding, UI layout, CSS, or JS changes.
- Strict GetCurrent/GetAtDate behavior and missing-price fallback behavior are preserved.
- Inventory/FIFO/posting, Sales lifecycle/write paths, BOM publish/archive/domain behavior, and Audit export behavior are unchanged.

## Validation

Batch 18.5 test-failure triage found the failing Web tests were caused by missing ignored ABP client-library artifacts, not a Batch 18 pricing regression. After running `abp install-libs` from `src/VPureLux.Web`, focused EF tests passed, focused Web tests passed, and full Web.Tests passed. Runtime smoke remains recommended before merge if it has not been completed separately.
```

## 15. Suggested PR/MR checklist

Current Batch 18.5 test status: focused EF, focused Web, and full Web.Tests passed after restoring ignored ABP client-library artifacts. Runtime smoke remains separately recommended before merge if it has not been completed.

```text
- [x] Batch 18.1 backend read-performance audit completed
- [x] Batch 18.2 component current price N+1 removed
- [x] Batch 18.3 product pricing context N+1 removed
- [x] Catalog Components price lookup batched
- [x] Pricing Index component price lookup batched
- [x] Product pricing context product prices batched
- [x] Product pricing context published BOM lookup batched
- [x] Product pricing context component prices batched
- [x] Catalog Products consumer scoped
- [x] BOM Product consumer scoped
- [x] Sales Create/Edit/Details consumers scoped
- [x] Strict GetCurrent/GetAtDate behavior preserved
- [x] Missing-price fallback preserved
- [x] Pricing permission behavior preserved
- [x] DTO/API contracts unchanged
- [x] No DB schema/migration/index changes
- [x] No business-rule changes
- [x] Focused EF tests passed in Batch 18.5
- [x] Focused Web tests passed in Batch 18.5
- [x] Full Web.Tests passed in Batch 18.5
- [ ] Runtime smoke not performed in this batch; recommended before merge
```

## 16. Batch 18.5 commit status

No Batch 18.5 commit has been created yet in this triage pass.
