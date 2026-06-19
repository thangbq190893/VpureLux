# AI Agent Workflow (Cursor / Codex)

> How to use AI agents on VPureLux so changes stay aligned with the architecture and conventions. Read this together with `docs/ARCHITECTURE_GUIDE.md`, `docs/ABP_CODING_CONVENTIONS.md`, `docs/UI_RAZOR_PAGES_GUIDE.md`, and `docs/DEFINITION_OF_DONE.md`.

## General loop (every task)

1. **Read backend first.** Before writing anything, read the relevant Domain entity/manager, `Application.Contracts` interface + DTOs, the `Application` service, and existing tests. The source is the source of truth, not the root spec docs.
2. **Reuse existing contracts.** Use the existing `I*AppService` methods and DTOs. Do **not** invent new APIs, DTO shapes, permissions, or error codes unless the task explicitly requires them — and if it does, define them in the correct layer (`Application.Contracts`, `VPureLuxPermissions`, `VPureLuxDomainErrorCodes`).
3. **Stay in scope.** Touch one module/concern per batch. Do not refactor unrelated modules.
4. **Validate.** Run `dotnet build VPureLux.slnx --no-restore` and `dotnet test VPureLux.slnx --no-build` (scoped with `--filter` while iterating). Use `rg` to confirm patterns (see below).
5. **Report.** List files changed, modules touched, validation results, and any `Needs verification` items.

## Which tasks suit Codex (backend / scripted / scoped)

- Backend logic in Domain/Application/EF Core: invariants, managers, repository queries, application services.
- Writing or extending **tests** (Domain, Application, EF Core, Web Api/Pages).
- Scoped, checklist-driven refactors with clear acceptance criteria (e.g. "ensure every method on `XAppService` has `[Authorize]` and a localized permission").
- Localization key reconciliation, error-code/message consistency passes.

## Which tasks suit Cursor (frontend / multi-file UI)

- Razor Pages UI work: `.cshtml`, PageModels, page `.js`, modals, tag-helper layouts.
- Multi-file UI refactors that follow `docs/UI_RAZOR_PAGES_GUIDE.md` (removing anti-patterns across a module).
- Interactive/manual UAT of operator workflows in the running app.

## Guardrails (do / don't)

- **Do** read the existing module end-to-end before editing; mirror the Customers module as the reference pattern.
- **Do** keep business logic in Domain/Application and presentation in Web.
- **Do** preserve permissions, localization, and capability-flag visibility in the UI.
- **Do** run `build` + `test` + `rg` and report results.
- **Don't** introduce AutoMapper (this project uses Mapperly), call `DbContext`/repositories from the UI, show raw GUIDs, or add inline `<script>`.
- **Don't** create EF Core migrations or change the schema unless the task explicitly asks (and then keep it a separate, reviewed batch).
- **Don't** trust legacy root `*.md` specs over the code; reconcile, mark `Needs verification`, and prefer source.

## Useful `rg` inspection commands

- Application services: `rg -l "ApplicationService" src/VPureLux.Application`
- Service contracts: `rg -l "interface I.*AppService" src/VPureLux.Application.Contracts`
- Razor Pages: list `src/VPureLux.Web/Pages/**/*.cshtml`
- DbContext / EF config: `src/VPureLux.EntityFrameworkCore/EntityFrameworkCore/VPureLuxDbContext.cs` and `rg -l "ApplyConfiguration" src/VPureLux.EntityFrameworkCore`
- Permissions: `src/VPureLux.Application.Contracts/Permissions/VPureLuxPermissions.cs`
- Error codes: `src/VPureLux.Domain.Shared/VPureLuxDomainErrorCodes.cs`
- Tests for a module: `rg -l "<Module>" test`
- UI anti-patterns: `rg -n "href=\"/" src/VPureLux.Web/Pages`, `rg -n "<script" src/VPureLux.Web/Pages`, `rg -n "IRepository|DbContext" src/VPureLux.Web/Pages`

## Reporting template (end of every task)

- **Docs/files created or updated:** …
- **Modules touched:** …
- **Build result:** `dotnet build VPureLux.slnx --no-restore` → …
- **Test result:** `dotnet test VPureLux.slnx --no-build` → …
- **rg checks run:** …
- **Uncertainty / Needs verification:** …
