using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VPureLux.Catalog;
using VPureLux.Catalog.Components;
using VPureLux.Permissions;
using VPureLux.Warranty;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Warranty.Assets;

[Authorize(VPureLuxPermissions.Warranty.ManageAssets)]
public class EditModel : VPureLuxPageModel
{
    private readonly IWarrantyAppService _warranty;
    private readonly IComponentAppService _components;

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public UpdateExternalCustomerAssetDto Input { get; set; } = new();

    public CustomerAssetDetailDto Asset { get; private set; } = new();
    public List<SelectListItem> ComponentOptions { get; private set; } = [];

    public EditModel(IWarrantyAppService warranty, IComponentAppService components)
    {
        _warranty = warranty;
        _components = components;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadPageAsync();
        if (Asset.Source != CustomerAssetSource.External)
        {
            return RedirectToPage("/Warranty/Assets/Details", new { id = Id });
        }
        Input = new UpdateExternalCustomerAssetDto
        {
            Model = Asset.Model ?? string.Empty,
            Brand = Asset.Brand,
            SerialNo = Asset.SerialNo,
            InstallationAddress = Asset.InstallationAddress,
            ExternalReference = Asset.ExternalReference,
            Note = Asset.Note,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Positions = Asset.Positions
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadPageAsync();
            return Page();
        }
        try
        {
            var result = await _warranty.UpdateExternalAssetAsync(Id, Input);
            Alerts.Success(L["Warranty:ExternalAssetUpdated", result.AssetNo, result.CreatedReminderCount]);
            if (result.DuplicateSerialAssetNos.Count > 0)
            {
                Alerts.Warning(L["Warranty:DuplicateSerialWarning", string.Join(", ", result.DuplicateSerialAssetNos)]);
            }
            return RedirectToPage("/Warranty/Assets/Details", new { id = Id });
        }
        catch (BusinessException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Code == null ? exception.Message : L[exception.Code]);
            await LoadPageAsync();
            return Page();
        }
    }

    private async Task LoadPageAsync()
    {
        Asset = await _warranty.GetAssetDetailsAsync(Id);
        var components = await _components.GetListAsync(new GetComponentListInput
        {
            Status = CatalogItemStatus.Active,
            Sorting = "code asc",
            MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount
        });
        ComponentOptions = components.Items.OrderBy(component => component.Code)
            .Select(component => new SelectListItem($"{component.Code} - {component.Name} ({component.Unit})", component.Id.ToString()))
            .ToList();
        ComponentOptions.Insert(0, new SelectListItem(L["Warranty:Unmapped"], string.Empty));
    }
}
