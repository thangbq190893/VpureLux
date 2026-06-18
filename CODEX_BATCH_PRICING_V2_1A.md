# Codex Batch: Pricing V2 Realignment 1A

## Goal

Replace the obsolete Component Purchase Price concept with Component Suggested Selling Price.

## Read First

* `CODEX_README_VPURELUX_V2.md`
* `BUSINESS_ARCHITECTURE_DECISIONS_V2.md`
* `MODULE_SPECIFICATIONS_V2.md`
* `UI_UX_ABP_GUIDE_V2.md`
* `IMPLEMENTATION_ROADMAP_V2.md`
* `V2_ARCHITECTURE_ALIGNMENT_REPORT.md`

## Approved Business Decisions

* Project is still in development.
* No real production data exists.
* Clean backend refactor is allowed.
* Existing `ComponentPurchasePriceVersion` data is obsolete dev data.
* Do not migrate old component purchase price data.
* Do not preserve the old “giá mua linh kiện” business concept.
* Replace it with Component Suggested Selling Price.
* Sales V2 is not part of this batch.
* Product Component Build Price / `Giá cấu thành linh kiện` is not part of this batch.

## Target Concept

Technical name:

`ComponentSuggestedSellingPriceVersion`

Vietnamese UI name:

`Giá bán đề xuất linh kiện`

Meaning:

Suggested customer selling price for a component.

It is not:

* actual purchase cost
* inventory receipt UnitCost
* FIFO cost
* product price
* profit

## Replace Old Terms

Remove/replace old business naming:

* `ComponentPurchasePriceVersion`
* `ComponentPurchasePrice`
* `PurchasePrice`
* `Component Purchase Price`
* `Giá mua linh kiện`
* component purchase price semantics in Pricing

Historical migration filenames/comments may remain only if unavoidable. Report any remaining old references.

## Allowed Scope

* Domain Pricing aggregate/repository/events
* Application.Contracts Pricing DTOs/interfaces/permissions
* Application Pricing services
* EF Core mapping/repository/migrations
* HttpApi controller/routes
* Web Pricing pages
* Localization
* Audit event handling
* Tests
* Docs only if needed

## Forbidden Scope

* Do not refactor Sales.
* Do not remove direct Component sales.
* Do not implement Product/SKU-only Sales.
* Do not implement `Giá cấu thành linh kiện`.
* Do not calculate BOM-based prices in Razor or JavaScript.
* Do not change Inventory Receipt UnitCost behavior.
* Do not change ProductSuggestedPriceVersion semantics except references needed for Pricing page consistency.
* Do not change Catalog/BOM behavior except compile-time references if required.

## Required Implementation

1. Refactor the old component purchase price model to Component Suggested Selling Price across all layers.

2. Keep the same price-version behavior:

   * create-only version
   * price greater than zero
   * EffectiveFrom required
   * one current active version per Component
   * successor closes previous current version
   * history remains readable

3. Use clean V2 names where practical:

   * `ComponentSuggestedSellingPriceVersion`
   * `IComponentSuggestedSellingPriceVersionRepository`
   * `IComponentSuggestedSellingPriceAppService`
   * `ComponentSuggestedSellingPriceVersionDto`
   * `CreateComponentSuggestedSellingPriceVersionDto`
   * `ComponentSuggestedSellingPriceController`

4. Update permissions:

   * `Pricing.ComponentSuggestedSellingPrices`
   * `Pricing.ComponentSuggestedSellingPrices.Create`
   * `Pricing.ComponentSuggestedSellingPrices.History`

5. Update UI:

   * Pricing tab: `Giá bán đề xuất linh kiện`
   * Remove `Giá mua linh kiện`
   * History/Create pages use selling-price terminology
   * Do not modalize in this batch unless trivial and safe

6. Update API route using V2 naming.
   Prefer a route like:

   * `/api/pricing/component-suggested-selling-prices`
   * or another route consistent with the existing API style

7. Update Audit:

   * old component purchase price audit terms/events become component suggested selling price terms/events
   * preserve audit safety rules

8. EF/migration:

   * old dev data does not need migration
   * clean rename/replacement is allowed
   * do not execute database drop commands automatically
   * report required local DB update/reset steps

9. Tests:

   * update old component purchase price tests to component suggested selling price tests
   * preserve equivalent coverage
   * do not delete tests just to make build pass

## Validation

Run:

```powershell
dotnet build VPureLux.slnx --no-restore
dotnet test VPureLux.slnx --no-build
```

If the repo has an existing EF pending-model check command, run it. If not found, report that no known command exists.

Run and report:

```powershell
rg -n "ComponentPurchasePrice|PurchasePrice|Giá mua linh kiện|component purchase price|purchase price" src test -g "!bin" -g "!obj"

rg -n "ComponentSuggestedSellingPrice|Giá bán đề xuất linh kiện" src test -g "!bin" -g "!obj"

rg -n "Pricing.ComponentPurchasePrices" src test -g "!bin" -g "!obj"

rg -n "Pricing.ComponentSuggestedSellingPrices" src test -g "!bin" -g "!obj"
```

## Final Report Required

Report:

1. Summary
2. Files changed by layer
3. Old concept removed/replaced
4. New aggregate/service/DTO/controller names
5. Permission names
6. API routes
7. EF/migration changes
8. Audit changes
9. UI/localization changes
10. Tests updated
11. Build/test results
12. Search results for old/new terms
13. Remaining old references and why
14. Local DB update/reset steps
15. Forbidden areas changed? yes/no
16. Sales touched? yes/no
17. Product Component Build Price touched? yes/no
18. Remaining risks
19. Recommended next single step

Do not generate the next prompt.
