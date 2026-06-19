# UI / Razor Pages Guide

> Project-specific Razor Pages patterns, derived from existing pages (the Customers module is the cleanest reference: `Pages/Customers/Index.cshtml`, `Index.cshtml.cs`, `Index.js`). Follow these to avoid ABP UI anti-patterns. See also root `UI_ABP_IMPLEMENTATION_RULES.md` and `UI_UX_ABP_GUIDE_V2.md`.

## PageModel rules

- **Inject application services only** (the `I*AppService` interfaces) plus framework services like `IAuthorizationService`. The PageModel base is `VPureLuxPageModel`.
- **No `DbContext`, `IRepository`, or domain services (`*Manager`) in the UI.** PageModels must not touch persistence or domain internals.
- **No business logic in the PageModel** beyond mapping request → app-service call → view data. Status transitions, calculations, and validation belong to the backend.
- Compute **capability flags** for visibility via `IAuthorizationService` (e.g. `CanCreate`, `CanEdit`, `CanManageStatus`) and bind list filters with `[BindProperty(SupportsGet = true)]`.
- Use `[TempData]` + a localization **key** (not a literal) for post-redirect status messages, and `RedirectToPage()` after POST handlers.

## Razor view (`.cshtml`) rules

- **No business logic in Razor or JavaScript.** Render data the service returned; do not recompute totals, prices, or stock.
- **Selectors and tables show code/name labels, never raw GUIDs.** Read DTOs expose reference names (e.g. `CustomerGroupName`); display those. For `<select>`, build `SelectListItem`s as `"{Code} - {Name}"` with the id as the value (see `Customers/CreateModal` PageModel).
- **Load page JavaScript with `<abp-script src="/Pages/.../X.js" />`** inside `@section scripts`. **No inline `<script>` blocks** in pages (the codebase has none — keep it that way).
- **Do not use `<abp-button href>`** for navigation. Use anchors/buttons wired appropriately.
- **No hardcoded internal `href="/..."`.** Use `asp-page="/Module/Page"` (+ `asp-route-id`) and `asp-page-handler` for form posts so routing stays correct.
- Use ABP **Tag Helpers** (`abp-card`, `abp-table`, `abp-row`, `abp-column`, `abp-input`, `abp-modal`) and localize every string via `IStringLocalizer<VPureLuxResource>` (`L["Module:Key"]`).
- Permission-gate UI affordances with the PageModel capability flags (`@if (Model.CanEdit) { ... }`).

## JavaScript rules (page `.js` files)

- Keep page JS in a dedicated file referenced by `<abp-script>`; wrap it in an IIFE.
- Use the ABP JS API: `abp.localization.getResource('VPureLux')` for text, `abp.notify.*` for toasts, `abp.message.confirm` for confirmations, `abp.ui.setBusy` for busy state.
- Wire behavior through **`data-*` attributes** rendered by the view (e.g. `data-customer-edit data-id="..."`); do not inline handlers in markup.
- **Do not call internal endpoints by hardcoded URL or implement business logic in JS.** Use `abp.appPath + 'Module/Modal'` for modal view URLs.

## Modal vs full-page workflow

- **Use `abp.ModalManager`** for quick, single-aggregate create/edit/details (the Customers module uses `CreateModal`/`EditModal`/`DetailsModal` with `ModalManager` and `onResult` → refresh). Provide both modal and full-page variants where the existing module does.
- **Use a full-page workflow** for complex, multi-step, or multi-aggregate operations (e.g. Sales order create/edit, Inventory receipt/issue/adjustment, BOM editing). These already exist as full pages — match that choice rather than forcing a modal.

## Quick anti-pattern checklist (must all be false)

- Repository/`DbContext`/domain service used in a PageModel or view.
- Business calculation or status logic in Razor/JS.
- Raw GUID shown to the operator.
- Inline `<script>` in a page, or JS not loaded via `<abp-script>`.
- `<abp-button href>` used for navigation.
- Hardcoded internal `href="/..."` instead of `asp-page`/`asp-page-handler`.
- Hardcoded display text instead of `IStringLocalizer<VPureLuxResource>`.
- UI affordance shown without the matching permission check.
