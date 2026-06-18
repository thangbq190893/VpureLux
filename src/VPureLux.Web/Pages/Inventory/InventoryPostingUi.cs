using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using VPureLux.Inventory;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Inventory;

public static class InventoryPostingUi
{
    public const string DateFormat = "dd/MM/yyyy";
    private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");

    public static string FormatDate(DateTime date)
    {
        return date.ToString(DateFormat, VietnameseCulture);
    }

    public static bool TryParseDate(string? value, out DateTime date)
    {
        return DateTime.TryParseExact(
            value,
            DateFormat,
            VietnameseCulture,
            DateTimeStyles.None,
            out date);
    }

    public static async Task<(List<SelectListItem> WarehouseOptions, List<SelectListItem> StockItemOptions)> LoadSelectorOptionsAsync(
        IWarehouseAppService warehouseAppService,
        IStockItemAppService stockItemAppService)
    {
        var warehouses = await warehouseAppService.GetListAsync(new GetInventoryListInput
        {
            Status = InventoryEntityStatus.Active,
            MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount
        });

        var warehouseOptions = warehouses.Items
            .OrderBy(x => x.Code)
            .Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString()))
            .ToList();

        var stockItems = await stockItemAppService.GetListAsync(new GetInventoryListInput
        {
            Status = InventoryEntityStatus.Active,
            MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount
        });

        var stockItemOptions = stockItems.Items
            .Where(x => x.ItemType == StockItemType.Component && x.IsInventoryEnabled)
            .OrderBy(x => x.CodeSnapshot)
            .Select(x => new SelectListItem($"{x.CodeSnapshot} - {x.NameSnapshot}", x.Id.ToString()))
            .ToList();

        return (warehouseOptions, stockItemOptions);
    }

    public static bool IsOptionSelected(string optionValue, Guid selectedId)
    {
        return optionValue == selectedId.ToString();
    }
}
