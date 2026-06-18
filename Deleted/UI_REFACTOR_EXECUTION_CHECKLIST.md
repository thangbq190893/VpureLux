# UI Refactor Execution Checklist

Source of truth: `UI_ARCHITECTURE_REFACTOR_PLAN.md`.

This checklist is for execution control only. It must not be used to change
certified backend behavior. Every batch must preserve existing routes first and
must avoid domain, application, repository, EF Core, migration, database schema,
and business-validation changes.

## Global Guardrails

- Do not change certified backend logic.
- Do not create migrations.
- Do not add new business features.
- Do not modify `VPureLux.Web.Public` unless explicitly needed to isolate ERP UI
  from public template content.
- Keep Razor Pages and LeptonX.
- Prefer ABP Razor conventions: `asp-page`, `asp-route-*`, `<abp-script>`,
  `abp.ModalManager`, `abp.message.confirm`, `abp.notify`, and busy states.
- New API/application contracts for selectors are out of scope unless marked
  "requires explicit approval" and left unimplemented.

## Validation Searches Recorded

Run before and after affected batches:

```powershell
rg -n '<abp-button[^>]*href=' src/VPureLux.Web/Pages -g '*.cshtml'
rg -n 'href="/' src/VPureLux.Web/Pages -g '*.cshtml'
rg --pcre2 -n '<script(?![^>]*src=)' src/VPureLux.Web/Pages -g '*.cshtml'
rg -n '<script[^>]*src=' src/VPureLux.Web/Pages -g '*.cshtml'
rg -n '@[A-Za-z0-9_.]+\.Status|@\(.*\.Status|@[^\\n]*\.Type|@[^\\n]*LineType' src/VPureLux.Web/Pages -g '*.cshtml'
rg -n 'Cost|Profit|TotalProfit|IssueCost|SuggestedPrice|ActualSellingPrice|Revenue|Margin' src/VPureLux.Web/Pages/Sales src/VPureLux.Web/Pages/Inventory -g '*.cshtml' -g '*.cs'
rg -n 'Pricing:OpenHistory|/Pricing/Components/|/Pricing/Products/' src/VPureLux.Web/Pages/Pricing -g '*.cshtml'
```

Current recorded findings:

- `<abp-button>` with `href`: 3 occurrences.
- Hardcoded internal `href="/..."`: 61 occurrences.
- Inline script blocks in ERP Razor pages: 2 occurrences.
- Raw page-level `<script src>` in ERP Razor pages: 4 occurrences.
- No current use of `abp.ajax`, `abp.ModalManager`, `abp.message.confirm`, or
  `abp.notify`.
- Direct enum/status rendering exists in Catalog, Customer, Customer Group,
  Inventory, and Sales pages.
- Sales profit values are rendered in Sales Details, History, and Customer
  History; cost-like inventory values are rendered in Inventory inquiries.
- Pricing index renders history links while the destination pages require
  `Pricing.History`.

## Batch 1: Presentation Regression Tests and Permission Matrix

Scope:

- Add UI-only regression coverage for current pages, visible actions, route
  preservation, localization keys, and permission-aware rendering.
- Finalize the permission visibility matrix before any UI conversion.

Likely files touched:

- `test/VPureLux.Web.Tests/Pages/*`
- `test/VPureLux.Web.Tests/Api/*` only if existing UI/API route behavior needs
  regression coverage.
- Documentation matrices at repository root.

Forbidden changes:

- No production UI refactor yet.
- No backend logic changes.
- No new business endpoints.

Validation commands:

```powershell
dotnet build VPureLux.slnx --no-restore
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build
dotnet test VPureLux.slnx --no-build --no-restore
```

Exit criteria:

- Route preservation tests exist for current ERP pages.
- Permission visibility assertions exist for sensitive buttons and fields.
- Full regression remains green.

## Batch 2: Localization Hardening

Scope:

- Convert operator-facing ERP text to Vietnamese.
- Add missing local keys: `Create`, `Delete`, `Login`, `Refresh`.
- Replace hardcoded English in ERP module pages.
- Localize enum/status labels and common action text.

Likely files touched:

- `src/VPureLux.Domain.Shared/Localization/VPureLux/vi-VN.json`
- ERP Razor pages under `src/VPureLux.Web/Pages`
- Optional localization test files.

Forbidden changes:

- Do not rename permissions, class names, route names, or business terms in
  code.
- Do not translate technical identifiers used for APIs or permission names.

Validation commands:

```powershell
dotnet build VPureLux.slnx --no-restore
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build
```

Exit criteria:

- No missing local Razor localization keys.
- No hardcoded English in ERP operator pages except approved technical names.
- Status text uses localized labels.

## Batch 3: Navigation and Route Hardening

Scope:

- Preserve all existing routes.
- Replace internal hardcoded hrefs with `asp-page` and `asp-route-*`.
- Fix all `<abp-button href="...">` misuse.
- Restructure menu hierarchy while keeping destination routes stable.

Likely files touched:

- `src/VPureLux.Web/Menus/VPureLuxMenuContributor.cs`
- `src/VPureLux.Web/Menus/VPureLuxMenus.cs`
- Razor pages with hardcoded internal hrefs.
- Route preservation tests.

Forbidden changes:

- Do not move route URLs without an approved redirect.
- Do not remove existing pages.
- Do not change backend operations.

Validation commands:

```powershell
rg -n '<abp-button[^>]*href=' src/VPureLux.Web/Pages -g '*.cshtml'
rg -n 'href="/' src/VPureLux.Web/Pages -g '*.cshtml'
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build
```

Exit criteria:

- Zero `<abp-button>` elements with `href`.
- Existing route tests pass.
- Menu visibility matches destination permissions.

Step 2.2 completion notes:

- Reviewed `VPureLuxMenuContributor` and `VPureLuxMenus`.
- Confirmed all current ERP menu URLs point to preserved Razor routes.
- Confirmed ERP menu labels use localization keys.
- Confirmed menu permissions match destination page permissions for current top-level and Catalog child menu items.
- No menu hierarchy restructuring was performed.
- No production menu code change was required.

## Batch 4: Customer Module as Reference ABP UI Pattern

Scope:

- Use Customer and Customer Group as the first reference implementation for ABP
  modals, action menus, confirmations, notifications, and page-specific JS.
- Preserve `/Customers` and `/CustomerGroups` routes.

Likely files touched:

- `src/VPureLux.Web/Pages/Customers/*`
- `src/VPureLux.Web/Pages/CustomerGroups/*`
- New UI-only JS files under `src/VPureLux.Web/Pages/Customer...`
- Web tests for customer pages.

Forbidden changes:

- No customer domain/application contract changes.
- No CustomerGroup rules or seed changes.

Validation commands:

```powershell
dotnet build VPureLux.slnx --no-restore
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Customer"
dotnet test VPureLux.slnx --no-build --no-restore
```

Exit criteria:

- Customer UI becomes the approved reference pattern.
- Create/edit/details/status actions are permission-aware and confirmed where
  needed.
- Operators no longer see broken or misleading actions.

Step 3.1 audit notes:

Customer pages reviewed:

| Page | Route | PageModel | Authorization | Handlers | App service calls | Binding / validation | UI actions |
|---|---|---|---|---|---|---|---|
| Customers Index | `/Customers` | `VPureLux.Web.Pages.Customers.IndexModel` | `Customers.View`; action flags for `Customers.Create`, `Customers.Edit`, `Customers.ManageStatus` | `OnGetAsync`, `OnPostActivateAsync`, `OnPostDeactivateAsync` | `ICustomerAppService.GetListAsync`, `ActivateAsync`, `DeactivateAsync` | `SearchText` and `Status` support GET; status enum filter uses localized display in table | Create, Details, Edit, Activate/Deactivate |
| Customers Create | `/Customers/Create` | `VPureLux.Web.Pages.Customers.CreateModel` | `Customers.Create` | `OnGetAsync`, `OnPostAsync` | `ICustomerAppService.CreateAsync`; `ICustomerGroupAppService.GetListAsync` for active groups | `CreateCustomerDto Input` bind property; app service validation remains source of truth | Save, Cancel |
| Customers Edit | `/Customers/Edit/{id}` | `VPureLux.Web.Pages.Customers.EditModel` | `Customers.Edit` | `OnGetAsync`, `OnPostAsync` | `ICustomerAppService.GetAsync`, `UpdateAsync`; `ICustomerGroupAppService.GetListAsync` | `Id` supports GET; `UpdateCustomerDto Input` bind property; code rendered disabled to preserve immutability | Save, Cancel |
| Customers Details | `/Customers/Details/{id}` | `VPureLux.Web.Pages.Customers.DetailsModel` | `Customers.View` | `OnGetAsync` | `ICustomerAppService.GetAsync` | `Id` supports GET; read-only DTO display | Back |

CustomerGroup pages reviewed:

| Page | Route | PageModel | Authorization | Handlers | App service calls | Binding / validation | UI actions |
|---|---|---|---|---|---|---|---|
| CustomerGroups Index | `/CustomerGroups` | `VPureLux.Web.Pages.CustomerGroups.IndexModel` | `CustomerGroups.View`; action flags for `CustomerGroups.Create`, `CustomerGroups.Edit`, `CustomerGroups.ManageStatus` | `OnGetAsync`, `OnPostActivateAsync`, `OnPostDeactivateAsync` | `ICustomerGroupAppService.GetListAsync`, `ActivateAsync`, `DeactivateAsync` | `SearchText` and `Status` support GET; status enum filter uses localized display in table | Create, Details, Edit, Activate/Deactivate |
| CustomerGroups Create | `/CustomerGroups/Create` | `VPureLux.Web.Pages.CustomerGroups.CreateModel` | `CustomerGroups.Create` | `OnGet`, `OnPostAsync` | `ICustomerGroupAppService.CreateAsync` | `CreateCustomerGroupDto Input` bind property; app service validation remains source of truth | Save, Cancel |
| CustomerGroups Edit | `/CustomerGroups/Edit/{id}` | `VPureLux.Web.Pages.CustomerGroups.EditModel` | `CustomerGroups.Edit` | `OnGetAsync`, `OnPostAsync` | `ICustomerGroupAppService.GetAsync`, `UpdateAsync` | `Id` supports GET; `UpdateCustomerGroupDto Input` bind property; code rendered disabled to preserve immutability | Save, Cancel |
| CustomerGroups Details | `/CustomerGroups/Details/{id}` | `VPureLux.Web.Pages.CustomerGroups.DetailsModel` | `CustomerGroups.View` | `OnGetAsync` | `ICustomerGroupAppService.GetAsync` | `Id` supports GET; read-only DTO display | Back |

Safe modal candidates for a later implementation step:

- Customer Create, Edit, and Details.
- CustomerGroup Create, Edit, and Details.
- Customer and CustomerGroup Activate/Deactivate confirmation dialogs.

Risks to preserve before implementation:

- Keep `/Customers`, `/Customers/Create`, `/Customers/Edit/{id}`, `/Customers/Details/{id}`, `/CustomerGroups`, `/CustomerGroups/Create`, `/CustomerGroups/Edit/{id}`, and `/CustomerGroups/Details/{id}` available.
- Do not change `Input` model binding, `Id` binding, handler names, or app service calls during modal conversion.
- Preserve redirect behavior or intentionally replace it with ABP modal result handling only after approval.
- Re-load CustomerGroup selection data when Customer create/edit validation fails.
- Keep disabled Code fields read-only in Edit pages; do not post mutable code values.
- Add confirmation and notifications to status actions without changing `ActivateAsync` / `DeactivateAsync` semantics.
- Keep localization keys already in `vi-VN.json`; add modal-specific confirmation/success keys only when implementation starts.

Current test coverage:

- `CustomerPagesTests` covers list/create/edit/details rendering and current routes.
- `CustomerPageModelPermissionTests` covers denied-permission action flags for Customer and CustomerGroup index pages.
- Gaps for later implementation: modal open/result behavior, validation re-render behavior, status confirmation behavior, and action-menu permission rendering after markup changes.

Step 3.2 completion notes:

- Added regression coverage for preserved Customer and CustomerGroup routes:
  `/Customers`, `/Customers/Create`, `/Customers/Edit/{id}`, `/Customers/Details/{id}`,
  `/CustomerGroups`, `/CustomerGroups/Create`, `/CustomerGroups/Edit/{id}`, and
  `/CustomerGroups/Details/{id}`.
- Added localized-label assertions for Customer and CustomerGroup list, create,
  and detail pages.
- Added regression coverage that Customer and CustomerGroup edit pages render
  immutable Code values as disabled read-only display fields.
- Added exact PageModel authorization policy assertions for Customer and
  CustomerGroup list/create/edit/details pages.
- Added positive action-flag tests for `Create`, `Edit`, and `ManageStatus`
  permissions, complementing the existing denied-permission tests.
- No production UI, handler, model binding, route, JavaScript, backend, or
  workflow behavior was changed.

Remaining test gaps before modal implementation:

- Modal open/result behavior cannot be tested until modal pages and scripts are
  approved.
- Validation re-render behavior should be covered when modal or postback
  validation handling is changed; Customer create/edit must continue reloading
  CustomerGroup dropdown data after validation failures.
- Status confirmation behavior should be tested when `abp.message.confirm` is
  introduced.
- Action-menu rendering should be tested if row buttons are replaced with an
  ABP action-menu pattern.

Step 3.3 completion notes:

- Implemented the first CustomerGroups reference UI slice without converting
  Create/Edit/Details to modals.
- Added page-specific `Pages/CustomerGroups/Index.js` and registered it through
  lowercase `scripts` with `<abp-script>`.
- Added `abp.message.confirm` before CustomerGroup Activate/Deactivate form
  submission.
- Preserved existing `OnPostActivateAsync` and `OnPostDeactivateAsync` handlers,
  form post semantics, route templates, and `CustomerGroups.ManageStatus`
  permission checks.
- Added TempData-backed success notification keys so `abp.notify.success` runs
  only after the existing post handler completes and redirects.
- Kept no-JS fallback acceptable: without JavaScript, the existing post buttons
  still submit to the same handlers.
- Added Web UI regression coverage for script registration, confirmation hooks,
  localized confirmation messages, and exact `ManageStatus` control of status
  action visibility.

Remaining CustomerGroups reference gaps:

- Create/Edit/Details remain full pages and are not modalized yet.
- Status actions use confirmation and notification, but still render as inline
  row forms rather than an ABP action menu.
- No browser-level JavaScript execution test exists; current coverage verifies
  rendered hooks and PageModel permissions.

Step 3.4 completion notes:

- Applied the accepted CustomerGroups status confirmation/notification pattern
  to Customers Index.
- Added page-specific `Pages/Customers/Index.js` and registered it through
  lowercase `scripts` with `<abp-script>`.
- Added `abp.message.confirm` before Customer Activate/Deactivate form
  submission.
- Preserved existing `OnPostActivateAsync` and `OnPostDeactivateAsync` handlers,
  form post semantics, route templates, and `Customers.ManageStatus` permission
  checks.
- Added TempData-backed success notification keys so `abp.notify.success` runs
  only after the existing post handler completes and redirects.
- Kept no-JS fallback acceptable: without JavaScript, the existing post buttons
  still submit to the same handlers.
- Added Web UI regression coverage for script registration, confirmation hooks,
  localized confirmation messages, and exact `ManageStatus` control of Customer
  status action visibility.

Remaining Customer reference gaps:

- Create/Edit/Details remain full pages and are not modalized yet.
- Status actions use confirmation and notification, but still render as inline
  row forms rather than an ABP action menu.
- No browser-level JavaScript execution test exists; current coverage verifies
  rendered hooks and PageModel permissions.

Customer reference UI completion notes:

- Converted Customers and CustomerGroups Index row actions to dropdown action
  menus while preserving full-page Details/Edit anchors and status post forms.
- Added ABP ModalManager-based create, edit, and read-only detail modal pages
  for Customers and CustomerGroups.
- Preserved existing full-page routes:
  `/Customers`, `/Customers/Create`, `/Customers/Edit/{id}`,
  `/Customers/Details/{id}`, `/CustomerGroups`, `/CustomerGroups/Create`,
  `/CustomerGroups/Edit/{id}`, and `/CustomerGroups/Details/{id}`.
- Preserved existing full-page PageModel handlers, bound `Input` properties,
  `Id` route binding, and application service calls.
- Customer create/edit modal PageModels reload CustomerGroup dropdown data when
  validation fails.
- Customer and CustomerGroup edit modal pages keep Code rendered as disabled
  read-only display.
- Existing page-specific scripts now own ModalManager open/result behavior,
  confirmation dialogs, success notifications, and list reload after successful
  modal save.
- Lightweight Web tests cover Customer/CustomerGroup route rendering, source
  script registration, action-menu/modal hooks, localized confirmation messages,
  and permission flags.

Remaining Customer reference risks:

- Browser-executed JavaScript behavior is not covered by Playwright or another
  browser runner; current tests verify rendered hooks and build correctness.
- Modal validation behavior is implemented using standard Razor Page
  ModelState re-rendering, but has not been exercised through a browser.
- Full solution tests were skipped in the final combined pass to save time and
  credits after build and Customer-filtered tests passed.

## Batch 5: Catalog and Pricing

Scope:

- Apply the reference pattern to Catalog and Pricing.
- Fix Catalog image script registration and preview behavior.
- Fix Pricing history visibility with `Pricing.History`.

Likely files touched:

- `src/VPureLux.Web/Pages/Catalog/*`
- `src/VPureLux.Web/Pages/Pricing/*`
- `src/VPureLux.Web/Pages/Catalog/catalog-image-preview.js`
- Web tests for Catalog Image and Pricing pages.

Forbidden changes:

- No Catalog image backend changes.
- No price-version application behavior changes.

Validation commands:

```powershell
dotnet build VPureLux.slnx --no-restore
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Catalog|FullyQualifiedName~Pricing"
dotnet test VPureLux.slnx --no-build --no-restore
```

Exit criteria:

- Catalog image UI uses ABP script registration.
- Pricing links align with `Pricing.History`.
- No Base64 appears in HTML.

## Batch 6: BOM

Scope:

- Remove inline BOM JavaScript.
- Preserve full-page BOM editor.
- Improve component selection UX where possible using existing contracts.

Likely files touched:

- `src/VPureLux.Web/Pages/Bom/*`
- New `BomEditor.js`
- Web tests for BOM pages.

Forbidden changes:

- No BOM aggregate/application changes.
- No new selector endpoint unless separately approved.

Validation commands:

```powershell
rg --pcre2 -n '<script(?![^>]*src=)' src/VPureLux.Web/Pages/Bom -g '*.cshtml'
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Bom"
dotnet test VPureLux.slnx --no-build --no-restore
```

Exit criteria:

- BOM editor JS is external and page-registered.
- Publish/archive/clone/edit UI is permission-aware and confirmed.

## Batch 7: Inventory

Scope:

- Improve Warehouse, Receipt, Issue, Adjustment, Balance, Lot, and Ledger UI.
- Introduce reusable selectors only with existing approved data.
- Add confirmations and notifications for posting and status actions.

Likely files touched:

- `src/VPureLux.Web/Pages/Inventory/*`
- Inventory page-specific JS files.
- Web tests for Inventory pages.

Forbidden changes:

- No inventory FIFO, ledger, balance, or posting behavior changes.
- No inventory schema or repository changes.

Validation commands:

```powershell
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Inventory"
dotnet test VPureLux.slnx --no-build --no-restore
```

Exit criteria:

- Posting pages are operator-friendly and route-stable.
- Sensitive posting actions are confirmed and notify completion.
- Cost-related visibility is reviewed and documented.

## Batch 8: Sales

Scope:

- Improve order creation/edit/detail/history UI.
- Ensure Sales cost/profit fields use exact permissions.
- Add Catalog image support to item selection without image snapshotting.

Likely files touched:

- `src/VPureLux.Web/Pages/Sales/*`
- Sales page-specific JS files.
- Web tests for Sales pages.

Forbidden changes:

- No Sales aggregate/application/confirmation workflow changes.
- No new product inventory, bundle, serial number, warranty, or reporting
  feature.

Validation commands:

```powershell
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Sales"
dotnet test VPureLux.slnx --no-build --no-restore
```

Exit criteria:

- `Sales.ViewCost` and `Sales.ViewProfit` are respected in UI.
- Confirm/cancel/remove-line workflows use confirmation and notifications.
- Existing routes are preserved.

## Batch 9: Audit

Scope:

- Improve Audit list/detail/report/export presentation.
- Consider read-only detail modal.
- Add export busy state and completion notification.

Likely files touched:

- `src/VPureLux.Web/Pages/Audit/*`
- Audit page-specific JS files.
- Web tests for Audit pages.

Forbidden changes:

- No BusinessAuditLog domain/application/infrastructure changes.
- No audit payload behavior changes.

Validation commands:

```powershell
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Audit"
dotnet test VPureLux.slnx --no-build --no-restore
```

Exit criteria:

- Audit detail is readable and safe.
- Export uses `Audit.Export` visibility and user feedback.

## Batch 10: UAT, Accessibility, and Responsive Review

Scope:

- Run full regression and operator walkthrough.
- Review keyboard usage, responsive layout, image loading, validation messages,
  and Vietnamese terminology.

Likely files touched:

- UAT documentation only unless approved fixes are found.
- Final UI test updates.

Forbidden changes:

- No new feature scope.
- No backend changes without a separate approval.

Validation commands:

```powershell
dotnet build VPureLux.slnx --no-restore
dotnet test VPureLux.slnx --no-build --no-restore
dotnet ef migrations has-pending-model-changes --project src/VPureLux.EntityFrameworkCore/VPureLux.EntityFrameworkCore.csproj --startup-project src/VPureLux.DbMigrator/VPureLux.DbMigrator.csproj --context VPureLuxDbContext --no-build
```

Exit criteria:

- Full regression passes.
- No pending EF model changes.
- UAT checklist is ready for operator sign-off.
