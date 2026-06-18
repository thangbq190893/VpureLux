# UI Permission Visibility Matrix

This matrix inventories UI elements that must be rendered only when the current
user has the exact permission that protects the backend operation or destination
page. Do not change backend permissions in this UI refactor.

Status values:

- `OK`: current rendering appears aligned.
- `Partial`: page or action is protected, but field/action visibility needs
  tightening.
- `Fix Required`: visible action or field is not aligned with destination or
  backend permission.
- `Needs Review`: exact sensitivity or current inherited behavior needs human
  confirmation.

| Module | Page | UI Element | Required Permission | Backend/Destination Permission | Current Status | Required Fix |
|---|---|---|---|---|---|---|
| Catalog | `/Catalog/Products` | Create Product button | `Catalog.Products.Create` | `Catalog.Products.Create` | OK, tag misuse | Replace `abp-button href` with anchor tag helper; keep permission check |
| Catalog | `/Catalog/Products` | Edit Product link | `Catalog.Products.Edit` | `Catalog.Products.Edit` | OK | Convert route to `asp-page` and keep permission check |
| Catalog | `/Catalog/Products` | Deactivate Product button | `Catalog.Products.Edit` | Product app service edit/status behavior | Partial | Add confirmation and notification; verify exact backend policy |
| Catalog | `/Catalog/Products/Edit/{id}` | Upload/replace image | `Catalog.Products.Edit` | `Catalog.Products.Edit` | OK | Add client-side confirmation/feedback only |
| Catalog | `/Catalog/Products/Edit/{id}` | Remove image | `Catalog.Products.Edit` | `Catalog.Products.Edit` | Partial | Add confirmation and notification |
| Catalog | `/Catalog/Components` | Create Component button | `Catalog.Components.Create` | `Catalog.Components.Create` | OK, tag misuse | Replace `abp-button href` with anchor tag helper |
| Catalog | `/Catalog/Components` | Edit Component link | `Catalog.Components.Edit` | `Catalog.Components.Edit` | OK | Convert route to `asp-page` |
| Catalog | `/Catalog/Components` | Deactivate Component button | `Catalog.Components.Edit` | Component app service edit/status behavior | Partial | Add confirmation and notification; verify exact backend policy |
| Catalog | `/Catalog/Components/Edit/{id}` | Upload/replace/remove image | `Catalog.Components.Edit` | `Catalog.Components.Edit` | Partial | Keep permission; add confirmation/feedback |
| BOM | `/Bom` | Open product BOM history | `Bom.View` | `/Bom/Product/{productId}` requires `Bom.View` | OK | Replace raw ProductId input with selector later if approved |
| BOM | `/Bom/Product/{productId}` | Create BOM button | `Bom.Create` | `/Bom/Create/{productId}` requires `Bom.Create` | OK, tag misuse | Replace `abp-button href`; preserve route |
| BOM | `/Bom/Product/{productId}` | Edit BOM link | `Bom.Create` and Draft status | `/Bom/Edit/{id}` requires `Bom.Create` | OK | Convert route to `asp-page`; keep draft-only visibility |
| BOM | `/Bom/Product/{productId}` | Clone BOM link | `Bom.Create` | `/Bom/Clone/{id}` requires `Bom.Create` | OK | Add confirmation or modal pattern |
| BOM | `/Bom/Product/{productId}` | Publish BOM button | `Bom.Publish` and Draft status | `Bom.Publish` | OK | Add `abp.message.confirm` and notification |
| BOM | `/Bom/Product/{productId}` | Archive BOM button | `Bom.Archive` and Published status | `Bom.Archive` | OK | Add confirmation and notification |
| BOM | `/Bom/Details/{id}` | Edit button | `Bom.Create` and Draft status | `/Bom/Edit/{id}` requires `Bom.Create` | OK | Convert route to `asp-page` |
| Customer | `/Customers` | Create Customer button | `Customers.Create` | `/Customers/Create` requires `Customers.Create` | OK | Convert route to `asp-page` |
| Customer | `/Customers` | Details link | `Customers.View` | `/Customers/Details/{id}` requires `Customers.View` | OK | Convert route to `asp-page` |
| Customer | `/Customers` | Edit link | `Customers.Edit` | `/Customers/Edit/{id}` requires `Customers.Edit` | OK | Convert route to `asp-page` |
| Customer | `/Customers` | Activate/Deactivate buttons | `Customers.ManageStatus` | Customer activate/deactivate app service | OK | Add confirmation and notification |
| Customer Group | `/CustomerGroups` | Create Group button | `CustomerGroups.Create` | `/CustomerGroups/Create` requires `CustomerGroups.Create` | OK | Convert route to `asp-page` |
| Customer Group | `/CustomerGroups` | Details link | `CustomerGroups.View` | `/CustomerGroups/Details/{id}` requires `CustomerGroups.View` | OK | Convert route to `asp-page` |
| Customer Group | `/CustomerGroups` | Edit link | `CustomerGroups.Edit` | `/CustomerGroups/Edit/{id}` requires `CustomerGroups.Edit` | OK | Convert route to `asp-page` |
| Customer Group | `/CustomerGroups` | Activate/Deactivate buttons | `CustomerGroups.ManageStatus` | CustomerGroup activate/deactivate app service | OK | Add confirmation and notification |
| Customer | `/Customers/Create` | Full-page create form | `Customers.Create` | `Customers.Create` | OK | Safe modal candidate; preserve `CreateCustomerDto Input` binding |
| Customer | `/Customers/Edit/{id}` | Full-page edit form | `Customers.Edit` | `Customers.Edit` | OK | Safe modal candidate; preserve `Id`, `UpdateCustomerDto Input`, and immutable code display |
| Customer | `/Customers/Details/{id}` | Full-page read-only detail | `Customers.View` | `Customers.View` | OK | Safe read-only modal candidate |
| Customer Group | `/CustomerGroups/Create` | Full-page create form | `CustomerGroups.Create` | `CustomerGroups.Create` | OK | Safe modal candidate; preserve `CreateCustomerGroupDto Input` binding |
| Customer Group | `/CustomerGroups/Edit/{id}` | Full-page edit form | `CustomerGroups.Edit` | `CustomerGroups.Edit` | OK | Safe modal candidate; preserve `Id`, `UpdateCustomerGroupDto Input`, and immutable code display |
| Customer Group | `/CustomerGroups/Details/{id}` | Full-page read-only detail | `CustomerGroups.View` | `CustomerGroups.View` | OK | Safe read-only modal candidate |
| Pricing | `/Pricing` | Component purchase price history links | `Pricing.History` | `/Pricing/Components/{componentId}` requires `Pricing.History` | OK | Fixed in Step 2.1: hidden unless user has `Pricing.History` |
| Pricing | `/Pricing` | Product suggested price history links | `Pricing.History` | `/Pricing/Products/{productId}` requires `Pricing.History` | OK | Fixed in Step 2.1: hidden unless user has `Pricing.History` |
| Pricing | `/Pricing/Components/{componentId}` | Create new purchase price version | `Pricing.ComponentPurchasePrices.Create` | `/Pricing/Components/Create/{componentId}` requires same permission | OK | Convert route to `asp-page`; consider modal |
| Pricing | `/Pricing/Products/{productId}` | Create new suggested price version | `Pricing.ProductSuggestedPrices.Create` | `/Pricing/Products/Create/{productId}` requires same permission | OK | Convert route to `asp-page`; consider modal |
| Inventory | `/Inventory` | Warehouses link | `Inventory.ManageWarehouses` | `/Inventory/Warehouses` requires same permission | OK | Convert route to `asp-page` |
| Inventory | `/Inventory` | Receipt link | `Inventory.Receive` | `/Inventory/Receipt` requires same permission | OK | Convert route to `asp-page` |
| Inventory | `/Inventory` | Issue link | `Inventory.Issue` | `/Inventory/Issue` requires same permission | OK | Convert route to `asp-page` |
| Inventory | `/Inventory` | Adjustment link | `Inventory.Adjust` | `/Inventory/Adjustment` requires same permission | OK | Convert route to `asp-page` |
| Inventory | `/Inventory` | Balances link | `Inventory.View` | `/Inventory/Balances` requires `Inventory.View` | OK | Convert route to `asp-page` |
| Inventory | `/Inventory` | Lots link | `Inventory.View` | `/Inventory/Lots` requires `Inventory.View` | OK | Convert route to `asp-page` |
| Inventory | `/Inventory` | Ledger link | `Inventory.ViewLedger` | `/Inventory/Ledger` requires same permission | OK | Convert route to `asp-page` |
| Inventory | `/Inventory/Warehouses` | Create Warehouse | `Inventory.ManageWarehouses` | Warehouse app service create | OK | Add confirmation/notification if status-changing |
| Inventory | `/Inventory/Warehouses` | Activate/Deactivate Warehouse | `Inventory.ManageWarehouses` | Warehouse activate/deactivate | OK | Add confirmation and notification |
| Inventory | `/Inventory/Receipt` | Post receipt | `Inventory.Receive` | Inventory receipt app service | OK | Add confirmation, busy state, notification |
| Inventory | `/Inventory/Issue` | Post issue | `Inventory.Issue` | Inventory issue app service | OK | Add confirmation, busy state, notification |
| Inventory | `/Inventory/Adjustment` | Post adjustment | `Inventory.Adjust` | Inventory adjustment app service | OK | Add confirmation, busy state, notification |
| Inventory | `/Inventory/Ledger` | Issue cost/ledger cost fields | `Inventory.ViewLedger` | Page requires `Inventory.ViewLedger` | OK | Accepted: do not add a new permission for Inventory cost/value fields |
| Sales | `/Sales` | Create Sales Order button | `Sales.Create` | `/Sales/Create` requires `Sales.Create` | OK | Convert route to `asp-page` |
| Sales | `/Sales` | Customer History link | `Sales.ViewCustomerHistory` and `Sales.ViewProfit` | `/Sales/CustomerHistory` requires `Sales.ViewCustomerHistory` and `Sales.ViewProfit` | OK | Fixed in Step 2.1: hidden unless user has both permissions |
| Sales | `/Sales` | Order History link | `Sales.View` | `/Sales/History` requires `Sales.View` | OK | Convert route to `asp-page` |
| Sales | `/Sales` | Details link | `Sales.View` | `/Sales/Details/{id}` requires `Sales.View` | OK | Convert route to `asp-page` |
| Sales | `/Sales/Details/{id}` | Edit order button | `Sales.Edit` and Draft status | `/Sales/Edit/{id}` requires `Sales.Edit` | OK | Convert route to `asp-page` |
| Sales | `/Sales/Details/{id}` | Confirm button | `Sales.Confirm` and Draft status | Sales confirm app service | OK | Add confirmation and notification |
| Sales | `/Sales/Details/{id}` | Cancel button | `Sales.Cancel` and Draft status | Sales cancel app service | OK | Add confirmation and notification |
| Sales | `/Sales/Details/{id}` | Total profit and line profit fields | `Sales.ViewProfit` | Sales profit permission | Fix Required | Hide profit amount unless `Sales.ViewProfit` is granted |
| Sales | `/Sales/Details/{id}` | Cost-related fields if added later | `Sales.ViewCost` | Sales cost permission | Needs Review | Ensure future cost UI checks `Sales.ViewCost` |
| Sales | `/Sales/History` | Total profit field | `Sales.ViewProfit` | Sales profit permission | Fix Required | Hide profit unless `Sales.ViewProfit` is granted |
| Sales | `/Sales/CustomerHistory` | Profit field | `Sales.ViewProfit` | Page currently has `Sales.ViewProfit` | OK | Keep both page and field-level guard |
| Sales | `/Sales/Edit/{id}` | Add line | `Sales.Edit` | Sales add line app service | OK | Add selectors and notification only |
| Sales | `/Sales/Edit/{id}` | Update line | `Sales.Edit` | Sales update line app service | OK | Add confirmation where price override changes |
| Sales | `/Sales/Edit/{id}` | Remove line | `Sales.Edit` | Sales remove line app service | Partial | Add confirmation |
| Sales | `/Sales/Edit/{id}` | Actual Selling Price override | `Sales.OverridePrice` when overriding | Sales override behavior | Needs Review | Confirm current backend enforcement; UI should hide/disable override unless allowed |
| Audit | `/Audit` | Export link | `Audit.Export` | `/Audit/Export` requires `Audit.Export` | OK | Convert route to `asp-page` |
| Audit | `/Audit` | Detail link | `Audit.View` | `/Audit/Details/{id}` requires `Audit.View` | OK | Convert route to `asp-page`; consider detail modal |
| Audit | `/Audit/Export` | Export submit | `Audit.Export` | Audit export app service | OK | Add busy state and completion notification |

## Needs Human Review

- Inventory cost/value fields are covered by existing Inventory permissions.
  Do not add a new permission in this refactor.
- Whether Sales actual price override UI should be disabled entirely without
  `Sales.OverridePrice`, or allowed when actual price equals suggested price.
- Whether Customer History should stay protected by both
  `Sales.ViewCustomerHistory` and `Sales.ViewProfit`, or split profit columns
  behind field-level visibility.

## Step 2.2 Menu Permission Review

Reviewed `VPureLuxMenuContributor` menu permissions against destination pages:

| Menu Item | Destination | Menu Permission | Destination/Page Permission | Status |
|---|---|---|---|---|
| Home | `/` | none | public/auth landing behavior | OK |
| Host Dashboard | `/HostDashboard` | `Dashboard.Host` | `Dashboard.Host` | OK |
| Tenant Dashboard | `/Dashboard` | `Dashboard.Tenant` | `Dashboard.Tenant` | OK |
| Catalog | parent only | `Catalog.Group` | child items enforce exact permissions | OK |
| Catalog Components | `/Catalog/Components` | `Catalog.Components.View` | `Catalog.Components.View` | OK |
| Catalog Products | `/Catalog/Products` | `Catalog.Products.View` | `Catalog.Products.View` | OK |
| BOM | `/Bom` | `Bom.View` | `Bom.View` | OK |
| Customers | `/Customers` | `Customers.View` | `Customers.View` | OK |
| Customer Groups | `/CustomerGroups` | `CustomerGroups.View` | `CustomerGroups.View` | OK |
| Pricing | `/Pricing` | `Pricing.View` | `Pricing.View` | OK |
| Inventory | `/Inventory` | `Inventory.View` | `Inventory.View` | OK |
| Sales | `/Sales` | `Sales.View` | `Sales.View` | OK |
| Audit | `/Audit` | `Audit.View` | `Audit.View` | OK |

Step 2.2 result:
- No menu permission changes were required.
- Menu labels use localization keys.
- Lower-level actions such as Pricing History, Sales Customer History, Inventory Ledger, and Audit Export remain page/action-level links rather than main menu items in this phase.
