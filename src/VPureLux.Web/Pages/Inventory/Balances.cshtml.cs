using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VPureLux.Inventory;
using VPureLux.Permissions;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Inventory;

[Authorize(VPureLuxPermissions.Inventory.View)]
public class BalancesModel : VPureLuxPageModel
{
    private static readonly System.Globalization.CultureInfo Vi = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");

    private readonly IInventoryQueryAppService _service;
    private readonly IWarehouseAppService _warehouses;
    private readonly IStockItemAppService _stockItems;

    [BindProperty(SupportsGet = true)] public Guid? WarehouseId { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? StockItemId { get; set; }

    public List<SelectListItem> WarehouseOptions { get; private set; } = new();
    public List<SelectListItem> StockItemOptions { get; private set; } = new();
    public Dictionary<Guid, string> WarehouseLabels { get; private set; } = new();
    public Dictionary<Guid, string> StockItemLabels { get; private set; } = new();

    public BalancesModel(
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
        await LoadFilterOptionsAsync();
        await LoadLabelsAsync();
    }

    public async Task<JsonResult> OnGetListAsync(InventoryInquiryListInput input)
    {
        var items = await _service.GetBalancesAsync(input.WarehouseId, input.StockItemId);
        await LoadLabelsAsync();
        var rows = items.Select(ToRow).ToList();

        return new JsonResult(new PagedResultDto<InventoryBalanceRow>(
            rows.Count,
            rows.Skip(input.SkipCount).Take(input.MaxResultCount).ToList()));
    }

    private InventoryBalanceRow ToRow(InventoryBalanceDto item) => new(
        GetWarehouseLabel(item.WarehouseId),
        GetStockItemLabel(item.StockItemId),
        FormatQuantity(item.QuantityOnHand),
        FormatMoney(item.InventoryValue));

    private string GetWarehouseLabel(Guid id) =>
        WarehouseLabels.TryGetValue(id, out var label) ? label : L["Inventory:UnknownWarehouse"];

    private string GetStockItemLabel(Guid id) =>
        StockItemLabels.TryGetValue(id, out var label) ? label : L["Inventory:UnknownStockItem"];

    private static string FormatMoney(decimal value)
    {
        var amount = decimal.Round(value, 0, MidpointRounding.AwayFromZero);
        return amount.ToString("#,0", Vi) + " ₫";
    }

    private static string FormatQuantity(decimal value)
    {
        if (value == decimal.Truncate(value))
        {
            return decimal.Truncate(value).ToString("0", Vi);
        }

        return value.ToString("0.####", Vi);
    }

    private async Task LoadFilterOptionsAsync()
    {
        WarehouseOptions = (await _warehouses.GetListAsync(new GetInventoryListInput
            {
                Status = InventoryEntityStatus.Active,
                MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount
            })).Items
            .OrderBy(x => x.Code)
            .Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString()))
            .ToList();

        StockItemOptions = (await _stockItems.GetListAsync(new GetInventoryListInput
            {
                Status = InventoryEntityStatus.Active,
                MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount
            })).Items
            .Where(x => x.ItemType == StockItemType.Component && x.IsInventoryEnabled)
            .OrderBy(x => x.CodeSnapshot)
            .Select(x => new SelectListItem($"{x.CodeSnapshot} - {x.NameSnapshot}", x.Id.ToString()))
            .ToList();
    }

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

    public class InventoryInquiryListInput : PagedResultRequestDto
    {
        public Guid? WarehouseId { get; set; }
        public Guid? StockItemId { get; set; }
    }

    public sealed record InventoryBalanceRow(
        string Warehouse,
        string StockItem,
        string QuantityOnHand,
        string InventoryValue);
}
