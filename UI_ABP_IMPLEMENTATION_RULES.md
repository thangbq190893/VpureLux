# VPureLux ABP UI Implementation Rules

## 1. General Rules

- Use Razor Pages and LeptonX.
- Use ABP tag helpers where appropriate.
- Use `asp-page` and `asp-route-*` for internal page navigation.
- Do not hardcode internal `href="/..."`.
- Do not place `href` on `<abp-button>`.
- Use localized strings for all operator text.
- Render actions only when the user has the exact permission for the action/destination.
- Prefer `Code - Name` displays over raw IDs.
- Do not move business rules into UI.

## 2. List Page Pattern

List pages should:

- Use a page title.
- Provide search/filter inputs.
- Use `abp-table` or consistent table markup.
- Render localized statuses.
- Render action menu when a row has multiple actions.
- Avoid raw GUID display.
- Register page-specific JS with lowercase `scripts` and `<abp-script>` only when needed.

Recommended action menu:

```cshtml
<div class="dropdown">
    <button class="btn btn-sm btn-outline-secondary dropdown-toggle" type="button" data-bs-toggle="dropdown">
        @L["Actions"]
    </button>
    <div class="dropdown-menu dropdown-menu-end">
        <a asp-page="./Details" asp-route-id="@item.Id" class="dropdown-item">
            @L["Details"]
        </a>
        @if (Model.CanEdit)
        {
            <a asp-page="./Edit" asp-route-id="@item.Id" class="dropdown-item">
                @L["Edit"]
            </a>
        }
        @if (Model.CanManageStatus && item.IsActive)
        {
            <form method="post" asp-page-handler="Deactivate" asp-route-id="@item.Id">
                <button type="submit" class="dropdown-item" data-status-form="deactivate">
                    @L["Deactivate"]
                </button>
            </form>
        }
    </div>
</div>
```

Use anchors for navigation. Use buttons/forms for post actions.

## 3. Full-Page Operational Workflow Pattern

Full-page workflows include:

- BOM Create/Edit.
- Inventory Receipt/Issue/Adjustment.
- Sales Order Create/Edit/Details.
- Audit Export/Reports.

These pages should remain full page and should not be modalized by default.

Required conventions:

- Use `<abp-card>` sections for major workflow blocks.
- Use selectors for related entities.
- Hide technical fields.
- Confirm posting/confirm/cancel/publish/archive operations.
- Apply busy state while posting.
- Notify success/failure.
- Preserve selected values and option lists when validation fails.

Correct hidden idempotency example:

```cshtml
<form method="post" id="receipt-form">
    <input asp-for="Input.IdempotencyKey" type="hidden" />
    <div asp-validation-summary="All" class="text-danger"></div>

    <label asp-for="Input.WarehouseId">@L["Warehouse"]</label>
    <select asp-for="Input.WarehouseId" asp-items="Model.WarehouseOptions" class="form-select"></select>

    <label asp-for="Input.StockItemId">@L["StockItem"]</label>
    <select asp-for="Input.StockItemId" asp-items="Model.StockItemOptions" class="form-select"></select>

    <button type="submit" class="btn btn-primary" id="post-receipt">
        @L["Inventory:PostReceipt"]
    </button>
</form>
```

Do not render `IdempotencyKey` with `<abp-input>` because that makes it operator-editable.

## 4. Selector Pattern

A selector is required when a field points to another aggregate/entity.

Basic dropdown pattern is acceptable for small data sets:

```cshtml
<select asp-for="Input.WarehouseId" asp-items="Model.WarehouseOptions" class="form-select">
    <option value="">@L["Select"]</option>
</select>
```

Display text should be:

```text
Code - Name
```

Option value can be the GUID `Id`.

Selector rules:

- Load options through existing application services only.
- Do not use DbContext/repository/domain service from PageModel.
- Do not create new selector APIs unless approved.
- Filter inactive records where the business rule requires active records.
- For phase 1 inventory, StockItem selector must exclude Product inventory items.
- On validation failure, reload options before returning `Page()`.

If no existing service supports the selector, mark `requires approved selector/query contract`.

## 5. Create/Edit Modal Pattern

Modal is for small CRUD only.

Modal page:

```cshtml
@page
@model CreateModalModel
@inject IStringLocalizer<VPureLuxResource> L

<abp-dynamic-form abp-model="Input" asp-page="./CreateModal">
    <abp-modal>
        <abp-modal-header title="@L["Create"]"></abp-modal-header>
        <abp-modal-body>
            <abp-form-content />
        </abp-modal-body>
        <abp-modal-footer buttons="@(AbpModalButtons.Cancel|AbpModalButtons.Save)"></abp-modal-footer>
    </abp-modal>
</abp-dynamic-form>
```

List JS:

```javascript
(function () {
    const l = abp.localization.getResource('VPureLux');

    const createModal = new abp.ModalManager({
        viewUrl: abp.appPath + 'Customers/CreateModal'
    });

    createModal.onResult(function () {
        abp.notify.success(l('SavedSuccessfully'));
        location.reload();
    });

    document.addEventListener('click', function (event) {
        const trigger = event.target.closest('[data-create-customer]');
        if (!trigger) {
            return;
        }

        event.preventDefault();
        createModal.open();
    });
})();
```

Acceptance criteria:

- The modal opens without changing browser URL.
- Full-page fallback still works.
- Validation errors stay in the modal.
- PageModel has `[Authorize]`.
- PageModel calls existing app service only.
- On success, modal closes, notification appears, list reloads.

## 6. Read-Only Detail Modal Pattern

Use detail modal for quick inspection.

Do not expose more fields in modal than the full-page details route unless permission rules explicitly allow them.

```javascript
const detailModal = new abp.ModalManager({
    viewUrl: abp.appPath + 'Audit/DetailsModal'
});

document.addEventListener('click', function (event) {
    const trigger = event.target.closest('[data-detail-id]');
    if (!trigger) {
        return;
    }

    event.preventDefault();
    detailModal.open({ id: trigger.dataset.detailId });
});
```

## 7. Confirmation Pattern

Use confirmation for:

- Deactivate.
- Activate when supported.
- Remove image.
- Publish BOM.
- Archive BOM.
- Confirm sales order.
- Cancel sales order.
- Remove sales line.
- Inventory Receipt/Issue/Adjustment posting.
- Audit export when large.

Form-post pattern:

```javascript
document.addEventListener('submit', function (event) {
    const form = event.target.closest('[data-confirm-form]');
    if (!form || form.dataset.confirmed === 'true') {
        return;
    }

    event.preventDefault();

    abp.message.confirm(form.dataset.confirmMessage, form.dataset.confirmTitle)
        .then(function (confirmed) {
            if (!confirmed) {
                return;
            }

            form.dataset.confirmed = 'true';
            abp.ui.setBusy(form);
            form.submit();
        });
});
```

No-JS fallback should remain acceptable when using normal forms.

## 8. Success Notification Pattern

For server postback + redirect, use TempData-backed notification or a consistent server-side notification mechanism.

For modal/ajax result, use:

```javascript
abp.notify.success(l('SavedSuccessfully'));
```

Do not show success before the backend operation has completed.

## 9. Script Registration

Correct:

```cshtml
@section scripts {
    <abp-script src="/Pages/Inventory/Receipt.js" />
}
```

Avoid:

```html
<script src="/Pages/Catalog/catalog-image-preview.js"></script>
```

Avoid inline module scripts:

```html
<script>
    function addRow() { ... }
</script>
```

## 10. Image UI Rules

Catalog image UI must preserve certified image rules:

- Supported formats: JPEG, PNG, WEBP.
- Decoded max size: 2 MB.
- Server calculates SHA256.
- Same-hash upload is idempotent.
- General DTOs and Razor list HTML must not embed Base64.
- Domain events/audit/logging must contain metadata only.
- List pages use thumbnail endpoints and placeholders.
- Preview JS should revoke object URLs after replacement.
- Remove image must ask confirmation.

## 11. Permission Field Pattern

Sensitive fields must be guarded, not just pages.

Example:

```cshtml
@if (Model.CanViewProfit)
{
    <td>@Model.Order.TotalProfit</td>
}
```

Sales:

- Profit fields require `Sales.ViewProfit`.
- Cost fields require `Sales.ViewCost` when present.
- Actual price override UI should respect `Sales.OverridePrice` when override is possible.

## 12. What Not To Do

Do not:

- Add fake disabled actions as if business support exists unless clearly labelled as unavailable.
- Add backend activate/reactivate behavior from UI refactor.
- Make operational workflows modals just to match CRUD samples.
- Show `00000000-0000-0000-0000-000000000000` to users.
- Create JavaScript calculations for FIFO, profit, margin, price versioning, or BOM publication rules.
