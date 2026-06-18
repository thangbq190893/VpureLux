using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Inventory;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.Inventory;

[Authorize(VPureLuxPermissions.Inventory.ManageWarehouses)]
public class WarehousesModel : VPureLuxPageModel
{
    private readonly IWarehouseAppService _service;
    [BindProperty] public CreateWarehouseDto NewWarehouse { get; set; } = new();
    public IReadOnlyList<WarehouseDto> Warehouses { get; private set; } = [];
    public WarehousesModel(IWarehouseAppService service) => _service = service;
    public async Task OnGetAsync() => Warehouses = (await _service.GetListAsync(new GetInventoryListInput { MaxResultCount = 100 })).Items;
    public async Task<IActionResult> OnPostAsync() { await _service.CreateAsync(NewWarehouse); return RedirectToPage(); }
    public async Task<IActionResult> OnPostActivateAsync(System.Guid id) { await _service.ActivateAsync(id); return RedirectToPage(); }
    public async Task<IActionResult> OnPostDeactivateAsync(System.Guid id) { await _service.DeactivateAsync(id); return RedirectToPage(); }
}
