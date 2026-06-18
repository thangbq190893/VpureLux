# CODEX V2 Inventory Posting UAT Workflow

## Goal

Implement **V2 UAT Backlog Batch 2: Inventory Posting UAT Workflow**.

This batch improves operator UX for Inventory posting pages while preserving certified backend behavior.

## Read First

Codex must read these files before coding:

- `CODEX_README_VPURELUX_V2.md`
- `BUSINESS_ARCHITECTURE_DECISIONS_V2.md`
- `MODULE_SPECIFICATIONS_V2.md`
- `UI_UX_ABP_GUIDE_V2.md`
- `V2_FINAL_UAT_BUG_BACKLOG.md`

## Tech Stack

- ABP Framework 10.x
- .NET 10
- EF Core
- Architecture:
  - Domain
  - Application Layer
  - Application.Contracts
  - EntityFrameworkCore
  - Razor Pages

## Target Backlog Items

Implement only:

- UAT-002
- UAT-003
- UAT-025
- UAT-026
- UAT-027

## Scope

Inventory posting pages only:

1. Receipt
2. Issue
3. Adjustment

Required outcomes:

- Receipt/Issue/Adjustment support realistic multi-line operator entry.
- IdempotencyKey remains hidden from operators.
- Dates use Vietnamese text format `dd/MM/yyyy`.
- Posting actions have ABP confirmation, busy state, and success/error notification.
- Existing Inventory FIFO and costing behavior remain unchanged.

## Do Not Touch

Do not change:

- Sales behavior
- Pricing semantics
- BOM behavior
- Catalog behavior
- Audit behavior
- Inventory FIFO business rules
- Inventory Receipt UnitCost meaning
- Product inventory
- Direct Component sales
- EF migrations unless unavoidable for a real contract/model gap

## ABP Architecture Rules

Follow ABP layering strictly:

- Razor PageModels call Application Services only.
- Do not inject DbContext, repositories, domain managers, or domain services into Razor PageModels.
- Domain owns business invariants.
- Application Services coordinate use cases.
- Application.Contracts owns DTOs/interfaces.
- EF Core owns persistence/query implementation.
- Use ApplicationService of ABP.
- Use IRepository or IReadOnlyRepository where needed.
- Use async/await fully.
- Use DTO pattern.
- Use AutoMapper when mapping is needed.
- Naming must be consistent with existing ABP conventions.
- Do not put FIFO, costing, stock calculation, or business rules in Razor/JavaScript.

## Implementation Mapping

### 1. Inventory Receipt

Implement multi-line Receipt UI.

Requirements:

- Keep Receipt as full-page workflow.
- Use warehouse selector showing `Mã kho - Tên kho`.
- Use Component/StockItem selector showing `Mã linh kiện - Tên linh kiện`.
- Allow add/remove receipt lines.
- Preserve indexed model binding across validation failures.
- Preserve selected warehouse, line stock items, quantities, unit costs, reason/note, and date after validation failure.
- Hide IdempotencyKey from operators.
- Preserve UnitCost as actual inventory input cost.
- Do not use Pricing suggested price in Receipt UI.

### 2. Inventory Issue

Implement multi-line Issue UI.

Requirements:

- Keep Issue as full-page workflow.
- Use warehouse selector showing `Mã kho - Tên kho`.
- Use Component/StockItem selector showing `Mã linh kiện - Tên linh kiện`.
- Allow add/remove issue lines.
- Preserve indexed model binding across validation failures.
- Hide IdempotencyKey from operators.
- Do not calculate FIFO in Razor or JavaScript.
- Existing Application/Domain FIFO flow must continue to issue stock.

### 3. Inventory Adjustment

Implement multi-line Adjustment UI if the existing DTO/use case supports line collections.

Requirements:

- Keep Adjustment as full-page workflow.
- Use warehouse selector showing `Mã kho - Tên kho`.
- Use Component/StockItem selector showing `Mã linh kiện - Tên linh kiện`.
- Allow add/remove adjustment lines where supported.
- Preserve indexed model binding across validation failures.
- Hide IdempotencyKey from operators.
- Preserve existing adjustment business rules.

If existing Adjustment contract is truly single-line only, do not redesign domain blindly. Add the smallest Application.Contracts/Application change required for the planned multi-line workflow and report it clearly.

### 4. Add/Remove Line JavaScript

Requirements:

- Keep JavaScript in external files.
- Register scripts with `<abp-script>`.
- No inline script.
- Dynamic rows must preserve dropdown options.
- Remove row must re-index fields so ASP.NET Core model binding works.
- Validation re-render must preserve selected values.

### 5. Confirmation, Busy State, Notification

For Receipt, Issue, and Adjustment posting actions:

- Add ABP confirmation before post.
- Add busy state or duplicate-submit prevention.
- Show success notification after successful post.
- Show user-friendly error or validation message on failure.
- Do not allow accidental duplicate posting.

### 6. Vietnamese Date UX

Standardize posting dates:

- Use text input with `dd/MM/yyyy`.
- Do not rely on native browser date parsing.
- Add or reuse a Web-layer date helper if already present.
- Server-side parse must handle `18/06/2026` correctly.
- Invalid dates must show localized validation message.
- Never convert a valid Vietnamese date into `01/01/0001`.

### 7. Selectors and Data Loading

Use existing Application Services where available:

- `IWarehouseAppService`
- `IStockItemAppService`
- Existing Inventory posting app services

If new Application query support is required:

- Add DTOs to Application.Contracts.
- Add ApplicationService methods.
- Use IReadOnlyRepository/IRepository.
- Optimize EF Core queries:
  - `AsNoTracking`
  - `Select` projection
  - limited `Include`
  - filter active warehouses/active stock items where appropriate
  - sort by code/name
- Do not load large graphs.

No export feature is in scope.
No Background Job is needed unless an existing posting operation is already large and unsuitable for synchronous UI posting.

## Validation Rules

Preserve existing validation and add UI/server-side validation where needed:

- Warehouse is required.
- At least one line is required.
- StockItem is required per line.
- Quantity must be positive.
- Receipt UnitCost must be positive where applicable.
- Adjustment reason is required where existing rules require it.
- Posting date is required and must be valid `dd/MM/yyyy`.

## Permissions

- Preserve existing Inventory permissions.
- Do not add new permissions unless an existing action is missing a required permission check.
- Posting pages must remain permission-protected.

## Tests

Add or update tests without weakening existing tests.

Required coverage:

- Receipt multi-line rendering.
- Receipt model binding preservation.
- Issue multi-line rendering.
- Issue model binding preservation.
- Adjustment multi-line rendering if supported.
- Adjustment model binding preservation if supported.
- IdempotencyKey is hidden from operators.
- `dd/MM/yyyy` date parse/format.
- `<abp-script>` registration.
- No inline scripts.
- Existing Inventory FIFO/Application behavior still passes.

## Local Dev Permission

You may run these without asking:

```powershell
dotnet build
dotnet test
rg
dotnet ef database update
```

Only run EF database update if a migration is unexpectedly generated and truly required.

Do not drop the whole database.

## Validation Commands

Run:

```powershell
dotnet build VPureLux.slnx --no-restore
dotnet test VPureLux.slnx --no-build
```

Run searches:

```powershell
rg -n "IdempotencyKey|WarehouseId|StockItemId" src/VPureLux.Web/Pages/Inventory -g "*.cshtml"
rg -n "<abp-button[^>]*href=" src/VPureLux.Web/Pages/Inventory -g "*.cshtml"
rg -n "href=\"/" src/VPureLux.Web/Pages/Inventory -g "*.cshtml"
rg --pcre2 -n "<script(?![^>]*src=)" src/VPureLux.Web/Pages/Inventory -g "*.cshtml"
rg -n "<script[^>]*src=" src/VPureLux.Web/Pages/Inventory -g "*.cshtml"
rg -n "<abp-script" src/VPureLux.Web/Pages/Inventory -g "*.cshtml"
```

Expected search result:

- No operator-visible raw GUID input/display.
- `IdempotencyKey` only appears as hidden/internal binding where required.
- No inline scripts.
- No raw page-level `<script src>`.
- No `<abp-button href>`.
- No hardcoded internal `href="/..."`.

## Final Report

Report exactly:

1. Summary.
2. Mapping from UAT backlog items to implementation.
3. Files changed by exact path.
4. Layer changes:
   - Domain
   - Application.Contracts
   - Application
   - EntityFrameworkCore
   - Web/Razor
   - Tests
5. Receipt behavior.
6. Issue behavior.
7. Adjustment behavior.
8. Date parsing/formatting behavior.
9. Confirmation/busy/notification behavior.
10. Permission and validation behavior.
11. Tests added/updated.
12. Build/test results.
13. Search results.
14. Migration/database commands run, if any.
15. Forbidden areas changed? yes/no.
16. Sales/Pricing/BOM/Catalog/Audit touched? yes/no per module.
17. Remaining risks.
18. Recommended next batch.

Do not generate the next prompt.
