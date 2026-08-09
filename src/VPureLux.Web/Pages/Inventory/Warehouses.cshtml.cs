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
    [BindProperty] public EditWarehouseInput EditWarehouse { get; set; } = new();
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

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        ModelState.Remove($"{nameof(NewWarehouse)}.{nameof(CreateWarehouseDto.Code)}");
        ModelState.Remove($"{nameof(NewWarehouse)}.{nameof(CreateWarehouseDto.Name)}");
        ModelState.Remove($"{nameof(NewWarehouse)}.{nameof(CreateWarehouseDto.Address)}");

        if (EditWarehouse.Id == Guid.Empty)
        {
            ModelState.AddModelError(nameof(EditWarehouse.Id), L["Inventory:UnknownWarehouse"]);
        }

        if (EditWarehouse.Name.IsNullOrWhiteSpace())
        {
            ModelState.AddModelError(nameof(EditWarehouse.Name), L["Inventory:WarehouseNameRequired"]);
        }

        if (!ModelState.IsValid)
        {
            await LoadWarehousesAsync();
            return Page();
        }

        try
        {
            await _service.UpdateAsync(EditWarehouse.Id, new UpdateWarehouseDto
            {
                Name = EditWarehouse.Name,
                Address = EditWarehouse.Address
            });
        }
        catch (BusinessException exception)
        {
            AddBusinessError(exception);
            await LoadWarehousesAsync();
            return Page();
        }

        StatusMessageKey = "Inventory:WarehouseUpdatedSuccessfully";
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
        Warehouses = (await _service.GetListAsync(new GetInventoryListInput { MaxResultCount = Volo.Abp.Application.Dtos.LimitedResultRequestDto.MaxMaxResultCount })).Items;
    }

    public class EditWarehouseInput
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
    }
}
