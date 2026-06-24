# Batch 18.3 - Product Pricing Context Batching

## 1. Root cause

`ProductPricingContextAppService.GetListAsync` built product pricing contexts by loading products first, then performing nested lookups:

- one current product suggested price lookup per product
- one BOM version list lookup per product, then selecting the published BOM in memory
- one current component suggested price lookup per BOM item

That shared read path is used by Catalog Products, Pricing Index, BOM Product, and Sales product context pages, so the same N+1 shape could appear in several user workflows.

## 2. Files changed

- `src/VPureLux.Application/Pricing/ProductPricingContextAppService.cs`
- `src/VPureLux.Application/Pricing/ProductPricingContextLookupService.cs`
- `src/VPureLux.Domain/Pricing/IProductSuggestedPriceVersionRepository.cs`
- `src/VPureLux.EntityFrameworkCore/Pricing/EfCoreProductSuggestedPriceVersionRepository.cs`
- `src/VPureLux.Domain/Bom/IBomVersionRepository.cs`
- `src/VPureLux.EntityFrameworkCore/Bom/EfCoreBomVersionRepository.cs`
- `src/VPureLux.Web/Pages/Catalog/Products/Index.cshtml.cs`
- `src/VPureLux.Web/Pages/Bom/Product.cshtml.cs`
- `src/VPureLux.Web/Pages/Sales/Create.cshtml.cs`
- `src/VPureLux.Web/Pages/Sales/Edit.cshtml.cs`
- `src/VPureLux.Web/Pages/Sales/Details.cshtml.cs`
- `test/VPureLux.EntityFrameworkCore.Tests/EntityFrameworkCore/Pricing/PricingAppServiceTests.cs`
- `test/VPureLux.Web.Tests/Pages/CatalogPageModelPermissionTests.cs`
- `test/VPureLux.Web.Tests/Pages/CatalogPagesTests.cs`
- `test/VPureLux.Web.Tests/Pages/PricingPagesTests.cs`
- `test/VPureLux.Web.Tests/Pages/BomPagesTests.cs`
- `test/VPureLux.Web.Tests/Pages/SalesPagesTests.cs`
- `docs/BACKEND_READ_PERFORMANCE_BATCH_18_3_PRODUCT_PRICING_CONTEXT_BATCHING.md`

## 3. Old read shape

For `P` products and `I` total BOM items:

- 1 product list query
- `P` product suggested price queries
- `P` BOM version list queries
- `I` component suggested price queries

Consumers such as Catalog Products, BOM Product, and Sales Details also requested all product contexts and filtered them after the full context graph was calculated.

## 4. New batch read shape

For the same context request:

- 1 product query for either all products or requested product IDs
- 1 product suggested price map query
- 1 published BOM map query
- 1 component suggested price map query for distinct BOM component IDs

Product-ID-scoped consumers now request only the product IDs they already need.

## 5. Product price batch lookup design

`IProductSuggestedPriceVersionRepository.FindAtDateMapAsync(productIds, date)` applies the same date-validity predicate as strict single-product `FindAtDateAsync`:

- product ID is in the requested set
- `EffectiveFrom <= date`
- `EffectiveTo` is null or later than the date

Missing product prices are omitted from the map, preserving existing `CurrentProductSuggestedPrice = null` behavior. Strict `GetCurrentAsync` and `GetAtDateAsync` were not changed.

## 6. BOM batch lookup design

`IBomVersionRepository.GetPublishedMapByProductIdsAsync(productIds)` loads only published BOM versions with items for the requested products. It orders by version number descending before grouping, matching the previous `GetListByProductIdAsync(...).FirstOrDefault(x => x.Status == Published)` selection shape if multiple published candidates ever exist.

Products without a published BOM are omitted from the map and still produce `HasPublishedBom = false` with null build price.

## 7. Component price map reuse

`ProductPricingContextLookupService` reuses the Batch 18.2 `IComponentSuggestedSellingPriceLookupService.FindCurrentMapAsync` for the distinct component IDs found in published BOMs.

Missing component prices still set `HasMissingComponentSuggestedPrices = true` and leave `ComponentBuildPrice` null. Missing component prices are not treated as zero.

## 8. Consumer changes

- `ProductPricingContextAppService.GetListAsync` now delegates to the batched lookup while preserving its existing public contract.
- Catalog Products now requests contexts only for products in the current list page.
- BOM Product now requests context only for the current product.
- Sales Create/Edit initial loads request contexts for the loaded product option set.
- Sales Create/Edit AJAX product-context handlers request only the requested product.
- Sales Details requests contexts only for product IDs on the order lines.
- Pricing Index product tab still calls the existing all-products app-service method, but that method now uses batch reads internally.

## 9. Behavior preserved

- Product suggested price, no-product-price, published-BOM, no-BOM, missing-component-price, component build price, and difference semantics are unchanged.
- Existing DTO fields and meanings are unchanged.
- No Razor layout, CSS, JavaScript, routes, form fields, links, action menus, database schema, migrations, or indexes were changed.
- Sales, BOM, and Inventory business/write behavior was not changed.

## 10. Permission behavior preserved

The product pricing context lookup checks `Pricing.View` before returning data. This preserves the previous behavior of `ProductPricingContextAppService`, including Sales pages catching `AbpAuthorizationException` and hiding product context when pricing context is unavailable.

Catalog Products still checks `CanViewPricingContext` before querying pricing context.

## 11. Tests added/updated

- EF pricing test covers scoped product context map returning only requested products.
- Existing EF pricing tests continue to cover no published BOM, missing component prices, build price, product price, and difference.
- Source-shape tests guard against per-product/per-BOM-item lookups returning inside product pricing context.
- Catalog Products, BOM Product, and Sales PageModel source-shape tests verify scoped product context calls.
- Existing Catalog, Pricing, BOM, and Sales page behavior tests continue to cover visible values and placeholders.

## 12. Validation results

Passed:

```powershell
dotnet build VPureLux.slnx --no-restore -m:2
```

Passed:

```powershell
dotnet test test/VPureLux.EntityFrameworkCore.Tests/VPureLux.EntityFrameworkCore.Tests.csproj --no-build --filter "FullyQualifiedName~Pricing|FullyQualifiedName~Bom" -m:1
```

Result: 47 passed, 0 failed.

Passed:

```powershell
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Catalog|FullyQualifiedName~Pricing|FullyQualifiedName~Bom|FullyQualifiedName~Sales" -m:1
```

Result: 64 passed, 0 failed.

Passed:

```powershell
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build -m:1
```

Result: 126 passed, 0 failed.

## 13. Deferred items

Deferred to Batch 18.4 only if still justified:

- Inventory Balances/Lots/Ledger read tuning.
- Sales order repository include/projection tuning.
- Any DBA/index decisions.

No inventory posting/FIFO, sales lifecycle, BOM publish/archive, audit export, schema, migration, route, or UI layout changes were made in this batch.
