# UI ABP Pattern Guide

This guide documents target UI patterns only. Do not implement these patterns
until the corresponding refactor batch is approved.

## 1. List / Inquiry Page

Use list pages for browsing, filtering, and launching actions. Keep read-only
inquiry pages lightweight and permission-aware.

Recommended shape:

```cshtml
@page
@model MyModule.IndexModel
@inject IStringLocalizer<VPureLuxResource> L

@section scripts {
    <abp-script src="/Pages/MyModule/Index.js" />
}

<abp-card>
    <abp-card-header>
        <abp-row>
            <abp-column size-md="_6">
                <h2>@L["MyModule:Title"]</h2>
            </abp-column>
            <abp-column size-md="_6" class="text-end">
                @if (Model.CanCreate)
                {
                    <a asp-page="./CreateModal" class="btn btn-primary" data-open-modal="create">
                        @L["Create"]
                    </a>
                }
            </abp-column>
        </abp-row>
    </abp-card-header>
    <abp-card-body>
        <form method="get" class="mb-3">
            <abp-row>
                <abp-column size-md="_4">
                    <input asp-for="Filter.SearchText" class="form-control" />
                </abp-column>
                <abp-column size-md="_2">
                    <button type="submit" class="btn btn-secondary">@L["Search"]</button>
                </abp-column>
            </abp-row>
        </form>

        <abp-table striped-rows="true">
            <thead>
            <tr>
                <th>@L["Code"]</th>
                <th>@L["Name"]</th>
                <th class="text-end">@L["Actions"]</th>
            </tr>
            </thead>
            <tbody>
            @foreach (var item in Model.Items)
            {
                <tr>
                    <td>@item.Code</td>
                    <td>@item.Name</td>
                    <td class="text-end">
                        <div class="dropdown">
                            <!-- action menu pattern -->
                        </div>
                    </td>
                </tr>
            }
            </tbody>
        </abp-table>
    </abp-card-body>
</abp-card>
```

Rules:

- Do not hardcode internal `href="/..."`; use `asp-page` and `asp-route-*`.
- Do not put `href` on `<abp-button>`.
- Render actions only when their exact permission is granted.
- Prefer code/name over raw GUIDs.
- Use localized headers and statuses.

## 2. Full-Page Operational Workflow

Use full pages for complex workflows that need context and multiple rows:

- BOM Create/Edit.
- Inventory Receipt/Issue/Adjustment.
- Sales Order Create/Edit/Details.
- Audit Export/Reports where large filters are involved.

Recommended conventions:

- Keep workflow state in the PageModel.
- Use `<abp-card>` sections for major steps.
- Use selectors instead of raw IDs where approved.
- Confirm irreversible or important actions.
- Show a busy state while posting.
- Notify success after completion.

Example:

```cshtml
@section scripts {
    <abp-script src="/Pages/Inventory/Transactions/Receipt.js" />
}

<form method="post" id="receipt-form">
    <abp-input asp-for="Input.IdempotencyKey" />
    <div asp-validation-summary="All" class="text-danger"></div>
    <button type="submit" class="btn btn-primary" id="post-receipt">
        @L["Inventory:PostReceipt"]
    </button>
</form>
```

## 3. Create / Edit Modal

Use ABP modal patterns for small CRUD flows:

- Customer create/edit.
- Customer Group create/edit.
- Warehouse create.
- Pricing version create.
- Catalog image replace/remove confirmation if suitable.

Example modal page:

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
        <abp-modal-footer buttons="@(AbpModalButtons.Cancel|AbpModalButtons.Save)">
        </abp-modal-footer>
    </abp-modal>
</abp-dynamic-form>
```

Example list-page JavaScript:

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

    document.querySelector('[data-create-customer]')?.addEventListener('click', function (event) {
        event.preventDefault();
        createModal.open();
    });
})();
```

Rules:

- Modal pages still require `[Authorize]`.
- Modal submissions call existing application services.
- Modal conversion must not change backend behavior.

## 4. Read-Only Detail Modal

Use read-only modals for quick inspection:

- Audit detail.
- Customer detail.
- Customer Group detail.
- Pricing version detail if needed.

Example:

```javascript
const detailModal = new abp.ModalManager({
    viewUrl: abp.appPath + 'Audit/DetailsModal'
});

document.addEventListener('click', function (event) {
    const button = event.target.closest('[data-audit-detail-id]');
    if (!button) {
        return;
    }
    detailModal.open({ id: button.dataset.auditDetailId });
});
```

Rules:

- Do not expose sensitive fields unless their permissions are granted.
- Render JSON metadata safely and readably.

## 5. Action Menu

Use an action menu when a row has multiple operations.

```cshtml
<div class="dropdown">
    <button class="btn btn-sm btn-outline-secondary dropdown-toggle" type="button" data-bs-toggle="dropdown">
        @L["Actions"]
    </button>
    <div class="dropdown-menu dropdown-menu-end">
        <a asp-page="./Details" asp-route-id="@item.Id" class="dropdown-item">@L["Details"]</a>
        @if (Model.CanEdit)
        {
            <a asp-page="./Edit" asp-route-id="@item.Id" class="dropdown-item">@L["Edit"]</a>
        }
        @if (Model.CanDeactivate)
        {
            <button type="button" class="dropdown-item" data-deactivate-id="@item.Id">
                @L["Deactivate"]
            </button>
        }
    </div>
</div>
```

Rules:

- The action menu must not show actions the user cannot perform.
- Use buttons for JavaScript actions and anchors for navigation.

## 6. Confirmation Dialog

Use `abp.message.confirm` before sensitive actions:

```javascript
(function () {
    const l = abp.localization.getResource('VPureLux');

    document.addEventListener('click', function (event) {
        const button = event.target.closest('[data-publish-bom-id]');
        if (!button) {
            return;
        }

        abp.message.confirm(l('Bom:ConfirmPublish'), l('Confirm')).then(function (confirmed) {
            if (!confirmed) {
                return;
            }

            abp.ui.setBusy();
            abp.ajax({
                url: abp.appPath + 'api/bom/versions/' + button.dataset.publishBomId + '/publish',
                type: 'POST'
            }).done(function () {
                abp.notify.success(l('SavedSuccessfully'));
                location.reload();
            }).always(function () {
                abp.ui.clearBusy();
            });
        });
    });
})();
```

Use confirmations for:

- Deactivate.
- Remove image.
- Publish.
- Archive.
- Confirm order.
- Cancel order.
- Remove order line.
- Inventory posting.
- Audit export if export may be large.

## 7. Success / Error Notification

Use ABP notifications after asynchronous operations:

```javascript
abp.notify.success(l('SavedSuccessfully'));
abp.notify.error(l('OperationFailed'));
```

Server-side postbacks can use TempData or ABP notification mechanisms if the
page remains server-rendered. The pattern must be consistent per module.

## 8. Page-Specific JavaScript Registration

Preferred page registration:

```cshtml
@section scripts {
    <abp-script src="/Pages/Sales/Orders/Edit.js" />
}
```

Rules:

- Use lowercase `scripts` section.
- Use `<abp-script>`, not raw `<script src>`.
- Keep page JavaScript next to the page it serves, unless shared.
- Shared behavior belongs in `wwwroot/js/shared` or an approved bundle.
- Do not define global functions from Razor pages.

## 9. Busy State Handling

Use a busy state around asynchronous operations:

```javascript
abp.ui.setBusy(document.body);
abp.ajax({
    url: url,
    type: 'POST',
    data: JSON.stringify(input)
}).done(function () {
    abp.notify.success(l('SavedSuccessfully'));
}).always(function () {
    abp.ui.clearBusy(document.body);
});
```

For form-scoped actions:

```javascript
const form = document.getElementById('sales-order-form');
abp.ui.setBusy(form);
// call service
abp.ui.clearBusy(form);
```

## 10. Route Generation

Correct:

```cshtml
<a asp-page="/Sales/Details" asp-route-id="@order.Id" class="btn btn-sm btn-outline-primary">
    @L["Details"]
</a>
```

Avoid:

```cshtml
<a href="/Sales/Details/@order.Id" class="btn btn-sm btn-outline-primary">@L["Details"]</a>
```

Avoid:

```cshtml
<abp-button href="/Sales/Create">@L["Create"]</abp-button>
```

Use:

```cshtml
<a asp-page="/Sales/Create" class="btn btn-primary">@L["Create"]</a>
```

## 11. Permission-Aware Field Rendering

Example:

```cshtml
@if (Model.CanViewProfit)
{
    <th>@L["Sales:Profit"]</th>
}
```

Rules:

- Buttons use operation permission.
- Links use destination permission.
- Sensitive fields use field/data permission.
- UI checks supplement backend authorization; they do not replace it.

