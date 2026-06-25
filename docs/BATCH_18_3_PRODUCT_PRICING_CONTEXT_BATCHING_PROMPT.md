# Batch 18.3 - Product Pricing Context Batch Optimization Prompt

## Purpose

Use this prompt with **Codex Agent** to implement **Batch 18.3** for VPureLux.

This batch optimizes the shared product pricing context read path by replacing per-product and per-BOM-item lookups with bounded batch queries.

Affected hot path:

- `ProductPricingContextAppService.GetListAsync`
- Catalog Products list
- Pricing Index product tab / product pricing context
- BOM Product pricing context
- Sales Create/Edit/Details product context usage

This is a backend/read-performance batch. It must preserve business behavior, DTO semantics, permissions, routes, UI, database schema, and strict pricing semantics.

---

## Recommended Codex Settings

Use:

```text
Model: GPT-5.5
Reasoning: Extra High
Speed: Standard
```

If credit is limited, use:

```text
Model: GPT-5.5
Reasoning: High
Speed: Standard
```

Do **not** use Fast mode for this batch.

---

## Short Prompt to Paste into Codex

If this file is added to the repository as:

```text
docs/BATCH_18_3_PRODUCT_PRICING_CONTEXT_BATCHING_PROMPT.md
```

paste this short instruction into Codex:

```text
Read and execute the instruction file:

docs/BATCH_18_3_PRODUCT_PRICING_CONTEXT_BATCHING_PROMPT.md

Follow it strictly.

This is Batch 18.3 only:
- Optimize ProductPricingContextAppService read path by batching product price, published BOM, and component price lookups.
- Preserve existing DTO contracts, pricing semantics, route/UI behavior, permissions, and missing-price/no-BOM behavior.
- Do not change database schema/migrations/indexes.
- Do not change Sales/BOM/Inventory business logic.
- Do not implement Inventory/Sales read tuning outside product pricing context usage.
- Commit only if build and tests pass.
- Do not push.
```

---

## Full Prompt

Owner: Codex Agent

Task: Batch 18.3 - Product pricing context batch optimization.

This is a targeted backend read-performance fix.

Do NOT change UI layout.
Do NOT change Razor markup unless absolutely necessary to consume the same data more efficiently.
Do NOT change CSS.
Do NOT change JavaScript.
Do NOT change database schema.
Do NOT add migrations.
Do NOT add indexes.
Do NOT change pricing semantics.
Do NOT change BOM publish/archive/domain behavior.
Do NOT change inventory posting/FIFO behavior.
Do NOT change sales order lifecycle behavior.
Do NOT change sales revenue/cost/profit calculations.
Do NOT change API route behavior.
Do NOT change public DTO fields unless strictly necessary and approved.
Do NOT remove permission checks.
Do NOT hide columns.
Do NOT change routes, handlers, form fields, links, action menus, or table columns.
Do NOT optimize Inventory ledger/balances/lots in this batch.
Do NOT optimize Sales order repository wide includes in this batch.
Do NOT push unless explicitly told.

## Context

Batch 18.1 audit found high-priority N+1/read-shape risk in the shared product pricing context path.

Batch 18.2 already handled component current price batching for:
- Catalog Components
- Pricing Index component tab

Batch 18.3 must now target the broader shared path:

```text
src/VPureLux.Application/Pricing/ProductPricingContextAppService.cs
```

Known current risk:

- Loads product list.
- For each product, looks up current product suggested price.
- For each product, loads BOM versions/items and selects published/current context.
- For each BOM item, looks up current component suggested price.
- This can affect Catalog Products, Pricing product context, BOM Product, and Sales Create/Edit/Details.

Current branch:

```text
refactor/performance
```

## Primary Goal

Optimize `ProductPricingContextAppService` so product pricing contexts are built using bounded batch reads instead of per-product/per-BOM-item queries.

## Expected Behavior

Preserve all existing behavior:

1. Product with current suggested price shows the same price as before.
2. Product without current suggested price shows the same null/missing-price behavior as before.
3. Product with published BOM shows the same BOM context as before.
4. Product without published BOM shows the same no-BOM/missing-BOM behavior as before.
5. BOM component suggested prices are calculated the same as before.
6. Missing component price inside BOM context behaves the same as before.
7. Difference/current price/build price fields keep the same semantics.
8. Sales pages that consume product pricing context keep the same visible values.
9. Catalog Products and Pricing product tab keep the same visible values.
10. BOM Product pricing context keeps the same visible values.
11. Permission behavior is preserved.
12. No API/schema/migration/business-rule changes.

## Files to Inspect

Inspect before editing:

```text
docs/BACKEND_READ_PERFORMANCE_BATCH_18_PLAN.md
docs/BACKEND_READ_PERFORMANCE_BATCH_18_2_COMPONENT_PRICE_BATCH_LOOKUP.md
src/VPureLux.Application/Pricing/ProductPricingContextAppService.cs
src/VPureLux.Application/Pricing/ComponentSuggestedSellingPriceLookupService.cs
src/VPureLux.Application/Pricing/ComponentSuggestedSellingPriceAppService.cs
src/VPureLux.Application/Pricing/ProductSuggestedSellingPriceAppService.cs
src/VPureLux.Application.Contracts/Pricing/*
src/VPureLux.Domain/Pricing/*
src/VPureLux.Domain/Bom/*
src/VPureLux.EntityFrameworkCore/Pricing/*
src/VPureLux.EntityFrameworkCore/Bom/*
src/VPureLux.Web/Pages/Catalog/Products/Index.cshtml.cs
src/VPureLux.Web/Pages/Pricing/Index.cshtml.cs
src/VPureLux.Web/Pages/Bom/Product.cshtml.cs
src/VPureLux.Web/Pages/Sales/Create.cshtml.cs
src/VPureLux.Web/Pages/Sales/Edit.cshtml.cs
src/VPureLux.Web/Pages/Sales/Details.cshtml.cs
test/VPureLux.EntityFrameworkCore.Tests/EntityFrameworkCore/Pricing/PricingAppServiceTests.cs
test/VPureLux.Web.Tests/Pages/CatalogPagesTests.cs
test/VPureLux.Web.Tests/Pages/PricingPagesTests.cs
test/VPureLux.Web.Tests/Pages/BomPagesTests.cs
test/VPureLux.Web.Tests/Pages/SalesPagesTests.cs
```

Also inspect relevant repository interfaces and implementations for:
- product suggested price versions
- component suggested price versions
- BOM versions and BOM items
- product repositories

## Implementation Requirements

### 1. Confirm current read shape

Identify exact current calls in `ProductPricingContextAppService.GetListAsync` and related methods.

Document internally while coding:

- product query count
- per-product product price lookup
- per-product BOM lookup
- per-BOM-item component price lookup

### 2. Add batch product current price lookup

Add a nullable/list-safe batch lookup for current product suggested prices.

Requirements:

- input: product IDs and effective/current date
- output: dictionary keyed by product ID
- missing current price: omitted/null, not exception
- same date validity predicate as existing strict single-item product current price lookup
- same winning-version ordering as existing single-item lookup
- strict product `GetCurrentAsync` / `GetAtDateAsync` behavior must remain unchanged

Prefer repository/application query service placement, not Web PageModel logic.

### 3. Reuse component batch lookup from Batch 18.2

Use the Batch 18.2 component price map for distinct BOM component IDs.

Requirements:

- do not call component `GetCurrentAsync` per BOM item
- do not return zero for missing component price unless existing context semantics already do that
- preserve existing missing component price behavior exactly

### 4. Add batch published BOM lookup

Add a batch read path for published/current BOM versions by product IDs.

Requirements:

- input: product IDs
- output: dictionary keyed by product ID, with published BOM version and items needed for context calculation
- do not load all BOM versions per product if only published/current version is needed
- preserve existing definition of “published BOM”
- preserve existing winning published version selection semantics if more than one candidate can exist
- include BOM items needed for build-price/context calculation
- preserve product-without-published-BOM behavior

Place this in the correct layer:
- repository/query implementation if it is data-access specific
- application service composition if it combines pricing/BOM/product context

Do not put complex EF logic in Razor PageModels.

### 5. Product-ID-scoped pricing context

If current `ProductPricingContextAppService.GetListAsync` always computes all products, add a bounded/internal method or overload that accepts product IDs or filter input.

Use it in consumers where appropriate:

- Catalog Products should compute contexts only for products on the current list page.
- Sales Details should compute contexts only for order-line product IDs.
- Sales AJAX/product context handlers should compute one product or a bounded set, not all products.
- BOM Product should compute only the current product if possible.
- Pricing Index product tab should remain behaviorally identical while using batch reads.

Do not change public API/DTO contracts unless already existing patterns support it. If a new Application Contract method is necessary, keep it additive and backward-compatible.

### 6. Preserve permissions

Preserve existing authorization behavior:

- If caller is not allowed to view pricing context, behavior must remain the same.
- Do not move permission checks into loops.
- Do not remove or weaken `[Authorize]`/policy checks.
- Sales pages that catch `AbpAuthorizationException` must continue to behave the same.

### 7. Preserve DTO semantics

Do not change DTO field names or meanings.

Preserve:

- current suggested price fields
- build/component cost fields
- difference fields
- missing price/null behavior
- no-BOM behavior
- published BOM labels/status
- component price contribution semantics
- date/money/number formatting indirectly by preserving values/types

### 8. Avoid broad scope

Do not optimize these in this batch:

- Inventory Balances/Lots/Ledger
- Sales order repository includes
- Sales write path/FIFO
- Sales order confirmation
- Inventory posting
- Audit export
- UI layout
- DB indexes

## Test Requirements

Add or update tests to cover behavior and read-shape protection.

### Functional behavior tests

Cover:

1. Product context for product with current suggested product price.
2. Product context for product without current suggested product price.
3. Product context for product with published BOM and component prices.
4. Product context for product with published BOM and missing component price.
5. Product context for product without published BOM.
6. Catalog Products list still renders pricing context values.
7. Pricing Index product tab still renders pricing context values.
8. Sales Details/Create/Edit product context behavior remains unchanged if existing tests cover it.
9. BOM Product context behavior remains unchanged if existing tests cover it.

### Performance-shape/source-shape tests

Add tests where practical to prevent regression:

1. `ProductPricingContextAppService` should not call per-product current price lookup.
2. `ProductPricingContextAppService` should not call per-product BOM list lookup.
3. `ProductPricingContextAppService` should not call per-BOM-item component price lookup.
4. Consumers should use product-ID-scoped context where practical.

Prefer behavior tests and source-shape tests only if the project already uses source-shape tests in Web tests.

Do not introduce fragile tests that break on harmless formatting changes.

## Validation

Run cleanup first:

```powershell
dotnet build-server shutdown

Get-Process testhost -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process dotnet -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process VBCSCompiler -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process MSBuild -ErrorAction SilentlyContinue | Stop-Process -Force
```

Do NOT kill Cursor, SQL Server, or Redis.

Then run:

```powershell
git status
git diff --name-only
dotnet build VPureLux.slnx --no-restore -m:2
```

Focused tests:

```powershell
dotnet test test/VPureLux.EntityFrameworkCore.Tests/VPureLux.EntityFrameworkCore.Tests.csproj --no-build --filter "FullyQualifiedName~Pricing|FullyQualifiedName~Bom" -m:1
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Catalog|FullyQualifiedName~Pricing|FullyQualifiedName~Bom|FullyQualifiedName~Sales" -m:1
```

If Application tests are changed or added, run the relevant Application test project too.

If focused tests pass, run:

```powershell
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build -m:1
```

## Runtime smoke recommendation

If build/tests pass and app can be run locally, smoke these pages manually or via authenticated HTTP:

```text
/Catalog/Products
/Pricing
/Bom/Product/{existingProductId}
/Sales/Create
/Sales/Details/{existingOrderId}
```

Confirm:

- page loads
- visible pricing context remains consistent
- missing-price/no-BOM placeholders remain correct
- no runtime exceptions

Do not record video.

## Documentation

Create or update:

```text
docs/BACKEND_READ_PERFORMANCE_BATCH_18_3_PRODUCT_PRICING_CONTEXT_BATCHING.md
```

The document must include:

1. Root cause
2. Files changed
3. Old read shape
4. New batch read shape
5. Product price batch lookup design
6. BOM batch lookup design
7. Component price map reuse
8. Consumer changes
9. Behavior preserved
10. Permission behavior preserved
11. Tests added/updated
12. Validation results
13. Deferred items, especially Inventory/Sales repository read tuning for Batch 18.4 if still justified

## Commit

If validation passes, commit:

```bash
git add .
git commit -m "fix(pricing): batch product pricing context reads"
```

Do NOT push.

## Final Output

Return:

1. Summary
2. Root cause of ProductPricingContext N+1
3. Files changed
4. Old read shape
5. New batch read shape
6. Product price batch lookup summary
7. BOM batch lookup summary
8. Component price map reuse summary
9. Consumer changes
10. Behavior preserved? yes/no
11. Permission behavior preserved? yes/no
12. DTO/API contracts changed? yes/no
13. ProductPricingContext page consumers covered
14. Tests added/updated
15. Build result
16. Focused test results
17. Full Web.Tests result
18. Runtime smoke result if performed
19. Commit hash, if committed
20. Git status after
21. Risky API/DB/schema/business changes? yes/no

---

## Reviewer Notes

Reject or ask for revision if this batch changes:

- database schema/migrations
- pricing semantics
- sales lifecycle/write path
- inventory/FIFO behavior
- broad UI layout
- unrelated Inventory/Audit/Sales repository optimization
- strict single-item pricing API behavior

Batch 18.3 should optimize read shape only.
