# UAT Fix 04H.1 - Product Code Manual Input

## Issue / Business Reason

Product Code was previously generated as `PROD-yyyyMMddNNNN` when a product was created with a blank code. The business rule changed because real products may use manufacturer, model, or custom commercial codes that must be entered explicitly by users.

## Root Cause / Current Behavior

`CreateProductDto.Code` was optional, Product Create pages displayed the shared auto-code hint, and `ProductAppService` generated a `PROD-*` code when the submitted code was blank. This made blank Product Code a successful create path instead of a validation error.

## Fix

- Product Create full page and modal now render an editable required Product Code field.
- `CreateProductDto.Code` is required with the friendly Vietnamese message `Vui lòng nhập mã sản phẩm.`
- `ProductAppService` trims manual Product Code and no longer calls the business-code generator for Product create.
- Whitespace-only Product Code is blocked by application validation with `CATALOG_011`.
- Duplicate Product Code still uses the existing domain uniqueness check and is localized as `Mã sản phẩm đã tồn tại.`

## Validation Behavior

- Blank Product Code is blocked.
- Whitespace Product Code is trimmed and then blocked if empty.
- Manual Product Code is preserved after trimming leading/trailing spaces.
- Duplicate Product Code is blocked with a friendly Vietnamese message.
- Product list/details/search keep displaying Product Code.
- Existing `PROD-*` products remain valid because the code format is not restricted.

## Product Edit Behavior

Product Edit remains unchanged: Product Code is shown as readonly/disabled and update DTO still excludes Code.

## Intentionally Not Changed

- Vật tư/Component Code auto-generation remains `MAT-yyyyMMddNNNN`.
- Inventory LotNo auto-generation remains `LOT-yyyyMMddNNNN`.
- SalesOrderNo generation is unchanged.
- Customer/Warehouse behavior is unchanged.
- BusinessCodeGenerator infrastructure remains for other flows.
- Domain/EF schema and migrations are unchanged.
- BOM, Pricing, Inventory, and Sales behavior are unchanged.

## Tests Run

- `dotnet build VPureLux.slnx --no-restore -m:2` - passed, 1 warning, 0 errors on final rebuild.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Catalog|FullyQualifiedName~Product|FullyQualifiedName~Component" -m:1` - passed, 72 total.
- `dotnet test test/VPureLux.EntityFrameworkCore.Tests/VPureLux.EntityFrameworkCore.Tests.csproj --no-build --filter "FullyQualifiedName~Catalog|FullyQualifiedName~Product|FullyQualifiedName~Component" -m:1` - passed, 53 total.
- `dotnet test test/VPureLux.Application.Tests/VPureLux.Application.Tests.csproj --no-build --filter "FullyQualifiedName~Catalog|FullyQualifiedName~Product|FullyQualifiedName~Component" -m:1` - passed, 19 total.
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Inventory" -m:1` - passed, 63 total.
- `git diff --check` - passed; Git reported CRLF normalization warnings only.
- Terminology grep requested by task - returned existing audit/evidence/prior fix-doc references only; this fix did not introduce user-facing legacy material wording.

## Manual Smoke Checklist

Deferred unless a browser session is run against the app:

- Open Product Create and confirm `Mã sản phẩm *` is editable.
- Submit with blank Product Code and confirm field-level Vietnamese validation.
- Submit with duplicate Product Code and confirm friendly duplicate message.
- Submit with spaces around a manual Product Code and confirm saved code is trimmed.
- Open Product Edit and confirm Product Code remains readonly.
- Open Vật tư Create and confirm auto-code hint still appears.
- Post an Inventory Receipt with blank LotNo and confirm `LOT-*` generation still works.
