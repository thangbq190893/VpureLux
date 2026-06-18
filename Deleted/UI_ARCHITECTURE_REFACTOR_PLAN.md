# UI Architecture Refactor Plan

## 1. Purpose and Boundaries

This plan defines a presentation-layer refactor for the certified ERP backend.
The objective is to align the Razor Pages UI with ABP Framework conventions,
improve long-term maintainability, and make daily operational workflows easier
to use.

The refactor must not change:

- Domain rules, aggregate behavior, or state machines.
- Application service behavior or business validation.
- Repository, EF Core, database, or migration behavior.
- Existing HTTP API business contracts unless a presentation-only query
  contract is separately approved.
- Certified module outcomes.

The refactor should preserve existing public routes during transition. Route
changes, if any, require redirects and regression tests.

## 2. Audit Scope and Measured Baseline

The audit covered:

- `src/VPureLux.Web/Pages`
- LeptonX application layout overrides
- Navigation contributors
- Localization resources
- JavaScript and CSS assets
- ABP tag helpers, permissions, page authorization, and UI interaction patterns
- `VPureLux.Web.Public` at a high level to distinguish public-template content
  from ERP presentation concerns

Measured baseline:

| Item | Current count |
|---|---:|
| ERP PageModels reviewed | 43 |
| Razor Pages in `VPureLux.Web/Pages` | 49 |
| Internal routes written directly as `href="/..."` | 61 |
| `abp-button` elements incorrectly carrying `href` | 3 |
| Inline JavaScript blocks in ERP Razor Pages | 2 |
| Raw page-level `<script src>` registrations | 4 |
| Business-focused page JavaScript files | 1 |
| Pages compressed to 12 lines or fewer | 22 |
| Uses of `abp.ajax`, `abp.notify`, `abp.message`, or `ModalManager` | 0 |
| Localization keys used by Razor but absent from local `vi-VN.json` | 4 |

The four locally missing keys are `Create`, `Delete`, `Login`, and `Refresh`.
They may currently resolve through inherited ABP resources, but relying on that
implicitly makes Vietnamese localization incomplete and harder to audit.

---

# A. Current-State Assessment

## A1. Overall Assessment

The current UI is functionally connected to the certified backend, but it is
primarily server-postback Razor markup rather than a structured ABP UI layer.
ABP authentication, authorization, navigation, localization injection, LeptonX,
and several Bootstrap tag helpers are present. However, page composition,
client-side behavior, route generation, feedback, selectors, and modal patterns
are inconsistent.

The main maintainability issue is not any single page. It is the absence of a
repeatable presentation pattern across modules.

## A2. Incorrect or Inconsistent ABP Tag Helper Usage

### Confirmed misuse

`<abp-button>` is used with `href` in:

- `Pages/Catalog/Products/Index.cshtml`
- `Pages/Catalog/Components/Index.cshtml`
- `Pages/Bom/Product.cshtml`

These are navigation actions, not form buttons. They should use an anchor with
ABP button styling and Razor route generation, for example an anchor tag helper
with `asp-page` and `asp-route-*`.

### Inconsistent usage

- Many pages mix `<abp-button>` with raw Bootstrap `<button>` and `<a
  class="btn">` without a clear convention.
- Many forms use manually composed labels, inputs, validation spans, and grid
  markup even where ABP form helpers can provide a consistent structure.
- Tables use `<abp-table>` only as styled static tables. List pages do not use a
  consistent paging, sorting, empty-state, or action-menu pattern.
- Several pages are compressed into single-line markup, which makes review,
  localization checks, and safe UI changes unnecessarily difficult.

## A3. JavaScript Architecture Findings

### Existing assets

- `Pages/Catalog/catalog-image-preview.js`
- Dashboard JavaScript files
- A nearly empty `wwwroot/global-scripts.js`

### Missing module-level JavaScript

Catalog, BOM, Customer, Pricing, Inventory, Sales, and Audit do not have a
consistent page-level JavaScript structure such as `Index.js`, modal scripts,
workflow scripts, or registered bundle contributors.

### Missing or inconsistent registration

- Catalog image pages register JavaScript through raw `<script>` tags rather
  than ABP script tag helpers or bundle contributors.
- Catalog uses an uppercase `Scripts` section while dashboard pages use the
  lowercase `scripts` convention. Section naming should be standardized.
- BOM Create and Edit contain duplicated inline JavaScript for adding, removing,
  and re-indexing component rows.
- No module declares explicit script dependencies or bundling ownership.

### Missing ABP interaction patterns

There is no current use of:

- `abp.ModalManager`
- `abp.ajax`
- generated JavaScript service proxies
- `abp.notify.success` / `abp.notify.error`
- `abp.message.confirm`
- standardized busy/loading indicators

As a result, status changes, deletes/removals, publish/archive operations,
confirm/cancel operations, and posting workflows generally execute through full
page posts without confirmation or clear success feedback.

## A4. Modal Pattern Findings

No ERP page currently uses the ABP modal pattern.

Suitable modal candidates:

- Create/Edit/Details for Customer and Customer Group.
- Warehouse create/edit/status operations.
- Create new Pricing version.
- Catalog image upload/replace/remove confirmation.
- Read-only Audit detail.
- Simple confirmation dialogs for deactivate, publish, archive, cancel, remove
  image, and remove sales line.

Workflows that should remain full pages:

- BOM editor.
- Inventory receipt, issue, and adjustment posting.
- Sales order create/edit/details.
- Audit reports and export.

## A5. Route Findings

There are 61 internal hardcoded routes in Razor markup.

Examples include:

- `/Catalog/Products/Edit/{id}`
- `/Bom/Details/{id}`
- `/Pricing/Components/{id}`
- `/Inventory/Receipt`
- `/Sales/Details/{id}`
- `/Audit/Details/{id}`

Risks:

- Route changes silently break navigation.
- Route values are manually concatenated.
- Route testing becomes string-based rather than page-based.
- Links do not benefit from Razor Page route validation.

Target convention:

- Use `asp-page`, `asp-route-*`, and `Url.Page`.
- Use named constants only where Razor Page tag helpers cannot apply.
- Keep API routes out of Razor markup unless the element genuinely consumes a
  binary API resource, such as Catalog thumbnails.

## A6. Localization Findings

The only application localization JSON is `vi-VN.json`, but most values are
English. The resource therefore declares Vietnamese culture without delivering
a Vietnamese operational UI.

Confirmed issues:

- Catalog list headers contain hardcoded English text: `Code`, `Name`, `Unit`,
  `Status`, and `Actions`.
- Starter home, cookie policy, privacy policy, and footer contain extensive
  hardcoded English content.
- The LeptonX footer override contains hardcoded vendor text and placeholder
  links.
- Status enum values are frequently rendered directly instead of through
  localized labels.
- Dates, quantities, prices, and VND values do not use a shared presentation
  format.
- Four Razor localization keys are absent locally.
- JavaScript has no localization strategy because there is almost no
  module-level JavaScript.

## A7. Permission-Aware Rendering Findings

Strengths:

- Main navigation items use `RequirePermissions`.
- Most PageModels have page-level authorization.
- Catalog, BOM, Customer, Inventory, Sales, Pricing, and Audit implement some
  permission-aware action visibility.

Gaps:

- Pricing Index exposes history links under `Pricing.View`, while destination
  pages require `Pricing.History`. Users may see an action that leads to access
  denial.
- Sales Details and Sales History render profit values without checking
  `Sales.ViewProfit`.
- Presentation rules for `Sales.ViewCost` and `Sales.ViewProfit` are not
  centralized.
- Confirmation is absent before sensitive authorized actions.
- Catalog Index pages rely on folder authorization conventions rather than an
  explicit page attribute. This is valid ABP behavior, but the mixed strategy
  makes authorization auditing less obvious.

Target rule:

Every visible action and sensitive field must use the same permission that
protects the destination or operation.

## A8. Image UI Findings

Strengths:

- Catalog product and component lists show lazy-loaded thumbnails.
- Catalog create/edit pages provide local image preview.
- General list DTOs do not embed Base64 content.

Gaps:

- Catalog preview JavaScript is registered inconsistently.
- Object URLs are not explicitly revoked after preview replacement.
- No standardized client-side file-size/type feedback exists.
- Remove-image actions have no confirmation dialog or success notification.
- Sales item selection does not display Catalog images because sales item entry
  currently uses raw identifiers rather than a searchable visual selector.
- Placeholder and thumbnail presentation is duplicated rather than implemented
  as a shared partial/component.

## A9. Searchable Selector Findings

Raw GUID entry is a major usability blocker for operators.

Affected workflows include:

- BOM product selection and BOM component lines.
- Inventory warehouse, stock item, and lot selection.
- Sales customer, warehouse, Catalog item, and BOM version selection.
- Sales customer-history lookup.
- Several inquiry screens display WarehouseId, StockItemId, ProductId, or
  ComponentId instead of recognizable code/name values.

Customer and warehouse dropdowns exist in parts of Sales, but they are not
searchable and do not establish a shared selector pattern.

Target selectors should support:

- Search by code and name.
- Permission-safe query results.
- Active/inactive indication.
- Catalog thumbnail where relevant.
- Type filtering for Product versus Component.
- Published BOM version filtering.
- Warehouse-aware stock availability where relevant.

## A10. Module-by-Module UI Findings

### Catalog

- Good baseline thumbnail and image preview support.
- Incorrect `abp-button href` usage.
- Hardcoded table headers and hardcoded page links.
- No confirmation/notification for deactivation or image removal.
- Image JavaScript registration is not ABP-compliant.
- List pages should use a shared thumbnail component and consistent action menu.

### BOM

- Create/Edit duplicate inline JavaScript.
- Product and Component are selected by raw GUID.
- Details/history display raw identifiers rather than product/component labels.
- Publish/archive actions lack confirmation and feedback.
- BOM editor needs a dedicated reusable JavaScript component, not inline code.

### Customer

- Customers and Customer Groups are split into separate top-level folders and
  top-level navigation items.
- CRUD pages are strong modal candidates.
- List actions are full-page posts without confirmation/notification.
- Customer Group dropdown is not searchable.
- Markup is heavily compressed and difficult to maintain.

### Pricing

- Current/history/timeline structure is useful.
- History action visibility is not aligned with `Pricing.History`.
- New-version creation is a modal candidate.
- Product/component tables have no search, paging, or thumbnail support.
- Dates and VND values need consistent formatting.

### Inventory

- Operational pages are minimal forms with raw identifiers.
- Receipt, issue, and adjustment only support the first line in the UI.
- No searchable warehouse/stock-item/lot selectors.
- Inquiry pages display identifiers rather than operator-friendly labels.
- No posting confirmation, busy state, result summary, or success notification.
- Inventory navigation is implemented as a page-level link hub instead of a
  structured module submenu.

### Sales

- Core workflow pages exist, but item entry uses raw identifiers.
- Catalog images are not shown during item selection.
- No searchable customer, warehouse, item, or BOM selectors.
- Profit fields are not consistently permission-aware.
- Confirm, cancel, remove-line, and override workflows lack confirmation and
  feedback.
- Dense single-line markup makes future changes risky.

### Audit

- Search, detail, reports, and export pages exist.
- Search fields are minimal and do not cover all supported filters in an
  operator-friendly way.
- Detail JSON is rendered raw rather than as readable change sections.
- Audit detail is a suitable read-only modal from the list.
- Export has no progress/busy feedback or completion notification.
- Hardcoded routes remain throughout.

### Shared Layout and Navigation

- The LeptonX application layout is largely retained, which is preferable to a
  custom layout rewrite.
- The footer override contains hardcoded vendor content and placeholder links.
- The ERP home page is still the ABP starter page and is not an operational ERP
  dashboard.
- Navigation is mostly flat. Customer Groups is separated from Customers;
  Inventory subfunctions are hidden behind an internal hub page; Audit and
  Pricing have no useful submenu structure.
- `VPureLux.Web.Public` remains largely template content. It should be treated as
  a separate public-site workstream and must not be mixed into the ERP UI
  refactor unless explicitly approved.

---

# B. Target ABP Architecture

## B1. Architectural Principles

1. Keep Razor Pages as the presentation framework.
2. Keep business decisions in certified application services.
3. Use generated JavaScript proxies or `abp.ajax` only to call existing
   application/API operations.
4. Use ABP ModalManager for small CRUD and read-only detail dialogs.
5. Keep complex operational workflows as full pages.
6. Use Razor Page tag helpers for all internal navigation.
7. Use one permission source for page authorization, action visibility, and
   sensitive-field visibility.
8. Use localized text in Razor and JavaScript.
9. Give every asynchronous operation a busy state, confirmation where needed,
   and success/error feedback.
10. Build shared selectors and visual components rather than repeating raw GUID
    inputs.

## B2. Shared Presentation Components

Recommended shared presentation assets:

```text
Pages/
  Shared/
    Components/
      CatalogItemSelector/
      CustomerSelector/
      WarehouseSelector/
      StockItemSelector/
      BomVersionSelector/
      CatalogThumbnail/
      StatusBadge/
      MoneyDisplay/
      EmptyState/
    Partials/
      _PageHeader.cshtml
      _ActionMenu.cshtml
      _ValidationSummary.cshtml
wwwroot/
  js/
    shared/
      selectors.js
      notifications.js
      confirmation.js
      formatting.js
  css/
    shared/
      erp-operations.css
```

Shared selectors must call existing query/application endpoints where possible.
If an existing endpoint cannot support a safe selector, any new presentation
query contract requires separate approval because this refactor may not alter
business logic.

## B3. Target Folder Structure by Module

The paths below describe presentation ownership. Existing routes should remain
stable during the first refactor pass.

### Catalog

```text
Pages/Catalog/
  Products/
    Index.cshtml
    Index.cshtml.cs
    Index.js
    CreateModal.cshtml
    CreateModal.cshtml.cs
    EditModal.cshtml
    EditModal.cshtml.cs
    ImageModal.cshtml
    ImageModal.cshtml.cs
  Components/
    Index.cshtml
    Index.cshtml.cs
    Index.js
    CreateModal.cshtml
    CreateModal.cshtml.cs
    EditModal.cshtml
    EditModal.cshtml.cs
    ImageModal.cshtml
    ImageModal.cshtml.cs
  catalog-image.js
```

### BOM

```text
Pages/Bom/
  Index.cshtml
  Index.cshtml.cs
  Index.js
  Product.cshtml
  Product.cshtml.cs
  Product.js
  Create.cshtml
  Create.cshtml.cs
  Edit.cshtml
  Edit.cshtml.cs
  BomEditor.js
  DetailsModal.cshtml
  DetailsModal.cshtml.cs
  CloneModal.cshtml
  CloneModal.cshtml.cs
```

### Customer

```text
Pages/Customer/
  Customers/
    Index.cshtml
    Index.cshtml.cs
    Index.js
    CreateModal.cshtml
    CreateModal.cshtml.cs
    EditModal.cshtml
    EditModal.cshtml.cs
    DetailsModal.cshtml
    DetailsModal.cshtml.cs
  Groups/
    Index.cshtml
    Index.cshtml.cs
    Index.js
    CreateModal.cshtml
    CreateModal.cshtml.cs
    EditModal.cshtml
    EditModal.cshtml.cs
    DetailsModal.cshtml
    DetailsModal.cshtml.cs
```

During transition, preserve `/Customers` and `/CustomerGroups` routes.

### Pricing

```text
Pages/Pricing/
  Index.cshtml
  Index.cshtml.cs
  Index.js
  Components/
    History.cshtml
    History.cshtml.cs
    History.js
    CreateVersionModal.cshtml
    CreateVersionModal.cshtml.cs
  Products/
    History.cshtml
    History.cshtml.cs
    History.js
    CreateVersionModal.cshtml
    CreateVersionModal.cshtml.cs
```

### Inventory

```text
Pages/Inventory/
  Index.cshtml
  Index.cshtml.cs
  Warehouses/
    Index.cshtml
    Index.cshtml.cs
    Index.js
    CreateModal.cshtml
    CreateModal.cshtml.cs
  Transactions/
    Receipt.cshtml
    Receipt.cshtml.cs
    Receipt.js
    Issue.cshtml
    Issue.cshtml.cs
    Issue.js
    Adjustment.cshtml
    Adjustment.cshtml.cs
    Adjustment.js
  Inquiries/
    Balances.cshtml
    Balances.cshtml.cs
    Balances.js
    Lots.cshtml
    Lots.cshtml.cs
    Lots.js
    Ledger.cshtml
    Ledger.cshtml.cs
    Ledger.js
```

### Sales

```text
Pages/Sales/
  Orders/
    Index.cshtml
    Index.cshtml.cs
    Index.js
    Create.cshtml
    Create.cshtml.cs
    Create.js
    Edit.cshtml
    Edit.cshtml.cs
    Edit.js
    Details.cshtml
    Details.cshtml.cs
    Details.js
  History/
    Index.cshtml
    Index.cshtml.cs
    Index.js
    Customer.cshtml
    Customer.cshtml.cs
    Customer.js
```

### Audit

```text
Pages/Audit/
  Index.cshtml
  Index.cshtml.cs
  Index.js
  DetailsModal.cshtml
  DetailsModal.cshtml.cs
  Reports.cshtml
  Reports.cshtml.cs
  Reports.js
  Export.cshtml
  Export.cshtml.cs
  Export.js
```

## B4. JavaScript Registration Standard

Each interactive page should:

- Own a page-specific `.js` file.
- Register it through `<abp-script>` in the standard lowercase `scripts`
  section, or through an explicit ABP bundle contributor when shared by several
  pages.
- Use an immediately invoked function or module pattern to avoid global
  functions.
- Resolve localized text with ABP localization APIs.
- Use generated service proxies where available.
- Use `abp.ajax` only when a generated proxy is not suitable.
- Use `abp.message.confirm` before sensitive operations.
- Use `abp.notify.success` after successful operations.
- Apply and clear a busy state around asynchronous actions.

No module workflow JavaScript should remain inline in `.cshtml`.

## B5. Navigation Target

Recommended main menu:

```text
Home / Dashboard
Master Data
  Catalog
    Products
    Components
  Customers
    Customers
    Customer Groups
BOM
Pricing
  Component Purchase Prices
  Product Suggested Prices
Inventory
  Overview
  Warehouses
  Receipt
  Issue
  Adjustment
  Balances
  Lots
  Ledger
Sales
  Orders
  Order History
  Customer Purchase History
Control
  Business Audit
    Search
    Reports
    Export
Administration
```

Every menu item must retain `RequirePermissions`. Child menu visibility must
use the exact permission of its destination.

---

# C. Refactoring Roadmap

## Phase 1: Localization Hardening

### Objectives

- Deliver a genuinely Vietnamese operator interface.
- Remove hardcoded user-facing English.
- Establish consistent labels for statuses, dates, quantities, money, and
  actions.

### Tasks

1. Inventory every Razor and JavaScript user-facing string.
2. Translate ERP operational keys in `vi-VN.json`.
3. Add locally owned values for the four currently inherited keys.
4. Replace Catalog hardcoded table headers.
5. Localize direct enum/status rendering.
6. Localize starter home, policy pages, and footer or remove them from the ERP
   operator surface.
7. Define shared VND, quantity, date, and date-time display conventions.
8. Add a CI check for missing localization keys and hardcoded UI text.

### Exit criteria

- No hardcoded operator-facing English in ERP module pages.
- All module statuses and actions display localized labels.
- All Razor and JavaScript localization keys resolve.

## Phase 2: Navigation Restructuring

### Objectives

- Make module functions discoverable.
- Remove internal link hubs where menu structure is more appropriate.
- Eliminate hardcoded route strings.

### Tasks

1. Introduce grouped module navigation with permission-aware child items.
2. Keep current routes stable.
3. Replace hardcoded internal `href` values with `asp-page` and `asp-route-*`.
4. Correct all `abp-button href` misuse.
5. Add breadcrumbs/page headers consistently.
6. Align active menu state with actual Razor Page routes.
7. Treat the public website navigation as a separate workstream.

### Exit criteria

- Zero `abp-button` elements with `href`.
- Zero manually concatenated internal entity routes.
- All menu children use exact destination permissions.

## Phase 3: ABP UI Pattern Compliance

### Objectives

- Establish a repeatable list/detail/create/edit pattern.
- Improve permission safety and operator feedback.

### Tasks

1. Define standard page templates for:
   - list/inquiry
   - full-page operational workflow
   - create/edit modal
   - read-only detail modal
2. Convert suitable Customer, Customer Group, Warehouse, Pricing-version, and
   Audit-detail flows to `abp.ModalManager`.
3. Add confirmation dialogs for deactivate, remove image, publish, archive,
   confirm order, cancel order, remove line, and inventory posting.
4. Add notifications for successful create/update/post/publish/archive/export
   operations.
5. Align visible actions and sensitive fields with permissions:
   - `Pricing.History`
   - `Sales.ViewProfit`
   - `Sales.ViewCost`
   - all create/edit/status/post/export permissions
6. Standardize action menus, empty states, validation summaries, and error
   presentation.
7. Introduce paging/sorting for list and inquiry pages where supported by
   existing application contracts.

### Exit criteria

- Modal candidates use ABP modal patterns.
- Sensitive operations require confirmation.
- Successful operations provide notifications.
- Permission-denied destinations are never rendered as visible actions.

## Phase 4: JavaScript Architecture Compliance

### Objectives

- Remove inline JavaScript and page-specific global behavior.
- Make client behavior testable and maintainable.

### Tasks

1. Move BOM inline row-management logic into `BomEditor.js`.
2. Register Catalog image behavior with `<abp-script>` or an ABP bundle.
3. Create module/page JavaScript files according to the target structure.
4. Use generated JavaScript service proxies or `abp.ajax`.
5. Add standardized busy, error, confirmation, and notification handling.
6. Use ABP localization from JavaScript.
7. Revoke image preview object URLs and add client-side feedback.
8. Add JavaScript tests for selectors, BOM row management, image preview, and
   sensitive action confirmation.

### Exit criteria

- No inline module JavaScript in Razor Pages.
- No raw page-level `<script src>` tags.
- Every interactive page explicitly owns and registers its JavaScript.

## Phase 5: UI Usability Improvements

### Objectives

- Replace technical identifiers with operational choices.
- Improve speed and confidence for daily users.

### Tasks

1. Implement searchable selectors:
   - Product and Component with code, name, status, and thumbnail.
   - Customer and Customer Group.
   - Warehouse and Stock Item.
   - Lot filtered by warehouse and stock item.
   - Published BOM version filtered by product.
2. Add Catalog images to Sales item selection without snapshotting images.
3. Replace raw IDs in inquiry tables with code/name displays where existing
   contracts already provide them.
4. Improve BOM editor with searchable component rows and clearer validation.
5. Improve Inventory posting pages with repeatable lines, posting summary, and
   clear FIFO-related outcome display.
6. Improve Sales order editing with visual item selection, suggested price
   context, override explanation, and permission-aware profit/cost display.
7. Improve Audit detail with readable metadata/change sections.
8. Add responsive behavior, loading states, empty states, keyboard usability,
   and accessibility review.

### Exit criteria

- Operators do not need to type or interpret GUIDs in normal workflows.
- Sales selection displays relevant Catalog images.
- Operational pages clearly communicate result, status, and next action.

---

# D. Estimated File Impact

The estimates below are planning ranges. Exact counts depend on whether modal
conversion and searchable selectors can use existing query contracts.

| Area | Existing files likely modified | New files likely created | Notes |
|---|---:|---:|---|
| Shared layout, footer, home | 3-5 | 1-3 | Retain LeptonX; use extension points |
| Navigation | 2 | 0-2 | Menu contributor and menu names |
| Localization | 1-3 | 0-7 | Vietnamese hardening; optional module-split JSON |
| Catalog UI | 6-10 | 5-9 | Index scripts, modals, shared thumbnail/image behavior |
| BOM UI | 10-12 | 3-6 | Full-page editor retained; remove inline JS |
| Customer UI | 16-18 | 6-10 | Modal conversion and folder consolidation |
| Pricing UI | 10-12 | 4-7 | History scripts and create-version modals |
| Inventory UI | 16-18 | 8-14 | Highest workflow and selector impact |
| Sales UI | 12-14 | 7-12 | Selectors, images, permission-sensitive fields |
| Audit UI | 8-10 | 4-7 | Detail modal, filters, reports/export feedback |
| Shared selectors/components | 0-2 | 10-18 | Reusable components and scripts |
| Presentation tests | 4-8 | 12-20 | Razor, navigation, permission, JS/UI tests |

Estimated total:

- Existing presentation files affected: approximately **70-95**.
- New presentation/support/test files: approximately **60-105**.
- Business logic, migrations, and domain/infrastructure files affected:
  **zero by default**.

Any need for new selector query contracts must be raised as a separate,
explicitly approved scope item.

---

# E. Risk Assessment

| Risk | Level | Impact | Mitigation |
|---|---|---|---|
| Refactor accidentally changes certified workflow behavior | High | Certification regression | Treat existing app services as immutable; add presentation regression tests before conversion |
| Route changes break bookmarks, menus, or tests | High | Users cannot reach workflows | Preserve routes first; use redirects only after route inventory and approval |
| Permission-aware UI diverges from backend permissions | High | Sensitive data/action exposure or confusing 403 responses | Build a permission matrix per page/action/field and test it |
| Searchable selectors require new query contracts | High | Scope expansion into application layer | Prefer existing endpoints; submit separate approval for presentation query contracts |
| Modal conversion hides validation errors or workflow context | Medium | Poor operator experience | Use modals only for small CRUD/read-only flows; keep complex workflows as full pages |
| JavaScript proxy names or routes change | Medium | Broken asynchronous actions | Centralize proxy calls and add browser/integration tests |
| Vietnamese translation is inconsistent with operational terminology | Medium | User confusion | Review terminology with business users during UAT |
| Large Inventory and Sales screens become slow | Medium | Reduced daily productivity | Add paging, debounced search, lazy thumbnails, and loading indicators |
| Catalog image previews leak object URLs or load excessive images | Low | Browser memory/performance issue | Revoke object URLs; retain lazy thumbnails; avoid Base64 in HTML |
| Public website template changes become mixed with ERP work | Medium | Uncontrolled scope | Keep `VPureLux.Web.Public` outside this refactor unless separately approved |

## Recommended Execution Order

1. Establish presentation regression tests and permission matrix.
2. Complete Phase 1 localization hardening.
3. Complete Phase 2 navigation and route hardening.
4. Refactor one low-risk module, preferably Customer, as the reference ABP UI
   pattern.
5. Apply the reference pattern to Catalog and Pricing.
6. Refactor BOM, Inventory, and Sales workflows with dedicated selectors and
   JavaScript.
7. Refactor Audit presentation.
8. Run full UAT and accessibility/responsive review.

## Approval Gate

No implementation should begin until the following are approved:

- Target navigation hierarchy.
- Vietnamese operational terminology.
- Modal versus full-page decisions.
- Searchable selector query strategy.
- Permission visibility matrix.
- Route preservation policy.

