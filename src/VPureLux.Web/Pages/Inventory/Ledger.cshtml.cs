using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Inventory;
using VPureLux.Permissions;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Inventory;

[Authorize(VPureLuxPermissions.Inventory.ViewLedger)]
public class LedgerModel : VPureLuxPageModel
{
    private readonly IInventoryQueryAppService _service;
    private readonly IWarehouseAppService _warehouses;

    public IReadOnlyList<InventoryTransactionDto> Items { get; private set; } = [];
    public Dictionary<Guid, string> WarehouseLabels { get; private set; } = new();

    public LedgerModel(
        IInventoryQueryAppService service,
        IWarehouseAppService warehouses)
    {
        _service = service;
        _warehouses = warehouses;
    }

    public async Task OnGetAsync()
    {
        Items = await _service.GetLedgerAsync();
        await LoadWarehouseLabelsAsync();
    }

    public string GetWarehouseLabel(Guid id) =>
        WarehouseLabels.TryGetValue(id, out var label) ? label : L["Inventory:UnknownWarehouse"];

    private async Task LoadWarehouseLabelsAsync()
    {
        WarehouseLabels = (await _warehouses.GetListAsync(new GetInventoryListInput
            {
                MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount
            })).Items
            .ToDictionary(x => x.Id, x => $"{x.Code} - {x.Name}");
    }
}
