# Implementation Roadmap

> Phased, batch-sized plan for future Cursor/Codex tasks. The codebase already has all seven custom modules with passing tests, so this roadmap prioritizes **backend correctness/verification first, then UI/operator UAT** — not greenfield feature building. Keep batches small (one module/concern at a time) and always finish with the validation commands and a report.

**Baseline (verify at the start of any batch):**
- `dotnet build VPureLux.slnx --no-restore`
- `dotnet test VPureLux.slnx --no-build`
- Containers running per `AGENTS.md` only if you need to run the apps (tests use SQLite in-memory).

Each phase below lists: **Goal · Scope · Likely files/modules · Validation · Risks · Done criteria.**

---

## Phase 0 — Documentation & baseline trust (this task)
- **Goal:** establish the `docs/` canonical set and confirm a green baseline.
- **Scope:** docs only; no behavior change.
- **Files/modules:** `docs/*`.
- **Validation:** `dotnet build VPureLux.slnx --no-restore`; `dotnet test VPureLux.slnx --no-build`.
- **Risks:** docs drifting from source.
- **Done:** all 8 docs present, build + tests green, report written.

## Phase 1 — Backend correctness audit (per module, one batch each)
- **Goal:** confirm each module's invariants, permissions, and error codes are enforced and covered by tests.
- **Scope (one module per batch, order: Catalog → Bom → Customers → Pricing → Inventory → Sales → Audit):** read Domain + Application + EF Core + tests; close any obvious gaps (missing `[Authorize]`, missing `BusinessException` code, untested invariant) **without changing intended behavior**.
- **Likely files/modules:** `src/VPureLux.Domain/<Module>`, `src/VPureLux.Application/<Module>`, `src/VPureLux.EntityFrameworkCore/<Module>`, `test/*/<Module>`.
- **Validation:** module-scoped `dotnet test VPureLux.slnx --no-build --filter "FullyQualifiedName~<Module>"`; full suite before commit.
- **Risks:** "fixing" something that's intentional; touching unrelated modules.
- **Done:** every permission on the service is defined + provided + localized; every thrown error code exists in `VPureLuxDomainErrorCodes` with a localized message; invariants have at least one domain/integration test; full suite green.

## Phase 2 — Localization completeness (vi-VN)
- **Goal:** ensure every UI/exception key resolves in `vi-VN.json`.
- **Scope:** find `L["..."]` keys and error-code message keys not present in `vi-VN.json`; add translations. No logic change.
- **Likely files:** `src/VPureLux.Domain.Shared/Localization/VPureLux/vi-VN.json`, `Pages/**/*.cshtml`, `VPureLuxDomainErrorCodes.cs`.
- **Validation:** `rg "L\[\"" src/VPureLux.Web/Pages` cross-checked against the JSON; build; run the Web app and spot-check pages.
- **Risks:** missing keys only surface at runtime; verify by loading pages.
- **Done:** no missing-key fallbacks on the audited pages; build green.

## Phase 3 — UI anti-pattern cleanup (per module, one batch each)
- **Goal:** bring each module's Razor Pages in line with `docs/UI_RAZOR_PAGES_GUIDE.md`.
- **Scope:** audit one module's pages for the anti-pattern checklist (no repo/DbContext in UI, no raw GUIDs, `<abp-script>` only, no inline scripts, no `<abp-button href>`, no hardcoded internal `href`, localized text, permission-gated affordances). Fix violations; do not change backend contracts.
- **Likely files:** `src/VPureLux.Web/Pages/<Module>/*` (`.cshtml`, `.cshtml.cs`, `.js`).
- **Validation:** `rg -n "href=\"/" src/VPureLux.Web/Pages/<Module>`; `rg -n "<script" src/VPureLux.Web/Pages/<Module>`; `rg -n "IRepository|DbContext|Manager" src/VPureLux.Web/Pages/<Module>`; `dotnet test VPureLux.slnx --no-build --filter "FullyQualifiedName~<Module>"`; manual page load.
- **Risks:** UI refactors regress permissions/visibility — keep capability flags intact.
- **Done:** anti-pattern checklist all-clear for the module; Web.Tests for the module green; pages load and operate.

## Phase 4 — Operator UAT pass (per workflow)
- **Goal:** validate end-to-end operator flows against the existing UAT material.
- **Scope:** run real flows (e.g. create customer group → customer → catalog → BOM publish → pricing → inventory receipt → sales order confirm) per `UI_UAT_FLOW_TEST_PLAN.md` / `DATA_FLOW_UAT_SCENARIOS_V2.md`; log defects.
- **Likely files:** none changed in the UAT batch itself; defects feed back into Phase 1/3 batches.
- **Validation:** run `VPureLux.Web` (admin `admin` / `1q2w3E*`), follow the flow, capture screenshots/video.
- **Risks:** UAT depends on seed data and running Redis + SQL Server (see `AGENTS.md`).
- **Done:** each targeted workflow completes without error, or defects are filed with reproduction steps.

## Phase 5 — Reconcile/retire legacy root docs (optional, low risk)
- **Goal:** reduce confusion between the new `docs/` set and the many root `*.md` specs.
- **Scope:** mark superseded root docs as historical (a header note) and link to the `docs/` canonical files. **Do not delete** without explicit approval.
- **Validation:** markdown link check; `dotnet build` (no code impact).
- **Risks:** removing still-relevant context.
- **Done:** root docs clearly labeled; `docs/` referenced as canonical.

---

## Recommended next batch
**Phase 1 — Catalog backend correctness audit.** It is the foundational module (Components/Products feed BOM, Pricing, Inventory, Sales), already has the richest test set, and verifying it first de-risks downstream modules. Keep the change surface inside `*/Catalog/*` and finish with the full test suite + report.
