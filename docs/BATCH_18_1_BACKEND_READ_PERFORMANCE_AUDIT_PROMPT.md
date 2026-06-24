# Batch 18.1 - Backend Read Performance Audit Prompt

## Purpose

Use this prompt with Codex Agent to perform a **documentation-only backend/read-performance audit** for VPureLux.  
The goal is to identify N+1 queries, inefficient read paths, repeated current-price/current-version lookups, over-fetching, and test gaps before making any backend optimization changes.

---

## Prompt

Owner: Codex Agent

Task: Batch 18.1 - Backend read-performance audit and optimization plan.

This is an audit/documentation task only.

Do NOT change production code.
Do NOT change Razor pages.
Do NOT change CSS.
Do NOT change JavaScript.
Do NOT change tests.
Do NOT change database schema.
Do NOT add migrations.
Do NOT add indexes.
Do NOT refactor.
Do NOT optimize code in this task.
Do NOT change API contracts.
Do NOT change DTO contracts.
Do NOT change business rules.
Do NOT change permissions.
Do NOT change pricing semantics.
Do NOT change inventory posting/FIFO behavior.
Do NOT change sales order lifecycle behavior.
Do NOT push unless explicitly told.

## Context

VPureLux UI polish phase is complete and tagged:

```text
ui-polish-phase-2026-06-25
```

Recent UAT runtime smoke audit found:

- No page-level runtime exceptions on current `main`.
- Catalog Components page loads successfully.
- Components without current suggested price show placeholder.
- Catalog Components list still has suspected N+1 current price lookups and took several seconds for a small list.
- Need a backend/read-performance audit before implementing fixes.

## Goal

Audit backend and Web PageModel read paths for performance risks, especially:

- N+1 database calls
- per-row AppService calls
- inefficient EF queries
- over-fetching
- repeated permission/data lookups
- repeated current-price/current-version lookups

Produce a prioritized optimization plan only. Do not fix anything yet.

## Audit scope

Inspect:

```text
src/VPureLux.Web/Pages
src/VPureLux.Application
src/VPureLux.Application.Contracts
src/VPureLux.Domain
src/VPureLux.EntityFrameworkCore
test/VPureLux.*.Tests
```

Focus modules:

1. Catalog
2. Pricing
3. BOM
4. Inventory
5. Sales
6. Customers / CustomerGroups
7. Audit

## Primary risk patterns to search

Search for:

1. `foreach` / `foreach await` calling:
   - AppService methods
   - Repository methods
   - `GetAsync`
   - `FindAsync`
   - `GetCurrentAsync`
   - `GetAtDateAsync`
   - `IsGrantedAsync`
2. Razor PageModels calling Application Services per row.
3. Application Services calling Repository per row.
4. Queries that call `ToListAsync` before filtering/sorting/paging.
5. Queries that materialize full entities when projection DTO would be enough.
6. Missing `AsNoTracking` on read-only EF queries.
7. Repeated current version/current price lookups.
8. Repeated BOM context/version lookups.
9. Repeated customer/product/component/warehouse lookups inside loops.
10. Over-wide `Include` chains or query explosion.
11. In-memory filtering after DB load.
12. Page-level query patterns that can become slow with realistic data volumes.
13. Existing tests that make N+1 invisible because data volume is tiny.

## Recommended repository search commands

Run these searches and record meaningful findings:

```bash
rg "foreach|foreach await" src/VPureLux.Web/Pages src/VPureLux.Application
rg "GetCurrentAsync|GetAtDateAsync|EnsureFound|FindAtDateAsync|TryGet|GetListAsync" src/VPureLux.Web/Pages src/VPureLux.Application
rg "GetAsync\(|FindAsync\(|FirstOrDefaultAsync\(|SingleOrDefaultAsync\(" src/VPureLux.Application src/VPureLux.Web/Pages
rg "ToListAsync\(" src/VPureLux.Application src/VPureLux.EntityFrameworkCore src/VPureLux.Web/Pages
rg "Include\(|ThenInclude\(" src/VPureLux.Application src/VPureLux.EntityFrameworkCore
rg "AsNoTracking" src/VPureLux.Application src/VPureLux.EntityFrameworkCore
rg "IsGrantedAsync" src/VPureLux.Web/Pages src/VPureLux.Application
rg "ObjectMapper|Map<|ProjectTo|Select\(" src/VPureLux.Application src/VPureLux.Web/Pages
rg "PageModel|OnGetAsync" src/VPureLux.Web/Pages
```

## Create document

Create:

```text
docs/BACKEND_READ_PERFORMANCE_BATCH_18_PLAN.md
```

## Document requirements

The document must include:

### 1. Executive summary

Summarize whether there are confirmed high-priority performance risks, theoretical risks, and recommended first fix.

### 2. Branch/base branch

Include current branch and base branch.

### 3. Audit scope

List folders/modules inspected.

### 4. Search commands used

List commands actually run.

### 5. High-risk findings table

Use columns:

| Severity | Module | File path | Method/Page | Finding | Suspected query count / risk | Recommended fix | Behavior risk |
|---|---|---|---|---|---|---|---|

Severity values:

```text
Blocker / Should Fix / Nice-to-have / Defer
```

### 6. N+1 findings table

Use columns:

| Caller | Callee | Loop source | Expected data volume | Proposed batch/query alternative |
|---|---|---|---|---|

### 7. Current price/version lookup findings

Cover at least:

- Component suggested selling price
- Product suggested price
- BOM current/published version
- Sales pricing/profit context

### 8. EF query findings

Cover:

- over-fetching
- missing projection
- missing `AsNoTracking`
- `Include` risk
- paging risk
- in-memory filtering risk

### 9. Permission lookup findings

Cover:

- repeated permission checks
- permission checks inside loops
- permission checks already cached/precomputed

### 10. Test coverage findings

Cover:

- existing tests
- missing regression tests for no-price/no-current-version cases
- missing performance-shape/source assertions
- tests that pass only because data volume is tiny

### 11. Proposed implementation batches

Propose only justified batches, for example:

```text
Batch 18.2 - Catalog/Pricing current price batch lookup
Batch 18.3 - BOM/Sales read context optimization, only if justified
Batch 18.4 - Inventory read query optimization, only if justified
Batch 18.5 - Final validation and PR summary
```

### 12. Non-goals

Explicitly state:

- no DB schema/index changes in this phase unless separately approved
- no business-rule changes
- no permission behavior changes
- no API contract changes unless separately approved
- no UI redesign
- no production code changes in this audit

### 13. Recommended first fix batch

Recommend the smallest safe first implementation batch.

## Judgment rules

Follow these rules strictly:

- Do not invent issues just to create work.
- Mark theoretical issues as `Defer` unless there is a clear hot path or UAT evidence.
- Treat Catalog/Pricing current price N+1 as high priority if confirmed.
- Prefer batch queries/read models over per-row AppService calls.
- Preserve strict `GetCurrentAsync` contracts unless a new nullable/batch method is explicitly introduced.
- Prefer adding list-safe `FindCurrentMapAsync` / batch read paths instead of changing strict detail/API behavior.
- Do not propose raw SQL unless EF cannot express the query safely.
- If proposing indexes, mark them as separate DBA/DB migration decisions, not part of the immediate code fix.
- Keep ABP layered architecture:
  - Do not put complex EF logic directly in Razor PageModel if it belongs in Application/Repository/query service.
  - Do not bypass Application layer from Web.
  - Do not change domain semantics for performance only.

## Validation

Run:

```bash
git status
git diff --name-only
```

Only the plan document should be changed.

## Commit

If only the plan doc changed, commit:

```bash
git add docs/BACKEND_READ_PERFORMANCE_BATCH_18_PLAN.md
git commit -m "docs: add Batch 18 backend read performance plan"
```

After commit:

```bash
git status
git log --oneline -5
```

Do NOT push.

## Final output

Return:

1. Summary
2. Files changed
3. High-risk findings
4. Confirmed N+1 findings
5. Current price/version findings
6. EF query findings
7. Permission lookup findings
8. Test coverage findings
9. Proposed fix batches
10. Recommended first fix batch
11. Commit hash, if committed
12. Git status after
13. Source code changed? yes/no

---

## Notes for reviewer

This prompt intentionally does **not** allow implementation.  
After the audit, review `docs/BACKEND_READ_PERFORMANCE_BATCH_18_PLAN.md` and only then approve the first small fix batch.
