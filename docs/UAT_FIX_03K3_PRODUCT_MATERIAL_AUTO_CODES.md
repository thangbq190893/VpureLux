# UAT Fix 03K.3 - Product and Material Auto Codes

## Reason

03K.1 identified Product and Vật tư/Component codes as high-value first candidates for automatic business-code generation. 03K.2 added the shared `IBusinessCodeGenerator`. This batch applies that infrastructure to Product and Vật tư create flows only.

## Scope

- Product create now generates a code when `CreateProductDto.Code` is blank.
- Vật tư/Component create now generates a code when `CreateComponentDto.Code` is blank.
- Product and Vật tư create pages/modals show `Tự động sinh khi lưu` instead of requiring a code input.
- Explicit code values are still accepted for API, seed, and import compatibility.
- Existing edit/detail/list display of code remains unchanged.

## Generated Prefixes

| Flow | SequenceName | Prefix | Example |
|---|---|---|---|
| Product | `Product` | `PROD` | `PROD-202607060001` |
| Vật tư/Component | `Material` | `MAT` | `MAT-202607060001` |

The format remains:

```text
{PREFIX}-{yyyyMMdd}{sequence:D4}
```

## Backend Generation Behavior

Generation happens in the Application create methods, not in Razor or JavaScript.

Product:

- Blank `CreateProductDto.Code` calls `IBusinessCodeGenerator`.
- Sequence name is `Product`.
- Prefix is `PROD`.
- Duplicate checks still use `IProductRepository.CodeExistsAsync`.
- `CatalogManager.CreateProductAsync` remains the final domain-level duplicate guard.

Vật tư/Component:

- Blank `CreateComponentDto.Code` calls `IBusinessCodeGenerator`.
- Sequence name is `Material`.
- Prefix is `MAT`.
- Duplicate checks still use `IComponentRepository.CodeExistsAsync`.
- `CatalogManager.CreateComponentAsync` remains the final domain-level duplicate guard.

## Redis, Lock, And DB Seed Usage

The generator keeps the 03K.2 cache and lock behavior:

- Cache logical key: `Sequence:{SequenceName}:{yyyyMMdd}`.
- Lock key: `VPureLux:SequenceLock:{SequenceName}:{yyyyMMdd}`.
- Cache stores the latest allocated integer for about seven days.

When cache is missing, Product and Vật tư seed from DB by MAX numeric suffix:

- Product: existing codes matching `PROD-{yyyyMMdd}%`.
- Vật tư/Component: existing codes matching `MAT-{yyyyMMdd}%`.

COUNT is not used.

## UI Behavior

Product and Vật tư create pages/modals now display a static code row:

```text
Tự động sinh khi lưu
```

The create forms do not post `Input.Code`, so an empty disabled field cannot overwrite the generated server-side value. Edit/detail/list pages continue to display code as before, and search/list filters by code are preserved.

## Explicit-Code Compatibility

Explicit `Code` remains supported when supplied through API, seed, or import paths. This keeps backward compatibility with existing callers and test data. Explicit codes still pass through the same duplicate checks and DB unique indexes.

## Intentionally Not Changed

- No DB/schema/migration/index changes.
- No Customer, CustomerGroup, Warehouse, LotNo, Receipt, Adjustment, Sales, BOM, or Pricing changes.
- No Inventory posting, FIFO, or costing changes.
- No backend identifier renames; `Component` remains the backend model name.
- No code allocation on page GET.
- No Razor or JavaScript code generation.

## Tests Run

Validation completed:

```text
dotnet build VPureLux.slnx --no-restore -m:2 -> passed
dotnet test test/VPureLux.Application.Tests/VPureLux.Application.Tests.csproj --no-build --filter "FullyQualifiedName~Catalog|FullyQualifiedName~BusinessCode" -m:1 -> passed, 28 tests
dotnet test test/VPureLux.EntityFrameworkCore.Tests/VPureLux.EntityFrameworkCore.Tests.csproj --no-build --filter "FullyQualifiedName~Catalog" -m:1 -> passed, 26 tests
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Catalog" -m:1 -> passed, 24 tests
git diff --check -> passed
legacy component wording grep -> no matches
```

Manual browser smoke is deferred for this batch.

## Deferred

- Customer code generation.
- Warehouse code generation.
- Inventory lot number generation.
- Manual override policy and permissions beyond the existing explicit-code API compatibility.
- Any future convergence with Sales/BOM/Pricing numbering.
