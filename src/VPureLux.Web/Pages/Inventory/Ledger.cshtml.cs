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
            query = query.Where(x => BuildSourceReferenceSearchText(x).Contains(source, StringComparison.OrdinalIgnoreCase));
        }

        return query;
    }

    private IEnumerable<LedgerTraceRow> BuildTraceRows(IEnumerable<InventoryTransactionDto> transactions)
    {
        foreach (var transaction in transactions)
        {
            var source = BuildSourceReferenceView(transaction);

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

    private SourceReferenceView BuildSourceReferenceView(InventoryTransactionDto transaction)
    {
        var referenceType = transaction.ReferenceType?.Trim();
        var details = new List<string>();
        var label = GetSourceLabel(transaction, referenceType);

        if (!string.IsNullOrWhiteSpace(referenceType))
        {
            if (referenceType.Equals("SalesOrderLine", StringComparison.OrdinalIgnoreCase))
            {
                if (transaction.ReferenceId.HasValue)
                {
                    details.Add($"{L["Inventory:SourceSalesOrderLineId"].Value}: {transaction.ReferenceId.Value:D}");
                }
            }
            else
            {
                details.Add($"{L["Inventory:SourceReferenceType"].Value}: {referenceType}");

                if (transaction.ReferenceId.HasValue)
                {
                    details.Add($"{L["Inventory:SourceReferenceId"].Value}: {transaction.ReferenceId.Value:D}");
                }
            }
        }

        if (transaction.BomVersionId.HasValue)
        {
            details.Add($"{L["Inventory:SourceBomVersion"].Value}: {transaction.BomVersionId.Value:D}");
        }

        return new SourceReferenceView(
            label,
            details.Count == 0 ? null : string.Join(" / ", details),
            transaction.BomVersionId);
    }

    private string GetSourceLabel(InventoryTransactionDto transaction, string? referenceType)
    {
        if (!string.IsNullOrWhiteSpace(referenceType))
        {
            if (referenceType.Equals("SalesOrderLine", StringComparison.OrdinalIgnoreCase))
            {
                return L["Inventory:SourceSalesOrder"].Value;
            }

            return L["Inventory:SourceUnknown"].Value;
        }

        if (transaction.BomVersionId.HasValue || transaction.Type == InventoryTransactionType.AssemblyIssue)
        {
            return L["Inventory:SourceBomManufacturing"].Value;
        }

        if (transaction.Type == InventoryTransactionType.PurchaseReceipt)
        {
            return L["Inventory:SourceManualReceipt"].Value;
        }

        if (transaction.Type == InventoryTransactionType.SalesIssue)
        {
            return L["Inventory:SourceManualIssue"].Value;
        }

        if (transaction.Type is InventoryTransactionType.AdjustmentIncrease or InventoryTransactionType.AdjustmentDecrease)
        {
            return L["Inventory:SourceAdjustment"].Value;
        }

        return L["Inventory:SourceUnknown"].Value;
    }

    private string BuildSourceReferenceSearchText(InventoryTransactionDto transaction)
    {
        var source = BuildSourceReferenceView(transaction);
        var parts = new List<string>
        {
            source.Label,
            transaction.Id.ToString("D")
        };

        if (!string.IsNullOrWhiteSpace(source.Detail))
        {
            parts.Add(source.Detail);
        }

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

        return string.Join(" / ", parts);
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
        SourceReferenceView Source,
        decimal QuantityIn,
        decimal QuantityOut,
        decimal? UnitCost,
        decimal? Amount,
        string? Reason);

    public sealed record SourceReferenceView(
        string Label,
        string? Detail,
        Guid? BomVersionId);
}
