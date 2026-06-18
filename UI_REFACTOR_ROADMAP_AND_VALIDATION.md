# VPureLux UI Refactor Roadmap and Validation

## 1. Roadmap Principle

Do not fix pages in a cosmetic order. Fix pages in the order that unblocks real operator workflows.

Priority order:

1. Raw GUID and technical-field blockers.
2. Permission and sensitive-field correctness.
3. Route and fallback correctness.
4. ABP script/action/notification compliance.
5. Modal conversion for approved simple CRUD.
6. Visual polish and responsive improvements.

## 2. Current Recommended Implementation Batches

### Batch A - Freeze and Audit Current Customer Reference

Goal:

- Verify Customer and CustomerGroups modal/action pattern really works in browser.
- Keep this as reference only if browser behavior is correct.

Scope:

- `/Customers`
- `/CustomerGroups`
- `Index.js`
- modal pages

Manual UAT:

- Create opens modal without URL navigation.
- Edit opens modal without URL navigation.
- Details opens read-only modal.
- Validation error keeps modal open.
- Save closes modal, notifies, reloads list.
- Full-page fallback routes still work.

Validation:

```powershell
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Customer"
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build
```

### Batch B - Inventory Receipt Usability Blocker

Goal:

- Make `/Inventory/Receipt` usable without raw GUIDs.

Scope:

- `Pages/Inventory/Receipt.cshtml`
- `Pages/Inventory/Receipt.cshtml.cs`
- optional `Receipt.js`
- localization
- lightweight tests

Required:

- Warehouse selector displays `Code - Name`.
- StockItem selector displays `Code - Name` and only active Component StockItems in phase 1.
- `IdempotencyKey` hidden.
- ReceivedAt operator-friendly.
- Confirmation/busy/notification if safe.
- Validation failure reloads option lists and keeps selections.

Do not:

- Add selector AppService unless approved.
- Use DbContext/repository from PageModel.
- Enable Product inventory.
- Change Inventory posting behavior.

Validation:

```powershell
dotnet build VPureLux.slnx --no-restore
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Inventory"
```

Manual UAT:

- Create warehouse and component/stock item first.
- Open Receipt.
- No GUIDs visible.
- Submit valid receipt.
- Check Balance/Lots/Ledger.

### Batch C - Inventory Issue and Adjustment Usability Blockers

Goal:

- Remove raw IDs from Issue and Adjustment.

Required:

- Warehouse selector.
- StockItem selector.
- Lot/availability display if existing contracts support it.
- Reason required for Adjustment.
- Technical idempotency hidden.
- Confirm/busy/notify.

Do not:

- Calculate FIFO in JavaScript.
- Create backend selector contract without approval.
- Change adjustment/issue rules.

### Batch D - BOM Selector Blockers

Goal:

- Remove raw ProductId/ComponentId from BOM workflows.

Scope:

- `/Bom`
- `/Bom/Product/{productId}`
- `/Bom/Create/{productId}`
- `/Bom/Edit/{id}`
- externalize editor JS only if needed.

Required:

- Product selector by Code/Name.
- Component selector rows by Code/Name.
- Display product/component names in history/details.
- Keep BOM editor full page.
- Do not change versioning/publish/archive rules.

Validation:

```powershell
dotnet build VPureLux.slnx --no-restore
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~Bom"
```

### Batch E - Catalog UI and Image Script Compliance

Goal:

- Clean Catalog UI without changing certified image backend.

Required:

- Register image preview JS with `<abp-script>`.
- No raw `<script src>`.
- Revoke object URLs if preview code creates them.
- Action menus for Products/Components.
- Deactivate confirmation/notification.
- Truthful inactive action if backend lacks Activate.
- Image remove confirmation.

Do not:

- Change image validation/backend/domain/events.
- Put Base64 in HTML/DTO/log/audit.
- Modalize image upload before manual browser test.

### Batch F - Pricing UI Cleanup

Goal:

- Make Pricing actions and history clear and permission-correct.

Required:

- History links require `Pricing.History`.
- Product/Component display by Code/Name.
- Create version may be modal if using existing services.
- VND formatting.

Do not:

- Add update/delete price version.
- Add Customer-specific pricing.
- Change backdated/version rules.

### Batch G - BOM JavaScript Compliance

Goal:

- Remove inline BOM editor JavaScript.

Required:

- Move row management to `BomEditor.js`.
- Register with `<abp-script>`.
- Keep full-page editor.
- Preserve model binding names/indexes.

### Batch H - Sales Permission and Selector Review

Goal:

- Make Sales safer before full UX enhancement.

Required:

- Hide profit unless `Sales.ViewProfit`.
- Hide cost unless `Sales.ViewCost` if shown.
- Confirm/cancel/remove-line confirmations.
- Do not implement full rich selector until Inventory/BOM selectors are stable.

Do not:

- Recalculate profit/cost in UI.
- Add bundle/serial/warranty/product inventory.
- Snapshot Catalog images into Sales.

### Batch I - Audit UI Cleanup

Goal:

- Improve Audit details and export UX.

Required:

- Read-only detail modal optional.
- Export busy state and notification.
- Readable JSON metadata/change sections.
- Keep append-only/no mutation model.

## 3. Standard Validation Commands

Fast build:

```powershell
dotnet build VPureLux.slnx --no-restore
```

Module test:

```powershell
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build --filter "FullyQualifiedName~<Module>"
```

Full Web tests:

```powershell
dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj --no-build
```

Full solution tests:

```powershell
dotnet test VPureLux.slnx --no-build --no-restore
```

EF pending model check:

```powershell
dotnet ef migrations has-pending-model-changes `
  --project src/VPureLux.EntityFrameworkCore/VPureLux.EntityFrameworkCore.csproj `
  --startup-project src/VPureLux.DbMigrator/VPureLux.DbMigrator.csproj `
  --context VPureLuxDbContext `
  --no-build
```

## 4. Standard Search Checks

Global page navigation/script checks:

```powershell
rg -n '<abp-button[^>]*href=' src/VPureLux.Web/Pages -g '*.cshtml'
rg -n 'href="/' src/VPureLux.Web/Pages -g '*.cshtml'
rg --pcre2 -n '<script(?![^>]*src=)' src/VPureLux.Web/Pages -g '*.cshtml'
rg -n '<script[^>]*src=' src/VPureLux.Web/Pages -g '*.cshtml'
```

Raw technical ID checks:

```powershell
rg -n 'WarehouseId|StockItemId|ProductId|ComponentId|CustomerId|CustomerGroupId|IdempotencyKey|00000000-0000-0000-0000-000000000000' src/VPureLux.Web/Pages -g '*.cshtml'
```

ABP API usage checks:

```powershell
rg -n 'abp.ModalManager|abp.message.confirm|abp.notify|abp.ui.setBusy|abp.ui.clearBusy' src/VPureLux.Web/Pages src/VPureLux.Web/wwwroot -g '*.js' -g '*.cshtml'
```

## 5. Credit-Saving Validation Policy

For small UI-only changes:

1. Build is mandatory.
2. Module-filtered Web tests if quick.
3. Full Web/full solution tests can be deferred only if the final output clearly lists skipped validation.

For changes touching PageModel binding, modal validation, selector loading, or multiple modules:

1. Build mandatory.
2. Module-filtered Web tests mandatory.
3. Full Web tests strongly recommended.
4. Full solution tests before commit.
5. EF check if any non-Web project changed.

## 6. Commit Checkpoints

Commit only after:

- Build passes.
- Relevant tests pass.
- Search checks pass.
- Manual UAT for browser-only behavior is acceptable.

Recommended checkpoint names:

- `Refactor customer UI to ABP reference pattern`
- `Improve inventory receipt selector UX`
- `Improve inventory posting selector UX`
- `Improve BOM selector UX`
- `Clean catalog image script registration`
