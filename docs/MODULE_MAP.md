# Module Map

> Map of the **custom** VPureLux modules as they exist in source. Only modules present in code are listed. Paths are relative to repo root. Where completeness/quality is not verifiable from source alone, it is marked **Needs verification**.

Layout per module: `src/VPureLux.Domain/<Module>`, `src/VPureLux.Application.Contracts/<Module>`, `src/VPureLux.Application/<Module>`, `src/VPureLux.EntityFrameworkCore/<Module>`, `src/VPureLux.Web/Pages/<Module>`, and `test/*`.

Entities marked *(owned/value)* are configured with `builder.Ignore<>()` or as owned types in `VPureLuxDbContext` (they are not standalone tables).

---

## Catalog
- **Purpose:** master data for `Component`s and finished `Product`s, including catalog images.
- **Key entities:** `Component`, `Product`, `ImageData` *(value)*.
- **Application services:** `ComponentAppService` (`Catalog/Components`), `ProductAppService` (`Catalog/Products`); helpers `CatalogManager` (domain), `CatalogImageProcessor`, `CatalogImageUploadHelper` (web).
- **Contracts:** `IComponentAppService`, `IProductAppService` + DTOs under `Application.Contracts/Catalog`.
- **Razor Pages:** `Pages/Catalog/Components/{Index,Create,Edit}`, `Pages/Catalog/Products/{Index,Create,Edit}`.
- **Permissions:** `Catalog.Components.{View,Create,Edit}`, `Catalog.Products.{View,Create,Edit}`.
- **Tests:** Domain (`Catalog/ComponentDomainTests`, `Catalog/ProductDomainTests`, `Catalog/CatalogImageDomainTests`, `Catalog/CatalogHardeningDomainTests`); Application (`Catalog/CatalogValidationTests`, `Catalog/CatalogImageSafetyTests`, `Catalog/CatalogImageProcessorTests`, `Catalog/CatalogAuditIntegrationTests`; helper `CatalogImageTestData`); EF Core (`Catalog/ComponentAppServiceTests`, `Catalog/ProductAppServiceTests`, `Catalog/CatalogRepositoryTests`, `Catalog/CatalogPermissionTests`, `Catalog/CatalogImageAppServiceTests`, `Catalog/CatalogImagePersistenceTests`); Web (`Pages/CatalogPagesTests`, `Pages/CatalogPageModelPermissionTests`, `Pages/CatalogImagePagesTests`, `Api/CatalogImageApiTests`).
- **Notes/risks:** image handling has dedicated safety/validation paths (signature/size/unsafe-content error codes `CATALOG_005..009`). Catalog image extension has its own root spec/cert docs (`CATALOG_IMAGE_EXTENSION_*`).

## Bom (Bill of Materials)
- **Purpose:** versioned BOM per product with draft → published → archived lifecycle (single active/published BOM per product enforced).
- **Key entities:** `BomVersion`, `BomItem` *(owned/value — `Ignore`d as standalone)*, `BomVersionNo` *(value)*.
- **Application services:** `BomAppService`; domain `BomManager`; app validator `BomCatalogValidator`.
- **Contracts:** `IBomAppService` + DTOs under `Application.Contracts/Bom`.
- **Razor Pages:** `Pages/Bom/{Index,Create,Edit,Clone,Details,Product}`.
- **Permissions:** `Bom.{View,Create,Publish,Archive}`.
- **Tests:** Domain (`Bom/BomVersionDomainTests`, `Bom/BomStateMachineAndEventTests`); EF Core (`Bom/BomAppServiceTests`, `Bom/BomRepositoryAndPersistenceTests`, `Bom/BomPermissionTests`); Web (`Pages/BomPagesTests`, `Api/BomApiTests`).
- **Notes/risks:** lifecycle invariants enforced via error codes `BOM_001..004`; migration `EnforceSinglePublishedBomPerProduct` adds a uniqueness constraint.

## Customers
- **Purpose:** `Customer` and `CustomerGroup` master data with active/inactive status.
- **Key entities:** `Customer`, `CustomerGroup`.
- **Application services:** `CustomerAppService`, `CustomerGroupAppService`; domain `CustomerManager`, `CustomerGroupManager`.
- **Contracts:** `ICustomerAppService`, `ICustomerGroupAppService` + DTOs (`CreateCustomerDto`, `UpdateCustomerDto`, `GetCustomerListInput`, etc.).
- **Razor Pages:** `Pages/Customers/{Index,Create,Edit,Details,CreateModal,EditModal,DetailsModal}`, `Pages/CustomerGroups/{Index,Create,Edit,Details,CreateModal,EditModal,DetailsModal}`.
- **Permissions:** `Customers.{View,Create,Edit,ManageStatus}`, `CustomerGroups.{View,Create,Edit,ManageStatus}`.
- **Tests:** Domain (`Customers/CustomerDomainTests`, `Customers/CustomerGroupDomainTests`); EF Core (`Customers/CustomerAppServiceTests`, `Customers/CustomerRepositoryAndSeedTests`, `Customers/CustomerPermissionTests`); Web (`Pages/CustomerPagesTests`, `Pages/CustomerPageModelPermissionTests`, `Api/CustomerApiTests`).
- **Notes/risks:** reference (`CustomerGroupName`) is mapped onto the DTO so UI shows code/name labels, not GUIDs — this is the canonical pattern. Error codes `CUSTOMER_001..007`.

## Pricing
- **Purpose:** effective-dated suggested prices: component suggested selling prices and product suggested prices.
- **Key entities:** `ComponentSuggestedSellingPriceVersion`, `ProductSuggestedPriceVersion`, value objects `Money`, `EffectivePeriod`, `PriceVersionNo` *(all value/owned)*.
- **Application services:** `ComponentSuggestedSellingPriceAppService`, `ProductSuggestedPriceAppService`, `ProductPricingContextAppService`; domain `PricingManager`; app validator `PricingCatalogValidator`.
- **Contracts:** `IComponentSuggestedSellingPriceAppService`, `IProductSuggestedPriceAppService`, `IProductPricingContextAppService` + DTOs.
- **Razor Pages:** `Pages/Pricing/{Index}`, `Pages/Pricing/Components/{Create,History}`, `Pages/Pricing/Products/{Create,History}`.
- **Permissions:** `Pricing.{View,History}`, `Pricing.ComponentSuggestedSellingPrices.{Create,History}`, `Pricing.ProductSuggestedPrices.Create`.
- **Tests:** Domain (`Pricing/PricingDomainTests`); EF Core (`Pricing/PricingAppServiceTests`, `Pricing/PricingRepositoryTests`, `Pricing/PricingBoundaryTests`, `Pricing/PricingPermissionTests`); Web (`Pages/PricingPagesTests`, `Api/PricingApiTests`). Root spec: `PRICING_MODULE_IMPLEMENTATION_SPECIFICATION.md`, `CODEX_BATCH_PRICING_V2_1A.md`.
- **Notes/risks:** active-version uniqueness, backdating and effective-period rules via error codes `PRICE_001..006`. A column rename migration exists (`RenameComponentPurchasePriceToComponentSuggestedSellingPrice`).

## Inventory
- **Purpose:** warehouses, stock items, lots, transactions (receipt/issue/adjustment) and balances.
- **Key entities:** `Warehouse`, `StockItem`, `InventoryLot`, `InventoryTransaction`, `InventoryBalance`, plus `InventoryTransactionLine`, `InventoryLotAllocation` *(owned/value — `Ignore`d as standalone)*.
- **Application services:** `WarehouseAppService`, `StockItemAppService`, `InventoryTransactionAppService`, `InventoryQueryAppService`; domain `InventoryManager`, `StockItemManager`.
- **Contracts:** `Inventory/InventoryServiceContracts.cs` (interfaces) + DTOs.
- **Razor Pages:** `Pages/Inventory/{Index,Warehouses,Balances,Lots,Ledger,Receipt,Issue,Adjustment}`.
- **Permissions:** `Inventory.{View,Receive,Issue,Adjust,ManageWarehouses,ViewLedger}`.
- **Tests:** Domain (`Inventory/InventoryDomainTests`); EF Core (`Inventory/InventoryWorkflowTests`, `Inventory/InventoryRepositoryAndPermissionTests`); Web (`Pages/InventoryPagesTests`, `Api/InventoryApiTests`). Root: `INVENTORY_MODULE_IMPLEMENTATION_SPECIFICATION.md`, `CODEX_V2_INVENTORY_POSTING_UAT_WORKFLOW.md`.
- **Notes/risks:** `InventoryLot`/`InventoryBalance` use `RowVersion` concurrency tokens (special-cased for SQLite). Idempotency and insufficient-stock rules via `INV_001..012`.

## Sales
- **Purpose:** sales orders with line items, BOM snapshot, confirm/cancel lifecycle, and cost/profit reporting.
- **Key entities:** `SalesOrder`, `SalesOrderLine` *(owned)*, `SalesOrderBomSnapshotItem` *(owned)*.
- **Application services:** `SalesOrderAppService`; domain `SalesManager`.
- **Contracts:** `ISalesOrderAppService` + DTOs.
- **Razor Pages:** `Pages/Sales/{Index,Create,Edit,Details,History,CustomerHistory}`.
- **Permissions:** `Sales.{View,Create,Edit,OverridePrice,Confirm,Cancel,ViewCost,ViewProfit,ViewCustomerHistory}`.
- **Tests:** Domain (`Sales/SalesDomainTests`); EF Core (`Sales/SalesWorkflowTests`, `Sales/SalesRepositoryAndPermissionTests`); Web (`Pages/SalesPagesTests`, `Api/SalesApiTests`). Root: `SALES_MODULE_IMPLEMENTATION_SPECIFICATION.md`.
- **Notes/risks:** `SalesOrder` uses `RowVersion`; confirm/cancel idempotency and BOM-must-be-published rules via `SALES_001..010`. Cost/profit are permission-gated (`ViewCost`, `ViewProfit`).

## Audit (business audit)
- **Purpose:** domain-level business audit log (separate from ABP framework audit logging).
- **Key entities:** `BusinessAuditLog`.
- **Application services:** `BusinessAuditAppService`; domain `BusinessAuditManager`.
- **Contracts:** `IBusinessAuditAppService` + DTOs.
- **Razor Pages:** `Pages/Audit/{Index,Details,Reports,Export}`.
- **Permissions:** `Audit.{View,Export}`.
- **Tests:** Domain (`Audit/BusinessAuditDomainTests`); EF Core (`Audit/AuditRepositoryAndApplicationTests`, `Audit/AuditEventIngestionTests`); Web (`Pages/AuditPagesTests`, `Api/AuditApiTests`). Root: `AUDIT_MODULE_*`.
- **Notes/risks:** payload-size guard via `AUDIT_001`.

## Dashboard
- **Purpose:** landing dashboards. `Pages/HostDashboard` and `Pages/TenantDashboard`, gated by `Dashboard.Host` / `Dashboard.Tenant`.
- **Notes:** primarily presentation; **Needs verification** whether dashboards aggregate real module data or are placeholders.

---

## Cross-module / known gaps (from repo signals — Needs verification)
- The root contains active UI backlogs (`UI_FIX_BACKLOG.md`, `V2_FINAL_UAT_BUG_BACKLOG.md`, `UI_BACKEND_GAP_REGISTER.md`) indicating known UI/UAT issues. Treat these as **Needs verification** against current code before relying on them.
- Several child collections are modeled as owned/value types (`Ignore`d standalone). When adding queries, go through the aggregate root, not the child.
