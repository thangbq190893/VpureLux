# VPureLux UI Refactor - Codex README

## Purpose

This document is the first file Codex must read before doing any VPureLux ERP UI refactor work.

The current goal is to make the certified ERP usable for operators without changing certified backend behavior.

The UI refactor is **not** a business-module implementation sprint. It is a presentation-layer cleanup and usability sprint.

## Required Read Order

Codex must read files in this order:

1. `CODEX_UI_REFACTOR_README.md`
2. `UI_REFACTOR_SOURCE_OF_TRUTH.md`
3. `UI_IMPLEMENTATION_DECISION_MATRIX.md`
4. `UI_ABP_IMPLEMENTATION_RULES.md`
5. `UI_BACKEND_GAP_REGISTER.md`
6. `UI_REFACTOR_ROADMAP_AND_VALIDATION.md`
7. `UI_UAT_FLOW_TEST_PLAN.md`
8. Existing certified module specifications, when present:
   - `CUSTOMER_MODULE_IMPLEMENTATION_SPECIFICATION.md`
   - `INVENTORY_MODULE_IMPLEMENTATION_SPECIFICATION.md`
   - `PRICING_MODULE_IMPLEMENTATION_SPECIFICATION.md`
   - `SALES_MODULE_IMPLEMENTATION_SPECIFICATION.md`
   - Catalog image certification/specification documents
   - Audit certification/specification documents
9. Target source files for the requested step.

If certified module specifications conflict with these UI documents on business behavior, the certified module specification wins. If these UI documents conflict with old UI refactor documents, these UI documents win.

## Non-Negotiable Rule

Do **not** implement fixes from memory or assumptions. Before changing a page, inspect:

- The Razor Page.
- The PageModel.
- The application service interface used by the PageModel.
- Existing DTOs and list methods.
- Existing authorization policy.
- Existing tests for the module.

Then classify the change as one of:

- UI-only fix.
- Existing application-service usage.
- Requires approved selector/query contract.
- Requires backend/business change.
- Documentation/test-only gap.

Only implement UI-only fixes or existing application-service usage unless the prompt explicitly approves backend work.

## Layer Boundaries

The UI layer may:

- Render Razor Pages.
- Use existing PageModel handlers.
- Use existing application services already available to the Web layer.
- Register page-specific JavaScript through `<abp-script>`.
- Use ABP UI APIs such as `abp.ModalManager`, `abp.message.confirm`, `abp.notify`, and `abp.ui.setBusy`.
- Hide technical fields from operators while preserving model binding.

The UI layer must not:

- Access `DbContext`.
- Access repositories directly.
- Call domain managers or domain services directly.
- Create new AppService methods without explicit approval.
- Change DTO/API contracts without explicit approval.
- Change Domain/Application/Infrastructure/EF Core/migrations.
- Add or weaken business validation.
- Implement business rules in JavaScript, Razor, or PageModel.

## Primary UI Priority

The implementation priority is:

1. Operator usability blockers.
2. Permission correctness and data-safety.
3. Route preservation and fallback safety.
4. ABP pattern compliance.
5. Visual polish.

Raw GUID inputs are usability blockers. Fix them before polishing action menus or modal behavior in unrelated modules.

## Current Known Validated Baseline

At the time this document was created, the following checkpoint was validated by the user:

- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build`: passed.
- `dotnet test VPureLux.slnx --no-build --no-restore`: passed.
- EF pending model check: no pending changes.
- Global search found no `<abp-button href>` and no `href="/"` in ERP Razor Pages.

This baseline may become stale. Always run the requested validation for the current step.

## What Codex Must Report After Each Step

Every implementation step must report:

1. Summary.
2. Files changed.
3. UI behavior changed.
4. Backend/business behavior changed: yes/no.
5. Permission behavior changed: yes/no.
6. Route behavior changed: yes/no.
7. Tests/build run.
8. Tests skipped and why.
9. Search checks.
10. Remaining risks.
11. Recommended next single step.

Do not generate a long chain of prompts. Do not continue to the next implementation step unless asked.
