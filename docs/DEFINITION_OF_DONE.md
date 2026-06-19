# Definition of Done

> A change to VPureLux is "done" only when **all** applicable criteria below are met. Use this as the final gate for every Cursor/Codex batch.

## Build & tests
- [ ] `dotnet build VPureLux.slnx --no-restore` succeeds (no new errors).
- [ ] `dotnet test VPureLux.slnx --no-build` passes (full suite; no new failures or skips).
- [ ] New/changed behavior is covered by tests at the appropriate layer (Domain for invariants, EF Core for app-service/repository integration, Web for Pages/Api), following existing test conventions.

## Architecture & conventions
- [ ] Layering respected: business rules in Domain/Application, persistence in EF Core, presentation only in Web (see `docs/ARCHITECTURE_GUIDE.md`).
- [ ] No outward dependencies introduced (Domain stays free of Application/EF references).
- [ ] Backend conventions followed: thin authorized application services, separate create/update DTOs, custom repositories for tailored queries, Mapperly for mapping (no AutoMapper), `async`/`await` with `AsyncExecuter` (see `docs/ABP_CODING_CONVENTIONS.md`).

## UI quality (if UI touched)
- [ ] No ABP UI anti-patterns: no `DbContext`/repository/domain service in PageModels or views; no business logic in Razor/JS; `<abp-script>` used (no inline `<script>`); no `<abp-button href>`; no hardcoded internal `href="/..."` (use `asp-page`/`asp-page-handler`).
- [ ] **No raw GUIDs shown to operators** — code/name labels only (e.g. `CustomerGroupName`).
- [ ] Modal vs full-page choice matches the workflow complexity (see `docs/UI_RAZOR_PAGES_GUIDE.md`).

## Permissions
- [ ] Permissions preserved/enforced: every application-service method has the correct `[Authorize(...)]`; new permissions are declared in `VPureLuxPermissions`, registered in `VPureLuxPermissionDefinitionProvider`, and localized.
- [ ] UI visibility uses capability flags from `IAuthorizationService`; the UI check is never the only security boundary.

## Localization
- [ ] All new user-facing text and exception messages have `vi-VN` entries in `Localization/VPureLux/vi-VN.json`; no hardcoded display strings.

## Error handling
- [ ] Rule violations throw `BusinessException(VPureLuxDomainErrorCodes.<Code>)`; any new code is defined in `VPureLuxDomainErrorCodes` with a localized message.

## Scope hygiene
- [ ] No unintended module touches — the diff stays within the batch's module/concern.
- [ ] No EF Core migration or schema change unless explicitly requested and reviewed as its own batch.
- [ ] No vendor/framework generated code modified. Application source files may be modified only when explicitly in the approved batch scope.

## Final report (required)
- [ ] Report includes: docs/files created or updated, modules touched, build result, test result, `rg` checks run, and any `Needs verification` items.
