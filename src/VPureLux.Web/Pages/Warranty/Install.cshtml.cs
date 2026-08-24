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
using Volo.Abp.Data;

namespace VPureLux.Web.Pages.Warranty;

[Authorize(VPureLuxPermissions.Warranty.ManageInstallations)]
public class InstallModel : VPureLuxPageModel
{
    private readonly IWarrantyAppService _warrantyAppService;
    private readonly IComponentAppService _componentAppService;

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public ConfirmAssetInstallationDto Input { get; set; } = new();

    public AssetInstallationEditorDto Asset { get; private set; } = new();
    public List<SelectListItem> ComponentOptions { get; private set; } = [];
    public string TechnicianName => CurrentUser.UserName ?? string.Empty;

    public InstallModel(
        IWarrantyAppService warrantyAppService,
        IComponentAppService componentAppService)
    {
        _warrantyAppService = warrantyAppService;
        _componentAppService = componentAppService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadPageAsync();
        Input = new ConfirmAssetInstallationDto
        {
            InstalledAt = Clock.Now,
            SerialNo = Asset.SerialNo,
            InstallationAddress = Asset.InstallationAddress,
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
            var result = await _warrantyAppService.ConfirmInstallationAsync(Id, Input);
            Alerts.Success(L["Warranty:InstallationConfirmed", result.CreatedReminderCount]);
            return RedirectToPage("/Warranty/PendingInstallations");
        }
        catch (AbpDbConcurrencyException)
        {
            ModelState.AddModelError(string.Empty, L["Warranty:InstallationConcurrencyError"]);
        }
        catch (BusinessException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Code == null ? exception.Message : L[exception.Code]);
        }

        await LoadPageAsync();
        return Page();
    }

    private async Task LoadPageAsync()
    {
        Asset = await _warrantyAppService.GetInstallationEditorAsync(Id);
        var components = await _componentAppService.GetListAsync(new GetComponentListInput
        {
            Status = CatalogItemStatus.Active,
            Sorting = "code asc",
            MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount
        });
        ComponentOptions = components.Items
            .OrderBy(component => component.Code)
            .ThenBy(component => component.Name)
            .Select(component => new SelectListItem(
                $"{component.Code} - {component.Name} ({component.Unit})",
                component.Id.ToString()))
            .ToList();
        ComponentOptions.Insert(0, new SelectListItem(L["Warranty:Unmapped"], string.Empty));
    }
}
