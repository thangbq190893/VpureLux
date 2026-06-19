# ABP Coding Conventions (VPureLux)

> Conventions observed in the current codebase. Follow these so new code matches existing modules (Customers, Catalog, Inventory, Sales, Pricing, Bom, Audit). Examples are described in prose, not large code blocks.

## Application services

- **Do** put each application service interface in `Application.Contracts` (`I<Name>AppService`) and the implementation in `Application` (`<Name>AppService : ApplicationService, I<Name>AppService`).
- **Do** decorate the class with `[Authorize(VPureLuxPermissions.<Module>.Default)]` and each method with the specific permission (e.g. `.View`, `.Create`, `.Edit`, `.ManageStatus`). The `CustomerAppService` is the reference pattern.
- **Do** keep application services thin: validate input via DTO data annotations, load aggregates through repositories, delegate invariants to domain `*Manager`s / entity methods, persist, then map to DTOs.
- **Don't** put invariant/business logic (uniqueness, status transitions, calculations) directly in the application service — that belongs in `Domain`.
- **Don't** return domain entities from public methods; always return DTOs.

## DTO pattern

- **Do** define DTOs in `Application.Contracts`, separated by intent: read DTOs (`<Name>Dto`), create (`Create<Name>Dto`), update (`Update<Name>Dto`), and list query inputs (`Get<Name>ListInput`, typically extending ABP paged/sorted inputs).
- **Do** annotate create/update DTOs with `System.ComponentModel.DataAnnotations` (`[Required]`, `[StringLength(<Consts>.Max...)]`, `[EmailAddress]`). Length limits reference domain `*Consts`.
- **Do** expose human-readable reference labels on read DTOs (e.g. `CustomerGroupName`) so the UI never shows raw GUIDs.
- **Don't** reuse a single combined create/update DTO when create and update inputs differ; the codebase keeps them separate.

## Repositories: `IRepository` / custom repositories

- **Do** define a custom repository interface in `Domain` (e.g. `ICustomerRepository`) when you need tailored queries (search, filter, count, code-exists), and implement it in `EntityFrameworkCore` as `EfCore<Name>Repository`.
- **Do** use `GetQueryableAsync()` + `AsyncExecuter.ToListAsync(...)` for ad-hoc queries inside services (never block on EF async).
- **Do** use `IReadOnlyRepository<T>` / read-only queries for pure reads where appropriate.
- **Don't** call `DbContext` directly from application services; go through repositories.
- **Don't** put query logic in the UI.

## async/await

- **Do** make all I/O-bound methods `async` and `await` them; suffix with `Async`.
- **Do** use ABP's `AsyncExecuter` to materialize `IQueryable` (do not call `.ToList()`/`.Count()` synchronously on EF queryables).
- **Don't** use `.Result`, `.Wait()`, or `async void`.

## Mapping (Mapperly, not AutoMapper)

- This project uses **Mapperly** (`Riok.Mapperly` via `Volo.Abp.Mapperly`). Each module has a hand-written mapper class in `Application` (e.g. `SalesApplicationMapper`, `PricingApplicationMapper`); `VPureLuxApplicationMappers.cs` is the shared placeholder.
- **Do** add entity→DTO mapping methods to the module's Mapperly mapper and inject it into the service (constructor injection), as `CustomerAppService` does with `CustomerApplicationMapper`.
- **Do** map reference names by passing the related aggregate into the mapper (the pattern used to populate `CustomerGroupName`).
- **Don't** introduce AutoMapper profiles or `ObjectMapper.Map<>` for these modules — stay consistent with Mapperly.

## Permissions

- **Do** declare every permission as a constant under `VPureLuxPermissions` (nested static classes per module) and register it in `VPureLuxPermissionDefinitionProvider` with localized display names.
- **Do** enforce on the backend with `[Authorize(...)]`; the UI authorization check is for **visibility only**, never the security boundary.
- **Don't** invent ad-hoc permission strings; reference the constants.
- **Don't** remove or rename existing permissions without updating the provider, UI checks, and localization.

## Localization

- **Do** add user-facing text to `Localization/VPureLux/vi-VN.json` (primary culture is `vi-VN`) and reference it via `IStringLocalizer<VPureLuxResource>` (`L["Customers:Title"]`).
- **Do** follow the existing key namespacing: `Module:Key` (e.g. `Customers:Create`), shared keys at top level (`Save`, `Cancel`, `Actions`), and enum/status keys like `Status:{value}`.
- **Don't** hardcode display strings in `.cshtml`, PageModels, or exception messages.

## Validation

- **Do** rely on DTO data annotations for input validation (ABP runs them automatically; PageModels check `ModelState.IsValid`).
- **Do** put business validation (e.g. price > 0, BOM must be published, sufficient inventory) in the domain layer as `BusinessException`s, or in dedicated application validators (e.g. `PricingCatalogValidator`, `BomCatalogValidator`) where cross-aggregate checks are needed.
- **Don't** duplicate domain validation in the UI; surface server errors instead.

## Business exception handling

- **Do** throw `Volo.Abp.BusinessException(VPureLuxDomainErrorCodes.<Code>)` for rule violations and attach context with `.WithData(name, value)`.
- **Do** define every error code in `VPureLuxDomainErrorCodes` (Domain.Shared) using the module-prefixed scheme (`CATALOG_00x`, `BOM_00x`, `CUSTOMER_00x`, `PRICE_00x`, `INV_0xx`, `SALES_0xx`, `AUDIT_00x`, `COM_00x`) and provide a localized message for it.
- **Do** throw a not-found `BusinessException` (e.g. `CustomerNotFound`) instead of returning null from loader helpers.
- **Don't** throw raw `Exception`/`InvalidOperationException` for domain rule violations, and don't leak internal details to users.
