# Batch 18.5 - Backend Read Performance Final Validation and PR Summary Prompt

## Purpose

Use this prompt with **Codex Agent** to close out Batch 18 backend read-performance work for VPureLux.

This batch is **documentation/validation only**. It must not introduce new optimizations.

Completed work so far:

- Batch 18.1: backend read-performance audit plan
- Batch 18.2: batch component current price lookup
- Batch 18.3: batch product pricing context reads

Batch 18.5 validates the branch, performs targeted runtime smoke if possible, creates the PR summary document, and commits documentation only.

---

## Recommended Codex Settings

Use:

```text
Model: GPT-5.5
Reasoning: High
Speed: Standard
```

Do **not** use Fast mode for final validation/PR summary.

---

## Short Prompt to Paste into Codex

If this file is added to the repository as:

```text
docs/BATCH_18_5_BACKEND_READ_PERFORMANCE_CLOSEOUT_PROMPT.md
```

paste this short instruction into Codex:

```text
Read and execute the instruction file:

docs/BATCH_18_5_BACKEND_READ_PERFORMANCE_CLOSEOUT_PROMPT.md

Follow it strictly.

This is Batch 18.5 only:
- Do not change production code.
- Do not add optimizations.
- Run final validation for Batch 18.1/18.2/18.3.
- Runtime-smoke key pricing-context pages if local app can run.
- Create docs/BACKEND_READ_PERFORMANCE_BATCH_18_PR_SUMMARY.md.
- Mark Batch 18 complete in the plan doc if needed.
- Commit documentation only if validation passes.
- Do not push.
```

---

## Full Prompt

Owner: Codex Agent

Task: Batch 18.5 - Final validation and PR summary for backend read-performance batching.

This is a documentation/validation task only.

Do NOT change production code.
Do NOT change Application code.
Do NOT change Domain code.
Do NOT change EntityFrameworkCore code.
Do NOT change Application.Contracts.
Do NOT change Razor pages.
Do NOT change CSS.
Do NOT change JavaScript.
Do NOT change tests unless a test is unexpectedly broken by documentation-only assumptions, which should not happen.
Do NOT add new optimizations.
Do NOT refactor.
Do NOT change database schema.
Do NOT add migrations.
Do NOT add indexes.
Do NOT change API contracts.
Do NOT change DTO contracts.
Do NOT change business rules.
Do NOT change permissions.
Do NOT change pricing semantics.
Do NOT change BOM behavior.
Do NOT change Inventory behavior.
Do NOT change Sales behavior.
Do NOT push unless explicitly told.

## Context

Current branch:

```text
refactor/performance
```

Completed Batch 18 commits:

```text
d143a7b docs: add Batch 18 backend read performance plan
0395b22 fix(pricing): batch load component current prices
a6cc5df fix(pricing): batch product pricing context reads
```

Batch 18.1 audit found:

- Catalog/Pricing current component price N+1.
- Product pricing context `1 + P + P + I` query shape.
- Inventory/Sales broader read tuning is lower priority and should remain deferred unless concrete UAT/prod evidence appears.

Batch 18.2 fixed:

- Catalog Components per-row `GetCurrentAsync`.
- Pricing Index component tab per-row `GetCurrentAsync`.
- Added list-safe nullable current component price batch lookup.
- Preserved strict `GetCurrentAsync` / `GetAtDateAsync`.

Batch 18.3 fixed:

- ProductPricingContext product price N+1.
- Published BOM per-product lookup.
- Per-BOM-item component current price lookup.
- Scoped consumers where applicable:
  - Catalog Products
  - Pricing Index
  - BOM Product
  - Sales Create/Edit/Details

## Goal

Create a final Batch 18 PR/MR summary document and run final validation. Do not make new code changes.

## Allowed Files

Only these files may be changed:

```text
docs/BACKEND_READ_PERFORMANCE_BATCH_18_PR_SUMMARY.md
docs/BACKEND_READ_PERFORMANCE_BATCH_18_PLAN.md
```

`docs/BACKEND_READ_PERFORMANCE_BATCH_18_PLAN.md` may be updated only to mark Batch 18.2, 18.3, and 18.5 status complete/deferred.

Do not edit source code, tests, config, or prompt files.

## Steps

### 1. Inspect branch state

Run:

```powershell
git status
git log --oneline --decorate -20
git diff --name-status main...HEAD
```

Record the diff summary.

### 2. Create PR summary doc

Create:

```text
docs/BACKEND_READ_PERFORMANCE_BATCH_18_PR_SUMMARY.md
```

The document must include:

1. Title
2. Branch/base branch
3. Scope summary
4. Commit list
5. Audit conclusion from Batch 18.1
6. Batch 18.2 summary:
   - root cause
   - old read shape
   - new batch lookup
   - files/areas changed
   - behavior preserved
7. Batch 18.3 summary:
   - root cause
   - old read shape `1 + P + P + I`
   - new read shape: product query + batch product price map + batch published BOM map + batch component price map
   - files/areas changed
   - consumers covered
   - behavior preserved
8. What explicitly did not change
9. API/DTO/DB/schema/business-rule safety notes
10. Permission behavior notes
11. Test results
12. Runtime smoke results, if performed
13. Deferred items
14. Suggested PR/MR description text
15. Suggested PR/MR checklist

### 3. Explicitly state what did not change

The summary must clearly state:

- No database schema changes.
- No migrations.
- No indexes.
- No API contract changes.
- No DTO contract changes.
- No business-rule changes.
- No pricing semantic changes.
- Strict `GetCurrentAsync` / `GetAtDateAsync` behavior preserved.
- Missing-price fallback behavior preserved.
- No permission behavior changes.
- No route/handler/form binding changes.
- No UI layout/CSS/JS changes.
- No Inventory/FIFO/posting changes.
- No Sales lifecycle/write-path changes.
- No BOM publish/archive/domain behavior changes.
- No Audit export behavior changes.

### 4. Deferred items

Deferred items must include:

- Inventory Balances/Lots/Ledger read tuning.
- Sales order repository/list projection tuning.
- Audit query/index tuning as a separate DBA/migration decision if ever needed.
- HealthChecks UI SSL noise.
- Catalog Components visual width if not already handled separately.
- Runtime query-count instrumentation if desired later.
- Any DB index proposal as separate approval.

### 5. Suggested PR/MR title

Use:

```text
Batch 18: Backend read performance - pricing context batching
```

### 6. Suggested PR/MR checklist

Include this checklist in the PR summary doc:

```text
- [x] Batch 18.1 backend read-performance audit completed
- [x] Batch 18.2 component current price N+1 removed
- [x] Batch 18.3 product pricing context N+1 removed
- [x] Catalog Components price lookup batched
- [x] Pricing Index component price lookup batched
- [x] Product pricing context product prices batched
- [x] Product pricing context published BOM lookup batched
- [x] Product pricing context component prices batched
- [x] Catalog Products consumer scoped
- [x] BOM Product consumer scoped
- [x] Sales Create/Edit/Details consumers scoped
- [x] Strict GetCurrent/GetAtDate behavior preserved
- [x] Missing-price fallback preserved
- [x] Pricing permission behavior preserved
- [x] DTO/API contracts unchanged
- [x] No DB schema/migration/index changes
- [x] No business-rule changes
- [x] Focused EF tests passed
- [x] Focused Web tests passed
- [x] Full Web.Tests passed
- [ ] Runtime smoke passed, if performed
```

If runtime smoke is not performed, replace the last item with:

```text
- [ ] Runtime smoke not performed in this batch; recommended before merge
```

## Validation

Run cleanup first:

```powershell
dotnet build-server shutdown

Get-Process testhost -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process dotnet -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process VBCSCompiler -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process MSBuild -ErrorAction SilentlyContinue | Stop-Process -Force
```

Do NOT kill Cursor, SQL Server, or Redis.

Run build:

```powershell
dotnet build VPureLux.slnx --no-restore -m:2
```

Run focused EF tests:

```powershell
dotnet test test/VPureLux.EntityFrameworkCore.Tests/VPureLux.EntityFrameworkCore.Tests.csproj --no-build --filter "FullyQualifiedName~Pricing|FullyQualifiedName~Bom" -m:1
```

Run focused Web tests:

```powershell
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Catalog|FullyQualifiedName~Pricing|FullyQualifiedName~Bom|FullyQualifiedName~Sales" -m:1
```

Run full Web tests:

```powershell
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build -m:1
```

Record results in the PR summary document.

## Runtime Smoke

If the app can be run locally, perform a minimal authenticated smoke.

Run:

```powershell
dotnet run --project src/VPureLux.Web/VPureLux.Web.csproj --no-build
```

Open/smoke these pages:

```text
/Catalog/Components
/Catalog/Products
/Pricing
/Bom/Product/{existingProductId}
/Sales/Create
/Sales/Edit/{existingOrderId}
/Sales/Details/{existingOrderId}
```

For each page, record:

- PASS / FAIL / BLOCKED
- whether the page loads
- whether pricing context remains visible/correct for admin
- whether missing-price/no-BOM placeholders remain correct
- any exception in logs

If authentication or local runtime blocks smoke, document the blocker. Do not fake smoke results.

Do not record video.

## Commit

After validation, check:

```powershell
git status
git diff --name-only
```

Only allowed docs should be changed in Batch 18.5.

If validation passes and only allowed docs changed, commit:

```bash
git add docs/BACKEND_READ_PERFORMANCE_BATCH_18_PR_SUMMARY.md docs/BACKEND_READ_PERFORMANCE_BATCH_18_PLAN.md
git commit -m "docs: add Batch 18 backend read performance PR summary"
```

Do NOT push.

## Final Output

Return:

1. Summary
2. Files changed
3. PR summary sections created
4. Diff scope summary from main
5. Build result
6. Focused EF test result
7. Focused Web test result
8. Full Web.Tests result
9. Runtime smoke result, if performed
10. Commit hash, if committed
11. Git status after
12. Any forbidden files changed? yes/no
13. Recommended next step for PR/MR

---

## Reviewer Notes

Reject or ask for revision if Batch 18.5 changes source code, tests, schema, config, UI, or adds more optimizations.

Batch 18.5 is close-out only.
