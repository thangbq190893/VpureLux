# Batch 18.2 - Component Current Price Batch Lookup

## 1. Root cause

Catalog Components and Pricing Index both loaded component rows first, then called strict `GetCurrentAsync` once per component to find the current suggested selling price. Components without a current price are valid on these list pages, so each missing price also flowed through exception handling before rendering fallback text.

This produced an N+1 read shape:

- one component list query
- up to one current-price query per listed component
- exception-based fallback for missing prices

## 2. Files changed

- `src/VPureLux.Domain/Pricing/IComponentSuggestedSellingPriceVersionRepository.cs`
- `src/VPureLux.EntityFrameworkCore/Pricing/EfCoreComponentSuggestedSellingPriceVersionRepository.cs`
- `src/VPureLux.Application/Pricing/ComponentSuggestedSellingPriceLookupService.cs`
- `src/VPureLux.Web/Pages/Catalog/Components/Index.cshtml.cs`
- `src/VPureLux.Web/Pages/Pricing/Index.cshtml.cs`
- `test/VPureLux.EntityFrameworkCore.Tests/EntityFrameworkCore/Pricing/PricingAppServiceTests.cs`
- `test/VPureLux.Web.Tests/Pages/CatalogPageModelPermissionTests.cs`
- `test/VPureLux.Web.Tests/Pages/CatalogPagesTests.cs`
- `test/VPureLux.Web.Tests/Pages/PricingPagesTests.cs`
- `docs/BACKEND_READ_PERFORMANCE_BATCH_18_2_COMPONENT_PRICE_BATCH_LOOKUP.md`

## 3. Batch lookup design

The repository now exposes `FindAtDateMapAsync(componentIds, date)`, which applies the same date-validity predicate used by the strict single-item `FindAtDateAsync` query:

- component ID is in the requested ID set
- `EffectiveFrom <= date`
- `EffectiveTo` is null or later than the date

It returns a dictionary keyed by `ComponentId`. Components without a current version are omitted.

The Web PageModels use `IComponentSuggestedSellingPriceLookupService`, an application-layer query service, rather than adding a new public app-service/API contract method. This keeps the batch lookup inside the application layer without changing routes or strict API behavior.

## 4. Behavior preserved

- Current component prices still render with the same values.
- Components without current suggested prices still render the existing fallback text.
- Catalog Components still checks pricing permission before querying current prices.
- Pricing Index still requires `Pricing.View`.
- UI layout, Razor markup, CSS, JavaScript, routes, permissions, database schema, and pricing semantics were not changed.

## 5. Strict API behavior preserved

`IComponentSuggestedSellingPriceAppService.GetCurrentAsync` and `GetAtDateAsync` were not changed. They still validate the component and throw `PRICE_003` / `PriceVersionNotFound` when no matching version exists.

The nullable behavior is limited to the list-safe lookup service used by list PageModels.

## 6. Tests added/updated

- Catalog Components page test now covers both a component with a current price and a component without a current price.
- Pricing Index component tab test now covers priced and unpriced components together.
- EF pricing test covers `FindCurrentMapAsync` returning the current price and omitting missing IDs.
- Source-shape tests assert the Catalog Components and Pricing Index PageModels use `FindCurrentMapAsync` and do not call `GetCurrentAsync` or the old per-row helper.
- Existing strict missing-price test remains in place for `GetCurrentAsync`.

## 7. Validation results

Initial build attempt compiled projects but failed copying Web binaries because IIS Express had `src/VPureLux.Web/bin/Debug/net10.0` files locked. IIS Express was stopped and the required build command was rerun.

Passed:

```powershell
dotnet build VPureLux.slnx --no-restore -m:2
```

Passed:

```powershell
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Catalog|FullyQualifiedName~Pricing" -m:1
```

Result: 37 passed, 0 failed.

Passed because an EF pricing test was updated:

```powershell
dotnet test test/VPureLux.EntityFrameworkCore.Tests/VPureLux.EntityFrameworkCore.Tests.csproj --no-build --filter "FullyQualifiedName~Pricing" -m:1
```

Result: 23 passed, 0 failed.

Passed:

```powershell
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build -m:1
```

Result: 122 passed, 0 failed.

## 8. Deferred items

Product pricing context batching remains deferred to Batch 18.3. This batch intentionally did not change `ProductPricingContextAppService`, Sales, BOM, Inventory, database indexes, migrations, routes, or API contracts.
