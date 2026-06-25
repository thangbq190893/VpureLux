# Batch 18.2 - Catalog/Pricing Current Component Price Batch Lookup Prompt

## Purpose

Use this prompt with **Codex Agent** to implement **Batch 18.2** for VPureLux.

This batch removes the confirmed N+1 current component suggested price lookup from:

- Catalog Components list
- Pricing Index component tab

The batch must preserve all existing pricing semantics, missing-price fallback behavior, strict `GetCurrentAsync` behavior, permissions, routes, UI layout, and database schema.

---

## Recommended Codex Settings

Use:

```text
Model: GPT-5.5
Reasoning: High
Speed: Standard
```

Do **not** use Fast mode for this backend/performance fix.

---

## Short Prompt to Paste into Codex

If this file is added to the repository as:

```text
docs/BATCH_18_2_COMPONENT_PRICE_BATCH_LOOKUP_PROMPT.md
```

paste this short instruction into Codex:

```text
Read and execute the instruction file:

docs/BATCH_18_2_COMPONENT_PRICE_BATCH_LOOKUP_PROMPT.md

Follow it strictly.

This is Batch 18.2 only:
- Remove N+1 current component suggested price lookups from Catalog Components and Pricing Index.
- Add a list-safe nullable batch current component price lookup.
- Preserve strict GetCurrentAsync/GetAtDateAsync behavior.
- Do not change ProductPricingContextAppService in this batch.
- Do not change UI layout/CSS/JS/database schema/routes/permissions/pricing semantics.
- Commit only if build and tests pass.
- Do not push.
```

---

## Full Prompt

Owner: Codex Agent

Task: Batch 18.2 - Catalog/Pricing current component price batch lookup.

This is a targeted backend read-performance fix.

Do NOT change UI layout.
Do NOT change CSS.
Do NOT change JavaScript.
Do NOT change database schema.
Do NOT add migrations.
Do NOT add indexes.
Do NOT change pricing semantics.
Do NOT change inventory behavior.
Do NOT change sales behavior.
Do NOT change BOM behavior.
Do NOT change API behavior of strict single-item methods.
Do NOT make existing `GetCurrentAsync` or `GetAtDateAsync` nullable if they currently throw for missing versions.
Do NOT change public DTO fields unless strictly necessary and approved.
Do NOT remove permission checks.
Do NOT hide columns.
Do NOT change routes, handlers, form fields, links, or action menus.
Do NOT implement ProductPricingContext batching in this batch.
Do NOT optimize Inventory/Sales/BOM in this batch.
Do NOT push unless explicitly told.

## Context

Batch 18.1 audit was committed as:

```text
d143a7 docs: add Batch 18 backend read performance plan
```

Audit findings:

- Catalog Components list calls `IComponentSuggestedSellingPriceAppService.GetCurrentAsync` once per listed component.
- Pricing Index component tab repeats the same per-component current-price lookup.
- Missing current price is valid for list pages and must render fallback text.
- Existing strict `GetCurrentAsync` behavior must remain unchanged for strict API/detail callers.
- Product pricing context batching is a separate Batch 18.3 and must not be implemented here.

Current branch:

```text
refactor/performance
```

Primary target:

Remove N+1 current component suggested selling price lookups from:

```text
src/VPureLux.Web/Pages/Catalog/Components/Index.cshtml.cs
src/VPureLux.Web/Pages/Pricing/Index.cshtml.cs
```

## Files to Inspect

Inspect these before editing:

```text
docs/BACKEND_READ_PERFORMANCE_BATCH_18_PLAN.md
src/VPureLux.Web/Pages/Catalog/Components/Index.cshtml.cs
src/VPureLux.Web/Pages/Catalog/Components/Index.cshtml
src/VPureLux.Web/Pages/Pricing/Index.cshtml.cs
src/VPureLux.Web/Pages/Pricing/Index.cshtml
src/VPureLux.Application/Pricing/ComponentSuggestedSellingPriceAppService.cs
src/VPureLux.Application.Contracts/Pricing/*
src/VPureLux.Domain/Pricing/*
src/VPureLux.EntityFrameworkCore/Pricing/*
test/VPureLux.Web.Tests/Pages/CatalogPagesTests.cs
test/VPureLux.Web.Tests/Pages/PricingPagesTests.cs
test/VPureLux.Application.Tests/Pricing/*
test/VPureLux.EntityFrameworkCore.Tests/EntityFrameworkCore/Pricing/*
```

Also inspect repository interfaces/implementations used by component suggested selling price versions.

## Goal

Introduce a list-safe batch lookup for current component suggested selling prices and use it in:

1. Catalog Components list
2. Pricing Index component tab

## Expected Behavior

1. A component with a current suggested selling price shows the same price as before.
2. A component without current suggested selling price shows the same fallback text as before.
3. Catalog Components page no longer calls current price service once per component.
4. Pricing Index component tab no longer calls current price service once per component.
5. `GetCurrentAsync` and `GetAtDateAsync` still throw the same domain/business exception for strict callers when no version exists.
6. No database schema, migration, route, UI, permission, or pricing semantic changes.

## Implementation Requirements

1. Read `docs/BACKEND_READ_PERFORMANCE_BATCH_18_PLAN.md`.
2. Identify the exact current N+1 call path in both target PageModels.
3. Add a nullable batch read path for current component prices:
   - input: collection of `ComponentId`
   - input: effective/current date, using the same date semantics as existing `GetCurrentAsync`
   - output: dictionary keyed by `ComponentId`
   - missing current price: no dictionary entry or nullable value, not exception
4. Place the batch read path in the correct layer:
   - prefer Application service/repository/query service
   - do not put complex EF query logic directly in Razor PageModel
   - preserve ABP layering
5. The batch query must select the same current version that existing `GetCurrentAsync` would choose for each component.
6. If multiple versions are valid, ordering must match existing single-item logic.
7. Preserve existing permission behavior:
   - if current user cannot view pricing context/current price, do not query prices and do not display prices.
8. Preserve existing fallback localization/text for missing price.
9. Do not return `0` for missing price.
10. Do not create price records automatically.
11. Do not catch all exceptions broadly.
12. Remove the per-row `GetCurrentAsync` list usage from Catalog Components and Pricing Index.
13. Keep `ProductPricingContextAppService` unchanged in this batch.
14. Check compile errors across `Application.Contracts`, `Application`, and `Web` caused by new method signatures.
15. Add or update tests.

## Recommended Design

Prefer adding a method like one of these, depending on existing project conventions:

```csharp
Task<IReadOnlyDictionary<Guid, ComponentSuggestedSellingPriceDto>> FindCurrentMapAsync(
    IReadOnlyCollection<Guid> componentIds,
    DateTime pricingDate
);
```

or:

```csharp
Task<Dictionary<Guid, ComponentSuggestedSellingPriceDto>> GetCurrentMapAsync(
    IReadOnlyCollection<Guid> componentIds,
    DateTime pricingDate
);
```

Use a nullable/list-safe method name if project convention prefers `Find*` for missing data.

Important:

- Do not change strict `GetCurrentAsync`.
- Do not change strict `GetAtDateAsync`.
- Do not change existing API behavior.
- Batch/list callers should use the new nullable map.
- Strict detail/API callers should continue using the strict method.

## Query Semantics

The batch method must match existing single-item current-price selection semantics.

For each component:

1. Filter to the requested component IDs.
2. Filter to versions valid/current at the same effective date used by `GetCurrentAsync`.
3. Choose the same winning version as the existing single-item query.
4. Return no entry if no current version exists.

Do not infer new business semantics.

If the existing single-item logic orders by effective date, version number, creation time, or another field, reuse that exact ordering.

## Test Requirements

Add or update tests to cover:

1. Catalog Components list renders a component with no current suggested price.
2. Catalog Components list renders a component with a current suggested price.
3. Pricing Index component tab renders components with and without current suggested price.
4. Missing price fallback remains unchanged.
5. Existing strict `GetCurrentAsync` missing-price behavior still throws the same error/code.
6. If practical, add a source-shape or mock interaction test to prevent PageModels from calling `GetCurrentAsync` per component again.

Prefer behavior tests plus one source-shape/performance-shape assertion if the project already uses this style.

Do not add brittle tests unless necessary.

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
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Catalog|FullyQualifiedName~Pricing" -m:1
```

If Application tests were added or changed, run the relevant Application test project too.

If focused tests pass, run:

```powershell
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build -m:1
```

## Documentation

Create or update:

```text
docs/BACKEND_READ_PERFORMANCE_BATCH_18_2_COMPONENT_PRICE_BATCH_LOOKUP.md
```

The document must include:

1. Root cause
2. Files changed
3. Batch lookup design
4. Behavior preserved
5. Strict API behavior preserved
6. Tests added/updated
7. Validation results
8. Deferred items, especially ProductPricingContext batching for Batch 18.3

## Commit

If validation passes, commit:

```bash
git add .
git commit -m "fix(pricing): batch load component current prices"
```

Do NOT push.

## Final Output

Return:

1. Summary
2. Root cause of N+1
3. Files changed
4. Batch lookup implementation summary
5. Catalog Components changes
6. Pricing Index changes
7. Existing strict `GetCurrentAsync` behavior preserved? yes/no
8. Missing price fallback preserved? yes/no
9. Pricing permission behavior preserved? yes/no
10. ProductPricingContext changed? yes/no
11. Tests added/updated
12. Build result
13. Focused test result
14. Full Web.Tests result
15. Commit hash, if committed
16. Git status after
17. Risky API/DB/schema/business changes? yes/no

---

## Reviewer Notes

Batch 18.2 must stay narrow.

Do not accept the result if it changes:

- ProductPricingContextAppService
- Sales pages/services
- BOM services
- Inventory services
- Razor layout/CSS
- database schema/migrations
- strict single-item pricing API behavior

Those belong to later batches.
