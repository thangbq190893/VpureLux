using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Inventory;
using VPureLux.Permissions;
using Volo.Abp;

namespace VPureLux.Web.Pages.Inventory;

[Authorize(VPureLuxPermissions.Inventory.ManageWarehouses)]
public class WarehousesModel : VPureLuxPageModel
{
    private readonly IWarehouseAppService _service;

    [BindProperty] public CreateWarehouseDto NewWarehouse { get; set; } = new();
    public IReadOnlyList<WarehouseDto> Warehouses { get; private set; } = [];
    [TempData] public string? StatusMessageKey { get; set; }

    public WarehousesModel(IWarehouseAppService service) => _service = service;

    public async Task OnGetAsync() => await LoadWarehousesAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadWarehousesAsync();
            return Page();
        }

        try
        {
            await _service.CreateAsync(NewWarehouse);
        }
        catch (BusinessException exception)
        {
            AddBusinessError(exception);
            await LoadWarehousesAsync();
            return Page();
        }

        StatusMessageKey = "Inventory:WarehouseCreatedSuccessfully";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostActivateAsync(Guid id)
    {
        try
        {
            await _service.ActivateAsync(id);
        }
        catch (BusinessException exception)
        {
            AddBusinessError(exception);
            await LoadWarehousesAsync();
            return Page();
        }

        StatusMessageKey = "Inventory:WarehouseActivatedSuccessfully";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id)
    {
        try
        {
            await _service.DeactivateAsync(id);
        }
        catch (BusinessException exception)
        {
            AddBusinessError(exception);
            await LoadWarehousesAsync();
            return Page();
        }

        StatusMessageKey = "Inventory:WarehouseDeactivatedSuccessfully";
        return RedirectToPage();
    }

    private async Task LoadWarehousesAsync()
    {
        Warehouses = (await _service.GetListAsync(new GetInventoryListInput { MaxResultCount = 100 })).Items;
    }
}
