using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VPureLux.Inventory;
using VPureLux.Permissions;
using VPureLux.Suppliers;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Inventory;

[Authorize(VPureLuxPermissions.Inventory.View)]
public class LotsModel : VPureLuxPageModel
{
    private static readonly System.Globalization.CultureInfo Vi = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");

    private readonly IInventoryQueryAppService _service;
    private readonly IInventoryLotAppService _lotAppService;
    private readonly IWarehouseAppService _warehouses;
    private readonly IStockItemAppService _stockItems;
    private readonly ISupplierAppService _suppliers;
    private readonly IAuthorizationService _authorizationService;

    [BindProperty(SupportsGet = true)] public Guid? WarehouseId { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? StockItemId { get; set; }
    [BindProperty(SupportsGet = true)] public string? LotNo { get; set; }

    public List<SelectListItem> WarehouseOptions { get; private set; } = new();
    public List<SelectListItem> StockItemOptions { get; private set; } = new();
    public List<SelectListItem> SupplierOptions { get; private set; } = new();
    public Dictionary<Guid, string> WarehouseLabels { get; private set; } = new();
    public Dictionary<Guid, string> StockItemLabels { get; private set; } = new();
    public bool CanUpdateSupplier { get; private set; }
    public bool CanUpdateLotInfo { get; private set; }

    public LotsModel(
        IInventoryQueryAppService service,
        IInventoryLotAppService lotAppService,
        IWarehouseAppService warehouses,
        IStockItemAppService stockItems,
        ISupplierAppService suppliers,
        IAuthorizationService authorizationService)
    {
        _service = service;
        _lotAppService = lotAppService;
        _warehouses = warehouses;
        _stockItems = stockItems;
        _suppliers = suppliers;
        _authorizationService = authorizationService;
    }

    public async Task OnGetAsync()
    {
        CanUpdateLotInfo = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Inventory.Receive)).Succeeded;
        CanUpdateSupplier = CanUpdateLotInfo;
        await LoadFilterOptionsAsync();
        await LoadLabelsAsync();
    }

    public async Task<JsonResult> OnGetListAsync(InventoryInquiryListInput input)
    {
        var items = await _service.GetLotsAsync(input.WarehouseId, input.StockItemId, input.LotNo);
        await LoadLabelsAsync();
        var rows = items.Select(ToRow).ToList();

        return new JsonResult(new PagedResultDto<InventoryLotRow>(
            rows.Count,
            rows.Skip(input.SkipCount).Take(input.MaxResultCount).ToList()));
    }

    private InventoryLotRow ToRow(InventoryLotDto item) => new(
        item.Id,
        item.LotNo,
        GetWarehouseLabel(item.WarehouseId),
        string.IsNullOrWhiteSpace(item.SupplierName)
            ? L["Inventory:NoSupplier"]
            : $"{item.SupplierCode} - {item.SupplierName}",
        item.SupplierId,
        GetStockItemLabel(item.StockItemId),
        InventoryPostingUi.FormatDate(item.ReceivedAt),
        FormatQuantity(item.ReceivedQuantity),
        FormatQuantity(item.AvailableQuantity),
        item.UnitCost,
        FormatMoney(item.UnitCost),
        FormatMoney(item.ReceivedQuantity * item.UnitCost));

    public async Task<IActionResult> OnPostUpdateSupplierAsync(Guid id, Guid supplierId)
    {
        if (!(await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Inventory.Receive)).Succeeded)
        {
            return Forbid();
        }

        await _lotAppService.UpdateSupplierAsync(id, new UpdateInventoryLotSupplierDto
        {
            SupplierId = supplierId
        });

        return new NoContentResult();
    }

    public async Task<IActionResult> OnPostUpdateUnitCostAsync(Guid id, decimal unitCost)
    {
        if (!(await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Inventory.Receive)).Succeeded)
        {
            return Forbid();
        }

        await _lotAppService.UpdateUnitCostAsync(id, new UpdateInventoryLotUnitCostDto
        {
            UnitCost = unitCost
        });

        return new NoContentResult();
    }

    public async Task<IActionResult> OnPostDeleteReceiptAsync(Guid id)
    {
        if (!(await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Inventory.Receive)).Succeeded)
        {
            return Forbid();
        }

        await _lotAppService.DeleteUnusedReceiptAsync(id);

        return new NoContentResult();
    }

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

        SupplierOptions = (await _suppliers.GetListAsync(new GetSupplierListInput
            {
                MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount,
                Sorting = "Code ASC"
            })).Items
            .OrderBy(x => x.Code)
            .Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString()))
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
        public string? LotNo { get; set; }
    }

    public sealed record InventoryLotRow(
        Guid Id,
        string LotNo,
        string Warehouse,
        string Supplier,
        Guid? SupplierId,
        string StockItem,
        string ReceivedAt,
        string ReceivedQuantity,
        string AvailableQuantity,
        decimal UnitCostValue,
        string UnitCost,
        string ReceiptValue);
}
