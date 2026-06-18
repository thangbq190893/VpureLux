# UI Route Preservation Matrix

All routes are marked **preserve first**. No public/internal route should be
changed during the first UI architecture refactor unless a safe redirect is
explicitly approved.

## Current Razor Page Routes

| Current Route | Current File | Target Razor Page | Route Parameters | Preserve/Redirect Decision | Required Fix |
|---|---|---|---|---|---|
| `/` | `Pages/Index.cshtml` | `Index` | none | Preserve first | ERP landing content needs separate approval |
| `/HostDashboard` | `Pages/HostDashboard.cshtml` | `HostDashboard` | none | Preserve first | No route change |
| `/Dashboard` | `Pages/TenantDashboard.cshtml` | `TenantDashboard` | none | Preserve first | No route change |
| `/CookiePolicy` | `Pages/CookiePolicy.cshtml` | `CookiePolicy` | none | Preserve first | Localize or isolate template content |
| `/PrivacyPolicy` | `Pages/PrivacyPolicy.cshtml` | `PrivacyPolicy` | none | Preserve first | Localize or isolate template content |
| `/Catalog/Products` | `Pages/Catalog/Products/Index.cshtml` | `Catalog/Products/Index` | none | Preserve first | Replace hardcoded links with tag helpers |
| `/Catalog/Products/Create` | `Pages/Catalog/Products/Create.cshtml` | `Catalog/Products/Create` | none | Preserve first | Use `asp-page` callers |
| `/Catalog/Products/Edit/{id}` | `Pages/Catalog/Products/Edit.cshtml` | `Catalog/Products/Edit` | `id:guid` | Preserve first | Use `asp-route-id` callers |
| `/Catalog/Components` | `Pages/Catalog/Components/Index.cshtml` | `Catalog/Components/Index` | none | Preserve first | Replace hardcoded links with tag helpers |
| `/Catalog/Components/Create` | `Pages/Catalog/Components/Create.cshtml` | `Catalog/Components/Create` | none | Preserve first | Use `asp-page` callers |
| `/Catalog/Components/Edit/{id}` | `Pages/Catalog/Components/Edit.cshtml` | `Catalog/Components/Edit` | `id:guid` | Preserve first | Use `asp-route-id` callers |
| `/Bom` | `Pages/Bom/Index.cshtml` | `Bom/Index` | none | Preserve first | Improve selector only |
| `/Bom/Product/{productId}` | `Pages/Bom/Product.cshtml` | `Bom/Product` | `productId:guid` | Preserve first | Use `asp-route-productId` callers |
| `/Bom/Create/{productId}` | `Pages/Bom/Create.cshtml` | `Bom/Create` | `productId:guid` | Preserve first | Use `asp-page` callers |
| `/Bom/Details/{id}` | `Pages/Bom/Details.cshtml` | `Bom/Details` | `id:guid` | Preserve first | Use `asp-route-id` callers |
| `/Bom/Edit/{id}` | `Pages/Bom/Edit.cshtml` | `Bom/Edit` | `id:guid` | Preserve first | Use `asp-route-id` callers |
| `/Bom/Clone/{id}` | `Pages/Bom/Clone.cshtml` | `Bom/Clone` | `id:guid` | Preserve first | Use `asp-route-id` callers |
| `/Customers` | `Pages/Customers/Index.cshtml` | `Customers/Index` | none | Preserve first | Use tag helper links |
| `/Customers/Create` | `Pages/Customers/Create.cshtml` | `Customers/Create` | none | Preserve first | Use tag helper links |
| `/Customers/Details/{id}` | `Pages/Customers/Details.cshtml` | `Customers/Details` | `id:guid` | Preserve first | Use `asp-route-id` callers |
| `/Customers/Edit/{id}` | `Pages/Customers/Edit.cshtml` | `Customers/Edit` | `id:guid` | Preserve first | Use `asp-route-id` callers |
| `/CustomerGroups` | `Pages/CustomerGroups/Index.cshtml` | `CustomerGroups/Index` | none | Preserve first | Use tag helper links |
| `/CustomerGroups/Create` | `Pages/CustomerGroups/Create.cshtml` | `CustomerGroups/Create` | none | Preserve first | Use tag helper links |
| `/CustomerGroups/Details/{id}` | `Pages/CustomerGroups/Details.cshtml` | `CustomerGroups/Details` | `id:guid` | Preserve first | Use `asp-route-id` callers |
| `/CustomerGroups/Edit/{id}` | `Pages/CustomerGroups/Edit.cshtml` | `CustomerGroups/Edit` | `id:guid` | Preserve first | Use `asp-route-id` callers |
| `/Pricing` | `Pages/Pricing/Index.cshtml` | `Pricing/Index` | none | Preserve first | Align history link permission |
| `/Pricing/Components/{componentId}` | `Pages/Pricing/Components/History.cshtml` | `Pricing/Components/History` | `componentId:guid` | Preserve first | Use `asp-route-componentId` callers |
| `/Pricing/Components/Create/{componentId}` | `Pages/Pricing/Components/Create.cshtml` | `Pricing/Components/Create` | `componentId:guid` | Preserve first | Use `asp-route-componentId` callers |
| `/Pricing/Products/{productId}` | `Pages/Pricing/Products/History.cshtml` | `Pricing/Products/History` | `productId:guid` | Preserve first | Use `asp-route-productId` callers |
| `/Pricing/Products/Create/{productId}` | `Pages/Pricing/Products/Create.cshtml` | `Pricing/Products/Create` | `productId:guid` | Preserve first | Use `asp-route-productId` callers |
| `/Inventory` | `Pages/Inventory/Index.cshtml` | `Inventory/Index` | none | Preserve first | Convert hub links to tag helpers or menu |
| `/Inventory/Warehouses` | `Pages/Inventory/Warehouses.cshtml` | `Inventory/Warehouses` | none | Preserve first | Use tag helper links |
| `/Inventory/Receipt` | `Pages/Inventory/Receipt.cshtml` | `Inventory/Receipt` | none | Preserve first | No route change |
| `/Inventory/Issue` | `Pages/Inventory/Issue.cshtml` | `Inventory/Issue` | none | Preserve first | No route change |
| `/Inventory/Adjustment` | `Pages/Inventory/Adjustment.cshtml` | `Inventory/Adjustment` | none | Preserve first | No route change |
| `/Inventory/Balances` | `Pages/Inventory/Balances.cshtml` | `Inventory/Balances` | none | Preserve first | No route change |
| `/Inventory/Lots` | `Pages/Inventory/Lots.cshtml` | `Inventory/Lots` | none | Preserve first | No route change |
| `/Inventory/Ledger` | `Pages/Inventory/Ledger.cshtml` | `Inventory/Ledger` | none | Preserve first | No route change |
| `/Sales` | `Pages/Sales/Index.cshtml` | `Sales/Index` | none | Preserve first | Use tag helper links |
| `/Sales/Create` | `Pages/Sales/Create.cshtml` | `Sales/Create` | none | Preserve first | Use tag helper links |
| `/Sales/Details/{id}` | `Pages/Sales/Details.cshtml` | `Sales/Details` | `id:guid` | Preserve first | Use `asp-route-id` callers |
| `/Sales/Edit/{id}` | `Pages/Sales/Edit.cshtml` | `Sales/Edit` | `id:guid` | Preserve first | Use `asp-route-id` callers |
| `/Sales/History` | `Pages/Sales/History.cshtml` | `Sales/History` | none | Preserve first | Use tag helper links |
| `/Sales/CustomerHistory` | `Pages/Sales/CustomerHistory.cshtml` | `Sales/CustomerHistory` | optional query `CustomerId` | Preserve first | Use tag helper links |
| `/Audit` | `Pages/Audit/Index.cshtml` | `Audit/Index` | query filters | Preserve first | Use tag helper links |
| `/Audit/Details/{id}` | `Pages/Audit/Details.cshtml` | `Audit/Details` | `id:guid` | Preserve first | Use `asp-route-id` callers |
| `/Audit/Reports` | `Pages/Audit/Reports.cshtml` | `Audit/Reports` | none | Preserve first | Use tag helper links |
| `/Audit/Export` | `Pages/Audit/Export.cshtml` | `Audit/Export` | none | Preserve first | Use tag helper links |

## Hardcoded Internal Href Findings

Original audited count: 61 `href="/..."` occurrences in ERP Razor pages.
Current count after Step 2.1: 1 `href="/..."` occurrence in ERP Razor pages.

## Step 2.1 Route Hardening Status

Completed:
- Replaced ERP module Razor Page navigation links with `asp-page` and `asp-route-*`.
- Fixed all `<abp-button href="...">` navigation misuse.
- Preserved all existing Razor Page route templates.
- Applied `Pricing.History` visibility to Pricing history links.
- Applied destination permission visibility to Sales Customer History link.
- Did not restructure menus, move pages, add redirects, or change route templates.

Remaining hardcoded `href="/..."`:
- `Pages/Index.cshtml` uses `/Account/Login`. This is an ABP Account module route, not an ERP module Razor Page navigation route, and was preserved in Step 2.1.

| Current File | Hardcoded Routes Found | Preserve/Redirect Decision | Required Fix |
|---|---|---|---|
| `Pages/Catalog/Products/Index.cshtml` | `/Catalog/Products/Create`, `/Catalog/Products/Edit/{id}` | Preserve first | Replace with `asp-page` / `asp-route-id`; fix `abp-button href` |
| `Pages/Catalog/Products/Create.cshtml` | `/Catalog/Products` | Preserve first | Replace with `asp-page="/Catalog/Products/Index"` |
| `Pages/Catalog/Products/Edit.cshtml` | `/Catalog/Products` | Preserve first | Replace with `asp-page="/Catalog/Products/Index"` |
| `Pages/Catalog/Components/Index.cshtml` | `/Catalog/Components/Create`, `/Catalog/Components/Edit/{id}` | Preserve first | Replace with `asp-page` / `asp-route-id`; fix `abp-button href` |
| `Pages/Catalog/Components/Create.cshtml` | `/Catalog/Components` | Preserve first | Replace with `asp-page="/Catalog/Components/Index"` |
| `Pages/Catalog/Components/Edit.cshtml` | `/Catalog/Components` | Preserve first | Replace with `asp-page="/Catalog/Components/Index"` |
| `Pages/Bom/Product.cshtml` | `/Bom/Create/{productId}`, `/Bom/Details/{id}`, `/Bom/Edit/{id}`, `/Bom/Clone/{id}` | Preserve first | Replace with page tag helpers; fix `abp-button href` |
| `Pages/Bom/Create.cshtml` | `/Bom/Product/{productId}` | Preserve first | Replace with `asp-page="/Bom/Product"` |
| `Pages/Bom/Edit.cshtml` | `/Bom/Details/{id}` | Preserve first | Replace with `asp-page="/Bom/Details"` |
| `Pages/Bom/Clone.cshtml` | `/Bom/Details/{id}` | Preserve first | Replace with `asp-page="/Bom/Details"` |
| `Pages/Bom/Details.cshtml` | `/Bom/Edit/{id}`, `/Bom/Product/{productId}` | Preserve first | Replace with `asp-page` / route values |
| `Pages/Customers/Index.cshtml` | `/Customers/Create`, `/Customers/Details/{id}`, `/Customers/Edit/{id}` | Preserve first | Replace with page tag helpers |
| `Pages/Customers/Create.cshtml` | `/Customers` | Preserve first | Replace with `asp-page="/Customers/Index"` |
| `Pages/Customers/Edit.cshtml` | `/Customers` | Preserve first | Replace with `asp-page="/Customers/Index"` |
| `Pages/Customers/Details.cshtml` | `/Customers` | Preserve first | Replace with `asp-page="/Customers/Index"` |
| `Pages/CustomerGroups/Index.cshtml` | `/CustomerGroups/Create`, `/CustomerGroups/Details/{id}`, `/CustomerGroups/Edit/{id}` | Preserve first | Replace with page tag helpers |
| `Pages/CustomerGroups/Create.cshtml` | `/CustomerGroups` | Preserve first | Replace with `asp-page="/CustomerGroups/Index"` |
| `Pages/CustomerGroups/Edit.cshtml` | `/CustomerGroups` | Preserve first | Replace with `asp-page="/CustomerGroups/Index"` |
| `Pages/CustomerGroups/Details.cshtml` | `/CustomerGroups` | Preserve first | Replace with `asp-page="/CustomerGroups/Index"` |
| `Pages/Pricing/Index.cshtml` | `/Pricing/Components/{id}`, `/Pricing/Products/{id}` | Preserve first | Replace with page tag helpers and apply `Pricing.History` visibility |
| `Pages/Pricing/Components/History.cshtml` | `/Pricing/Components/Create/{componentId}`, `/Pricing` | Preserve first | Replace with page tag helpers |
| `Pages/Pricing/Components/Create.cshtml` | `/Pricing/Components/{componentId}` | Preserve first | Replace with page tag helpers |
| `Pages/Pricing/Products/History.cshtml` | `/Pricing/Products/Create/{productId}`, `/Pricing` | Preserve first | Replace with page tag helpers |
| `Pages/Pricing/Products/Create.cshtml` | `/Pricing/Products/{productId}` | Preserve first | Replace with page tag helpers |
| `Pages/Inventory/Index.cshtml` | `/Inventory/Warehouses`, `/Inventory/Receipt`, `/Inventory/Issue`, `/Inventory/Adjustment`, `/Inventory/Balances`, `/Inventory/Lots`, `/Inventory/Ledger` | Preserve first | Replace with page tag helpers or menu items |
| `Pages/Sales/Index.cshtml` | `/Sales/CustomerHistory`, `/Sales/History`, `/Sales/Create`, `/Sales/Details/{id}` | Preserve first | Replace with page tag helpers and permission-align history |
| `Pages/Sales/Create.cshtml` | `/Sales` | Preserve first | Replace with `asp-page="/Sales/Index"` |
| `Pages/Sales/Details.cshtml` | `/Sales/Edit/{id}`, `/Sales` | Preserve first | Replace with page tag helpers |
| `Pages/Sales/Edit.cshtml` | `/Sales/Details/{id}` | Preserve first | Replace with page tag helper |
| `Pages/Sales/History.cshtml` | `/Sales/Details/{id}` | Preserve first | Replace with page tag helper |
| `Pages/Audit/Index.cshtml` | `/Audit/Reports`, `/Audit/Export`, `/Audit/Details/{id}` | Preserve first | Replace with page tag helpers |
| `Pages/Audit/Reports.cshtml` | `/Audit?Input.Module=Pricing`, `/Audit?Input.Module=BOM`, `/Audit?Input.Module=Inventory`, `/Audit?Input.Module=Sales` | Preserve first | Replace with `asp-page="/Audit/Index"` and query route values |
| `Pages/Index.cshtml` | `/Account/Login` | Preserve first | Confirm whether ERP home remains in scope |

## Other Route-Related Findings

- `VPureLuxMenuContributor` already uses `~` application-root URLs and
  `RequirePermissions`; these should be preserved.
- Some routes are absolute `@page` routes under Pricing. They are valid and must
  be preserved during refactor.
- No safe redirect currently exists for renamed module folders; therefore all
  current routes remain the stable external surface.

## Step 2.2 Menu Route Consistency Review

Reviewed menu sources:
- `src/VPureLux.Web/Menus/VPureLuxMenuContributor.cs`
- `src/VPureLux.Web/Menus/VPureLuxMenus.cs`

Menu route status:
- `~/` -> `/`: preserved.
- `~/HostDashboard` -> `/HostDashboard`: preserved.
- `~/Dashboard` -> `/Dashboard`: preserved.
- `~/Catalog/Components` -> `/Catalog/Components`: preserved.
- `~/Catalog/Products` -> `/Catalog/Products`: preserved.
- `~/Bom` -> `/Bom`: preserved.
- `~/Customers` -> `/Customers`: preserved.
- `~/CustomerGroups` -> `/CustomerGroups`: preserved.
- `~/Pricing` -> `/Pricing`: preserved.
- `~/Inventory` -> `/Inventory`: preserved.
- `~/Sales` -> `/Sales`: preserved.
- `~/Audit` -> `/Audit`: preserved.

Step 2.2 result:
- No menu route changes were required.
- No route templates were changed.
- Menu hierarchy restructuring remains deferred to a separately approved phase.

## Step 3.1 Customer Reference UI Route Audit

Reviewed Customer and CustomerGroup pages before modal implementation:

| Route | Current Page | Current PageModel | Modal Candidate | Preservation Requirement |
|---|---|---|---|---|
| `/Customers` | `Pages/Customers/Index.cshtml` | `Customers.IndexModel` | No; list remains full page | Preserve as primary Customer list route |
| `/Customers/Create` | `Pages/Customers/Create.cshtml` | `Customers.CreateModel` | Yes; create modal candidate | Preserve route as full-page fallback or modal view route |
| `/Customers/Edit/{id}` | `Pages/Customers/Edit.cshtml` | `Customers.EditModel` | Yes; edit modal candidate | Preserve route and `id:guid` parameter |
| `/Customers/Details/{id}` | `Pages/Customers/Details.cshtml` | `Customers.DetailsModel` | Yes; read-only detail modal candidate | Preserve route and `id:guid` parameter |
| `/CustomerGroups` | `Pages/CustomerGroups/Index.cshtml` | `CustomerGroups.IndexModel` | No; list remains full page | Preserve as primary CustomerGroup list route |
| `/CustomerGroups/Create` | `Pages/CustomerGroups/Create.cshtml` | `CustomerGroups.CreateModel` | Yes; create modal candidate | Preserve route as full-page fallback or modal view route |
| `/CustomerGroups/Edit/{id}` | `Pages/CustomerGroups/Edit.cshtml` | `CustomerGroups.EditModel` | Yes; edit modal candidate | Preserve route and `id:guid` parameter |
| `/CustomerGroups/Details/{id}` | `Pages/CustomerGroups/Details.cshtml` | `CustomerGroups.DetailsModel` | Yes; read-only detail modal candidate | Preserve route and `id:guid` parameter |

Step 3.1 route findings:
- Customer and CustomerGroup pages already use Razor Page tag helpers for internal navigation.
- No hardcoded `href="/..."` remains in the audited Customer folders.
- No `<abp-button href="...">` misuse remains in the audited Customer folders.
- Modal conversion must not remove or rename the current routes unless a separately approved redirect/fallback strategy exists.
