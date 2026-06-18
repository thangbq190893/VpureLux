using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Inventory;
using VPureLux.Permissions;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Inventory;

[Authorize(VPureLuxPermissions.Inventory.View)]
public class LotsModel : VPureLuxPageModel
{
    private readonly IInventoryQueryAppService _service;
    private readonly IWarehouseAppService _warehouses;
    private readonly IStockItemAppService _stockItems;

    public IReadOnlyList<InventoryLotDto> Items { get; private set; } = [];
    public Dictionary<Guid, string> WarehouseLabels { get; private set; } = new();
    public Dictionary<Guid, string> StockItemLabels { get; private set; } = new();

    public LotsModel(
        IInventoryQueryAppService service,
        IWarehouseAppService warehouses,
        IStockItemAppService stockItems)
    {
        _service = service;
        _warehouses = warehouses;
        _stockItems = stockItems;
    }

    public async Task OnGetAsync()
    {
        Items = await _service.GetLotsAsync();
        await LoadLabelsAsync();
    }

    public string GetWarehouseLabel(Guid id) =>
        WarehouseLabels.TryGetValue(id, out var label) ? label : L["Inventory:UnknownWarehouse"];

    public string GetStockItemLabel(Guid id) =>
        StockItemLabels.TryGetValue(id, out var label) ? label : L["Inventory:UnknownStockItem"];

    private async Task LoadLabelsAsync()
    {
        WarehouseLabels = (await _warehouses.GetListAsync(new GetInventoryListInput
            {
                MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount
            })).Items
            .ToDictionary(x => x.Id, x => $"{x.Code} - {x.Name}");

        StockItemLabels = (await _stockItems.GetListAsync(new GetInventoryListInput
            {
                MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount
            })).Items
            .ToDictionary(x => x.Id, x => $"{x.CodeSnapshot} - {x.NameSnapshot}");
    }
}
