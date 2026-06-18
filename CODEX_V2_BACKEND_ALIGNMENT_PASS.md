# Codex V2 Backend Alignment Pass

## Goal

Align VPureLux backend/business architecture to V2 before doing more manual UI UAT.

The project is still local development. There is no production data. Backend refactor and migrations are allowed when needed.

## Read First

* `CODEX_README_VPURELUX_V2.md`
* `BUSINESS_ARCHITECTURE_DECISIONS_V2.md`
* `MODULE_SPECIFICATIONS_V2.md`
* `IMPLEMENTATION_ROADMAP_V2.md`
* `V2_ARCHITECTURE_ALIGNMENT_REPORT.md`

## Core V2 Decisions

* Everything sold is Product/SKU.
* Component is inventory material/part.
* Sales must not sell Component directly.
* Loose component sale must be modeled as Product/SKU with BOM of one Component.
* Product must have published BOM before sale/confirmation.
* Inventory phase 1 stocks Component only.
* Pricing owns suggested selling prices only.
* Inventory Receipt UnitCost is actual input cost.
* Product Suggested Price remains manually entered.
* Giá cấu thành linh kiện is read/query context only.

## Local Dev Approval

You may run without asking:

* `dotnet build`
* `dotnet test`
* `rg`
* `dotnet ef migrations add`
* `dotnet ef database update`
* DbMigrator

Do not drop the whole database unless explicitly required and reported.

## ABP Architecture Rules

* Domain owns business invariants.
* Application coordinates use cases.
* Application.Contracts owns DTOs/interfaces/permissions.
* EF owns persistence only.
* Razor PageModels call Application Services only.
* No DbContext/repository/domain service in Razor PageModels.
* Do not calculate business rules in Razor/JavaScript.
* Follow ABP Razor Pages conventions.
* No inline script.
* No `<abp-button href>`.
* No hardcoded internal `href="/..."`.

## Phase 1 - Catalog Activation and Terminology

Implement if missing:

* Product Activate app service/API/UI support if domain supports it.
* Component Activate app service/API/UI support if domain supports it.
* Active rows show `Ngừng sử dụng`.
* Inactive rows show `Kích hoạt` / `Sử dụng lại`.
* Remove fake disabled activate buttons.
* UI/localization:

  * Product.Code = `Mã sản phẩm`
  * Component.Code = `Mã linh kiện`
  * Product = `Sản phẩm`
  * Component = `Linh kiện`

Preserve existing Deactivate and audit behavior.

## Phase 2 - Sales V2 Product/SKU Only

Refactor Sales so order lines sell Product/SKU only.

Required:

* Remove/disable direct Component sales from new Sales workflows.
* Sales line references ProductId.
* Product must be active.
* Product must have a published BOM before confirmation.
* Loose component sales happen through Product/SKU with BOM of one Component.
* On confirmation:

  * resolve published BOM
  * expand components
  * validate component stock
  * allocate FIFO through existing Inventory application/domain logic
  * snapshot product, BOM, component requirements, actual selling price, cost, revenue, profit
* Existing Product Suggested Price remains default price source if available.
* Actual Selling Price remains Sales-owned and manually editable.
* Cost/profit visibility permissions must be preserved.

Do not implement direct Component sales compatibility unless required only for historical data. Since this is dev, old direct Component sales data may be discarded through migration if needed.

## Phase 3 - BOM Enforcement for Sales

Ensure Sales cannot confirm Product without published BOM.

Do not require Product activation to have BOM. Enforce BOM at Sales selection/confirmation.

BOM with one Component is valid.

## Phase 4 - Inventory Consistency

Ensure Inventory remains Component-only in phase 1.

Do not enable Product StockItems.

Inventory Receipt/Issue/Adjustment must continue to use Component StockItems and FIFO backend logic.

Do not change Receipt UnitCost meaning.

## Phase 5 - Pricing Integration Check

Verify Pricing V2 still compiles and works with Sales changes:

* Component Suggested Selling Price unchanged.
* Product Suggested Selling Price unchanged.
* Giá cấu thành linh kiện unchanged.
* Product Suggested Price can be used as default actual price in Sales if existing service supports it.

Do not auto-calculate Product Suggested Price.

## Phase 6 - Audit Alignment

Update audit event handling for any new/renamed V2 Sales/Catalog events.

Do not log Base64 images or large payloads.

## Validation

After each significant phase, run targeted tests if practical.

At the end run:

```powershell
dotnet build VPureLux.slnx --no-restore
dotnet test VPureLux.slnx --no-build
```

If migrations are generated, apply them locally:

```powershell
dotnet ef database update --project src/VPureLux.EntityFrameworkCore/VPureLux.EntityFrameworkCore.csproj --startup-project src/VPureLux.DbMigrator/VPureLux.DbMigrator.csproj --context VPureLuxDbContext
```

Run searches:

```powershell
rg -n "SalesOrderLineType.Component|LineType.*Component|Component line|direct Component" src test -g "!bin" -g "!obj"
rg -n "ComponentPurchasePrice|Giá mua linh kiện" src test -g "!bin" -g "!obj"
rg -n "<abp-button[^>]*href=" src/VPureLux.Web/Pages -g "*.cshtml"
rg -n "href=\"/" src/VPureLux.Web/Pages -g "*.cshtml"
rg --pcre2 -n "<script(?![^>]*src=)" src/VPureLux.Web/Pages -g "*.cshtml"
```

## Final Report

Report:

1. Summary.
2. Phases completed.
3. Files changed by layer.
4. Domain changes.
5. Application contract/API changes.
6. EF/migration changes and DB commands run.
7. Sales V2 behavior.
8. Catalog activation behavior.
9. Inventory behavior confirmation.
10. Pricing behavior confirmation.
11. Audit changes.
12. Build/test results.
13. Search results.
14. Remaining UI issues intentionally deferred.
15. Remaining backend risks.
16. Recommended manual UAT/fixbug pass.

Do not generate the next prompt.
