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

[Authorize(VPureLuxPermissions.Inventory.ViewLedger)]
public class LedgerModel : VPureLuxPageModel
{
    private readonly IInventoryQueryAppService _service;
    private readonly IWarehouseAppService _warehouses;
    private readonly IStockItemAppService _stockItems;

    [BindProperty(SupportsGet = true)] public Guid? WarehouseId { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? StockItemId { get; set; }
    [BindProperty(SupportsGet = true)] public InventoryTransactionType? Type { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; }
    [BindProperty(SupportsGet = true)] public string? SourceReference { get; set; }

    public IReadOnlyList<InventoryTransactionDto> Items { get; private set; } = [];
    public IReadOnlyList<LedgerTraceRow> Rows { get; private set; } = [];
    public List<SelectListItem> WarehouseOptions { get; private set; } = new();
    public List<SelectListItem> StockItemOptions { get; private set; } = new();
    public IReadOnlyList<InventoryTransactionType> TransactionTypes { get; } = Enum.GetValues<InventoryTransactionType>()
        .OrderBy(x => (byte)x)
        .ToList();
    public Dictionary<Guid, string> WarehouseLabels { get; private set; } = new();
    public Dictionary<Guid, string> StockItemLabels { get; private set; } = new();

    public LedgerModel(
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
        var transactions = await _service.GetLedgerAsync(WarehouseId, StockItemId);
        Items = ApplyTransactionFilters(transactions).ToList();
        await LoadFilterOptionsAsync();
        await LoadWarehouseLabelsAsync();
        await LoadStockItemLabelsAsync();
        Rows = BuildTraceRows(Items).ToList();
    }

    public string GetWarehouseLabel(Guid id) =>
        WarehouseLabels.TryGetValue(id, out var label) ? label : L["Inventory:UnknownWarehouse"];

    public string GetStockItemLabel(Guid id) =>
        StockItemLabels.TryGetValue(id, out var label) ? label : L["Inventory:UnknownStockItem"];

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

    private async Task LoadWarehouseLabelsAsync()
    {
        WarehouseLabels = (await _warehouses.GetListAsync(new GetInventoryListInput
            {
                MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount
            })).Items
            .ToDictionary(x => x.Id, x => $"{x.Code} - {x.Name}");
    }

    private async Task LoadStockItemLabelsAsync()
    {
        StockItemLabels = (await _stockItems.GetListAsync(new GetInventoryListInput
            {
                MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount
            })).Items
            .ToDictionary(x => x.Id, x => $"{x.CodeSnapshot} - {x.NameSnapshot}");
    }

    private IEnumerable<InventoryTransactionDto> ApplyTransactionFilters(IEnumerable<InventoryTransactionDto> transactions)
    {
        var query = transactions;

        if (Type.HasValue)
        {
            query = query.Where(x => x.Type == Type.Value);
        }

        if (FromDate.HasValue)
        {
            var from = FromDate.Value.Date;
            query = query.Where(x => x.PostedAt.HasValue && x.PostedAt.Value.Date >= from);
        }

        if (ToDate.HasValue)
        {
            var to = ToDate.Value.Date;
            query = query.Where(x => x.PostedAt.HasValue && x.PostedAt.Value.Date <= to);
        }

        if (!string.IsNullOrWhiteSpace(SourceReference))
        {
            var source = SourceReference.Trim();
            query = query.Where(x => BuildSourceReference(x).Contains(source, StringComparison.OrdinalIgnoreCase));
        }

        return query;
    }

    private IEnumerable<LedgerTraceRow> BuildTraceRows(IEnumerable<InventoryTransactionDto> transactions)
    {
        foreach (var transaction in transactions)
        {
            var source = BuildSourceReference(transaction);

            foreach (var line in transaction.Lines.Where(line => !StockItemId.HasValue || line.StockItemId == StockItemId))
            {
                var amount = GetLineAmount(line);
                yield return new LedgerTraceRow(
                    transaction.PostedAt,
                    transaction.WarehouseId,
                    line.StockItemId,
                    transaction.Type,
                    source,
                    line.Direction == InventoryMovementDirection.Increase ? line.Quantity : 0,
                    line.Direction == InventoryMovementDirection.Decrease ? line.Quantity : 0,
                    GetLineUnitCost(line),
                    amount,
                    transaction.Reason);
            }
        }
    }

    private static string BuildSourceReference(InventoryTransactionDto transaction)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(transaction.ReferenceType))
        {
            parts.Add(transaction.ReferenceType);
        }

        if (transaction.ReferenceId.HasValue)
        {
            parts.Add(transaction.ReferenceId.Value.ToString("D"));
        }

        if (transaction.BomVersionId.HasValue)
        {
            parts.Add($"BOM {transaction.BomVersionId.Value:D}");
        }

        return parts.Count == 0 ? "-" : string.Join(" / ", parts);
    }

    private static decimal? GetLineUnitCost(InventoryTransactionLineDto line)
    {
        if (line.UnitCost.HasValue)
        {
            return line.UnitCost;
        }

        var totalQuantity = line.Allocations.Sum(x => x.Quantity);
        if (totalQuantity == 0)
        {
            return null;
        }

        return line.Allocations.Sum(x => x.TotalCost) / totalQuantity;
    }

    private static decimal? GetLineAmount(InventoryTransactionLineDto line)
    {
        if (line.Direction == InventoryMovementDirection.Increase && line.UnitCost.HasValue)
        {
            return line.Quantity * line.UnitCost.Value;
        }

        if (line.Direction == InventoryMovementDirection.Decrease && line.Allocations.Count != 0)
        {
            return line.Allocations.Sum(x => x.TotalCost);
        }

        return null;
    }

    public sealed record LedgerTraceRow(
        DateTime? PostedAt,
        Guid WarehouseId,
        Guid StockItemId,
        InventoryTransactionType Type,
        string SourceReference,
        decimal QuantityIn,
        decimal QuantityOut,
        decimal? UnitCost,
        decimal? Amount,
        string? Reason);
}
