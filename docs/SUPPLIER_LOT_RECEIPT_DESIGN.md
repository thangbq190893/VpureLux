# Supplier by Inventory Lot Design

## Issue

Inventory receipt needs supplier selection, and supplier information must be traceable from actual receipt lots.

## Decision

Supplier is not stored on Product or Component. A supplier represents the source of a receipt lot, so the relationship is stored through `AppInventoryLotSuppliers`.

## Data Model

- `AppSuppliers`: supplier master data for create/edit/delete/list.
- `AppInventoryLotSuppliers`: one supplier link per inventory lot.
- Supplier code/name are snapshotted on the lot link so historical receipt data remains readable if the supplier master data changes later.

## Receipt Behavior

- Receipt can optionally select a supplier.
- When a supplier is selected, each generated receipt lot receives one lot-supplier link.
- Existing LotNo auto-generation, FIFO allocation, costing, posting rules, Product, and Component schemas are unchanged.

## UI

- Supplier master page uses ABP DataTables server-side paging.
- Inventory Receipt shows supplier selection at the receipt header.
- Inventory lot history shows supplier code/name when available.

## Validation

- Invalid supplier id is rejected with a friendly Vietnamese business error.
- Missing supplier remains allowed for the first rollout to avoid blocking production receipt flow before supplier master data is fully prepared.

## Tests Run

- `dotnet build VPureLux.slnx --no-restore -m:2`
- `dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Inventory|FullyQualifiedName~Supplier" -m:1`
- `dotnet test test/VPureLux.EntityFrameworkCore.Tests/VPureLux.EntityFrameworkCore.Tests.csproj --no-build --filter "FullyQualifiedName~Inventory|FullyQualifiedName~Supplier" -m:1`
- `dotnet test test/VPureLux.Application.Tests/VPureLux.Application.Tests.csproj --no-build --filter "FullyQualifiedName~Inventory|FullyQualifiedName~Supplier" -m:1` matched no tests.
- `git diff --check`
- `git grep -n -i "linh kiện" -- src test docs BUSINESS_ARCHITECTURE_DECISIONS_V2.md UI_IMPLEMENTATION_DECISION_MATRIX.md UI_REFACTOR_SOURCE_OF_TRUTH.md UI_UX_ABP_GUIDE_V2.md`
