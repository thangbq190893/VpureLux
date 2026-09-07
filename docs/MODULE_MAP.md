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

## Warranty / CustomerCare
- **Purpose:** manage replacement policies, machine eligibility, idempotent Sales intake, installation confirmation, customer machines bought inside/outside the company, actual component positions, reminder lifecycle, and per-machine maintenance history.
- **Key entities:** `ComponentReplacementPolicy`, `ProductMachineSetting`, `CustomerAsset`, `CustomerAssetComponent`, `AssetReplacementReminder`, `AssetMaintenanceEvent`, `CustomerCareSyncFailure`.
- **Application services:** `WarrantyAppService`, `CustomerCareSalesIntakeService`; Web periodic worker `CustomerCareSalesIntakeWorker`.
- **Contracts:** `IWarrantyAppService` and DTOs under `Application.Contracts/Warranty`; runtime gate options under `Domain.Shared/CustomerCare`.
- **Razor Pages:** `Pages/Warranty/{Index,Policies,Machines,PendingInstallations,Install,SyncFailures,ReminderActionModal}` and `Pages/Warranty/Assets/{Index,CreateExternal,Edit,Details}`.
- **Permissions:** `Warranty.{View,ManagePolicies,ManageMachines,ManageSyncFailures,ManageInstallations,ManageAssets,ManageReminders}`.
- **Tests:** Domain (`Warranty/CustomerCareDomainTests`); EF Core (`Warranty/CustomerCareSchemaTests`, `Warranty/WarrantyPermissionTests`, Sales workflow intake/installation regression); Web (`Pages/WarrantyPagesTests`).
- **Important rules:** Sales confirmation never calls CustomerCare synchronously. Intake is disabled by default and requires both gates plus an explicit go-live instant. A reminder requires a confirmed baseline, mapped active Component, and enabled policy. Reminder cycle/warning values are snapshots. External machines may keep unmapped positions and missing baselines; no date is fabricated. All lifecycle actions append an idempotent maintenance event.
- **Migration:** `20260824050543_AddCustomerCareFoundation` is schema-only and does not alter stable Sales/Product/Component/BOM/Inventory/Customer tables.
- **Acceptance (2026-09-07):** User explicitly ACCEPTED Warranty/CustomerCare; W-008 and W-GATE are DONE. Existing reminder snapshots stay immutable, while a completed reminder starts a successor from the current enabled policy only when the Component and actual position remain active and mapped. The first-run metadata exception, isolated R2 reconciliation, role/UI evidence, and closed intake-race checks remain recorded in `W008_WARRANTY_UAT_20260904.md`. W-008 source was sealed in `d1e8b5684d21eca3ee5586fe75913b60e24de190`; it is not a new production release.
- **Service integration:** `SERVICE_INVENTORY_AUDIT.md` records the historical bcc1b36/220d41c audit. S-003 adds CustomerCare-owned actual completion events/current-policy successors and shared asset coordination. Direct manual replacement is server-blocked only while Service is enabled; disabled-Service W-008 behavior remains intact. See `S003_SERVICE_COMPLETION.md`.

## Service (S-001/S-002/S-003/S-004/S-005/S-006)
- **Purpose:** separate Service workflow for work/labor and materials on one customer machine; labor is not a Sales/Catalog Product. Runtime is default-disabled by configuration design; the accepted production deployment explicitly enables Service.
- **Entities:** `ServiceWork`; operational `ServiceOrder`; owned `ServiceOrderLine`; separate `ServicePayment` ledger and immutable factual `ServiceRefund`. Draft -> Confirmed -> InProgress -> Completed, with reason-required cancellation before completion and canonical exact replay.
- **Contracts/application:** Work/Order/Payment contracts and AppServices, `ServiceOrderOperationCoordinator`, `ServiceCompletionProcessor`. Orders expose CRUD/transitions/atomic Complete and paged lookups. `ICustomerCareServiceCompletion` owns care writes. `IServicePaymentAppService` exposes payment/void/refund plus order/customer monetary summaries and paged histories. No report command is exposed.
- **Snapshot/edit rule:** existing line IDs and code/name/unit/price/cost snapshots survive header, note and quantity edits. Only explicit Component/Work replacement copies current catalog fields. Inactive historical selections remain visible. Labor `StandardCostSnapshot` preserves null (unknown) versus zero (known zero).
- **EF/query:** custom Work/Order/read repositories. Lists and lookups use SQL count/filter/whitelisted sort/Skip/Take with stable tie breakers. Material eligibility uses active Component plus an inventory-enabled active Component StockItem. Business code seed/max is database-side and uniqueness remains database-backed.
- **UI:** one Service menu, server-side DataTables, remote Select2, dynamic Material/Labor planning rows, ABP Cancel/Complete modals, antiforgery and safe encoding. Complete accepts integer actual quantities and UTC+07 local datetime; completed Details shows actual amounts.
- **Permissions:** `Service.{View,Create,Edit,Confirm,Cancel,Complete,ManageWorks,ManagePayments,ViewCost,ViewProfit}` plus root. Feature and permissions are checked server-side; Start reuses Confirm. ManagePayments covers payment, reasoned void and refund; read summaries/history use View. Reports restore `Reports.Service.View` and `Reports.Consolidated.View`, with underlying source/cost/profit checks.
- **Migrations:** original `20260824113235_AddServiceModule`; S-001 `20260906174229_AddServiceWorkCatalogFields`; S-002 `20260906185742_AddServiceOrderWorkflow` adds only nullable CancellationReason and nullable line StandardCostSnapshot. None was applied by S-001/S-002.
- **S-003 migration:** `20260906200825_AddServiceCompletionFacts` adds seven nullable facts/source fields; no DML/backfill or application. ActualCostAmount/ActualProfitAmount preserve unknown labor cost. ServiceIssue is enum value 6, with the current Inventory FIFO and batched lots/balances.
- **Tests/evidence:** Domain/Application Service tests; EF `ServiceFoundationTests` and `ServiceOrderWorkflowTests`; Web `ServiceWorkWebTests`, `ServiceOrderWebTests` and source guards. See `S001_SERVICE_FOUNDATION.md` and `S002_SERVICE_ORDER_WORKFLOW.md`.
- **S-004 money:** `ServiceMoneySummary.Projection` is the canonical domain/SQL formula. Grouped LEFT JOINs and customer SQL aggregation keep settlement separate from completed revenue. Payment/void/refund share the existing ServiceOrder lock with Cancel/Complete; replay never resurrects voided cash. Cancel preserves advances and creates refund due without automatic cash return. Existing Details contains ABP money/void modals and server-paged payment/refund histories. Sales payments are unchanged.
- **S-004 migration/evidence:** generated-only `20260907021302_AddServicePaymentSettlement`: eight nullable payment fields, new factual refund table and indexes; no DML/backfill/application. Source `2a93dfb847201fe87749d6dc7d4b267db16fad4c`; formulas, replay, races, legacy, security, browser and bounded Web evidence: `S004_SERVICE_PAYMENTS.md`.
- **S-005 reports:** `IBusinessRevenueAppService`, `BusinessRevenueAppService` and `EfCoreBusinessRevenueReadRepository` compose completed Service with confirmed effective Sales lines in SQL. `Pages/Reports/{ServiceRevenue,BusinessRevenue}` share a thin PageModel/partial/JS and retain independent existing Sales reports. Historical FIFO/nullable labor costs, UTC+07 completion calendar, factual S004 settlement and server-side cross-source permissions are preserved. Unknown cost yields null profit; summary/count/filter/sort/page2 execute in SQL. No migration or Sales stored-procedure change. Source `e40aed2e9b8e1072e3958d9a47ae52d780fd0592`; evidence and Sales gross/net discrepancy: `S005_SERVICE_REPORTS.md`.
- **Boundary:** Confirm/Start remain free of stock/revenue/care/payment side effects. S-003 completion atomically posts FIFO, actual snapshots and care events/successors; S-004 settlement and S-005 recognition reports do not rewrite those facts. S-001 through S-006 and SERVICE-V1-ROLLOUT are DONE; Service V1 is production RELEASED / ACCEPTED at `b0bf197e8525acb2f254995af70b3da8a397a9f8`, tag `release-2026-09-07-service-v1`, with Service enabled. The prior Sales V1 release remains the immediate rollback baseline. Detailed completion locks/rollback: `S003_SERVICE_COMPLETION.md`.
- **Production release:** active path `/opt/vpurelux/releases/web-20260907-152000-service-v1-b0bf197`; rollout migrated history 20 -> 24 once, passed 41/41 pre-migration reconciliation and, after separately audited concurrent user Sales activity, final read-only baseline reconciliation 42/42. No rollout backfill or fake production UAT occurred. Sales V1 at `/opt/vpurelux/releases/web-20260903-180516-sales-v1-a4717aa` is retained for immediate rollback; full evidence: `SERVICE_V1_PRODUCTION_ROLLOUT_20260907.md`.
- **S-006 fixes:** `5e7727a` avoids SQL Server COUNT_BIG(NULL) for Sales-only report summaries; `2583dbe` enforces standard antiforgery on three Service conventional controllers only; `c2663ac` preserves legacy Details wall times through `IsLegacyCompletion`. No new migration or Sales workflow/API change.
- **S-006 evidence:** `S006_SERVICE_UAT_20260907.md`, `evidence/s006/verification-evidence.json`. User-authorized VPL backup/clone/empty chain and four pending migrations applied, no business DML/backfill; final41/41 legacy fingerprints unchanged. Real SQL/Redis9races, operator care/money/HTTP/UI and394tests pass; full combined Web not claimed. S-006 itself ended READY FOR PRODUCTION ROLLOUT: clean b0bf197 full publish was verified locally, and that task did not access/deploy production or push. A later separate SERVICE-V1-ROLLOUT deployed and accepted Service production; see `SERVICE_V1_PRODUCTION_ROLLOUT_20260907.md`. C01 is accepted as projection-only coverage: planned10/actual12 workflow is N/A by design under S003, monetary projection12/5/7 passes, and Actual > Planned remains disabled.

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
