using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VPureLux.Catalog;
using VPureLux.Catalog.Components;
using VPureLux.Customers;
using VPureLux.Permissions;
using VPureLux.Warranty;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Warranty.Assets;

[Authorize(VPureLuxPermissions.Warranty.ManageAssets)]
public class CreateExternalModel : VPureLuxPageModel
{
    private readonly IWarrantyAppService _warranty;
    private readonly ICustomerAppService _customers;
    private readonly IComponentAppService _components;

    [BindProperty]
    public CreateExternalCustomerAssetDto Input { get; set; } = new();

    public List<SelectListItem> CustomerOptions { get; private set; } = [];
    public List<SelectListItem> ComponentOptions { get; private set; } = [];

    public CreateExternalModel(
        IWarrantyAppService warranty,
        ICustomerAppService customers,
        IComponentAppService components)
    {
        _warranty = warranty;
        _customers = customers;
        _components = components;
    }

    public async Task OnGetAsync()
    {
        await LoadOptionsAsync();
        Input.IdempotencyKey = Guid.NewGuid().ToString("N");
        Input.Positions = Enumerable.Range(1, 9).Select(index => new ExternalAssetPositionInput
        {
            PositionCode = $"CORE-{index:D2}",
            PositionName = $"Lõi {index}",
            Quantity = 1
        }).ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        try
        {
            var result = await _warranty.CreateExternalAssetAsync(Input);
            Alerts.Success(L["Warranty:ExternalAssetCreated", result.AssetNo, result.CreatedReminderCount]);
            if (result.DuplicateSerialAssetNos.Count > 0)
            {
                Alerts.Warning(L["Warranty:DuplicateSerialWarning", string.Join(", ", result.DuplicateSerialAssetNos)]);
            }
            return RedirectToPage("/Warranty/Assets/Details", new { id = result.AssetId });
        }
        catch (BusinessException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Code == null ? exception.Message : L[exception.Code]);
            await LoadOptionsAsync();
            return Page();
        }
    }

    private async Task LoadOptionsAsync()
    {
        var customers = await _customers.GetListAsync(new GetCustomerListInput
        {
            Status = CustomerStatus.Active,
            Sorting = "code asc",
            MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount
        });
        CustomerOptions = customers.Items
            .OrderBy(customer => customer.Code)
            .Select(customer => new SelectListItem($"{customer.Code} - {customer.Name}", customer.Id.ToString()))
            .ToList();

        var components = await _components.GetListAsync(new GetComponentListInput
        {
            Status = CatalogItemStatus.Active,
            Sorting = "code asc",
            MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount
        });
        ComponentOptions = components.Items
            .OrderBy(component => component.Code)
            .Select(component => new SelectListItem(
                $"{component.Code} - {component.Name} ({component.Unit})",
                component.Id.ToString()))
            .ToList();
        ComponentOptions.Insert(0, new SelectListItem(L["Warranty:Unmapped"], string.Empty));
    }
}
