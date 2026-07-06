using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VPureLux.Inventory;
using VPureLux.Permissions;
using Volo.Abp;

namespace VPureLux.Web.Pages.Inventory;

[Authorize(VPureLuxPermissions.Inventory.Adjust)]
public class AdjustmentModel : VPureLuxPageModel
{
    private readonly IInventoryTransactionAppService _service;
    private readonly IInventoryQueryAppService _queryService;
    private readonly IWarehouseAppService _warehouseAppService;
    private readonly IStockItemAppService _stockItemAppService;

    [BindProperty] public Guid WarehouseId { get; set; }
    [BindProperty] public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString("N");
    [BindProperty] public string Reason { get; set; } = string.Empty;
    [BindProperty] public List<CountAdjustmentLineInput> CountLines { get; set; } = [new CountAdjustmentLineInput()];
    public List<SelectListItem> WarehouseOptions { get; private set; } = new();
    public List<SelectListItem> StockItemOptions { get; private set; } = new();
    public IReadOnlyList<BalanceQuantityView> BalanceQuantities { get; private set; } = [];
    public string BalanceQuantitiesJson => JsonSerializer.Serialize(BalanceQuantities);
    public string DefaultDateText => InventoryPostingUi.FormatDate(Clock.Now.Date);

    public AdjustmentModel(
        IInventoryTransactionAppService service,
        IInventoryQueryAppService queryService,
        IWarehouseAppService warehouseAppService,
        IStockItemAppService stockItemAppService)
    {
        _service = service;
        _queryService = queryService;
        _warehouseAppService = warehouseAppService;
        _stockItemAppService = stockItemAppService;
    }

    public async Task OnGetAsync()
    {
        EnsureCountLines();
        await LoadOptionsAsync();
        await LoadBalanceQuantitiesAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        EnsureCountLines();
        await LoadBalanceQuantitiesAsync();
        var postInputs = await BuildAdjustmentInputsAsync();

        if (!ModelState.IsValid || postInputs.Count == 0)
        {
            await LoadOptionsAsync();
            return Page();
        }

        try
        {
            foreach (var input in postInputs)
            {
                await _service.PostAdjustmentAsync(input);
            }
        }
        catch (BusinessException exception)
        {
            AddBusinessError(exception);
            await LoadOptionsAsync();
            return Page();
        }

        TempData["InventoryPostSuccessMessage"] = "Inventory:AdjustmentPostedSuccessfully";
        return RedirectToPage();
    }

    private async Task LoadOptionsAsync()
    {
        (WarehouseOptions, StockItemOptions) = await InventoryPostingUi.LoadSelectorOptionsAsync(
            _warehouseAppService,
            _stockItemAppService);
    }

    private async Task LoadBalanceQuantitiesAsync()
    {
        BalanceQuantities = (await _queryService.GetBalancesAsync())
            .Select(x => new BalanceQuantityView(
                x.WarehouseId,
                x.StockItemId,
                x.QuantityOnHand.ToString("0.####", CultureInfo.InvariantCulture)))
            .ToList();
    }

    private async Task<Dictionary<Guid, decimal>> LoadCurrentQuantityMapAsync()
    {
        if (WarehouseId == Guid.Empty)
        {
            return new Dictionary<Guid, decimal>();
        }

        return (await _queryService.GetBalancesAsync(WarehouseId))
            .ToDictionary(x => x.StockItemId, x => x.QuantityOnHand);
    }

    private void EnsureCountLines()
    {
        if (string.IsNullOrWhiteSpace(IdempotencyKey))
        {
            IdempotencyKey = Guid.NewGuid().ToString("N");
        }

        if (CountLines.Count == 0)
        {
            CountLines.Add(new CountAdjustmentLineInput());
        }

        foreach (var line in CountLines)
        {
            if (string.IsNullOrWhiteSpace(line.ReceivedAtText))
            {
                line.ReceivedAtText = DefaultDateText;
            }
        }
    }

    private async Task<List<PostAdjustmentDto>> BuildAdjustmentInputsAsync()
    {
        var result = new List<PostAdjustmentDto>();
        var increaseLines = new List<ReceiptLineInput>();
        var decreaseLines = new List<IssueLineInput>();

        if (WarehouseId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(WarehouseId), L["Inventory:WarehouseRequired"]);
        }

        if (string.IsNullOrWhiteSpace(Reason))
        {
            ModelState.AddModelError(nameof(Reason), L["Inventory:AdjustmentReasonRequired"]);
        }

        var currentQuantities = await LoadCurrentQuantityMapAsync();

        for (var i = 0; i < CountLines.Count; i++)
        {
            var line = CountLines[i];

            if (line.StockItemId == Guid.Empty)
            {
                ModelState.AddModelError($"{nameof(CountLines)}[{i}].{nameof(CountAdjustmentLineInput.StockItemId)}", L["Inventory:StockItemRequired"]);
                continue;
            }

            if (!line.CountedQuantity.HasValue)
            {
                ModelState.AddModelError($"{nameof(CountLines)}[{i}].{nameof(CountAdjustmentLineInput.CountedQuantity)}", L["Inventory:CountedQuantityRequired"]);
                continue;
            }

            if (line.CountedQuantity.Value < 0)
            {
                ModelState.AddModelError($"{nameof(CountLines)}[{i}].{nameof(CountAdjustmentLineInput.CountedQuantity)}", L["Inventory:CountedQuantityNonNegative"]);
                continue;
            }

            var currentQuantity = currentQuantities.GetValueOrDefault(line.StockItemId);
            var delta = decimal.Round(line.CountedQuantity.Value - currentQuantity, InventoryConsts.QuantityScale, MidpointRounding.AwayFromZero);
            line.CurrentQuantity = currentQuantity;
            line.Delta = delta;

            if (delta > 0)
            {
                if (string.IsNullOrWhiteSpace(line.LotNo))
                {
                    ModelState.AddModelError($"{nameof(CountLines)}[{i}].{nameof(CountAdjustmentLineInput.LotNo)}", L["Inventory:PositiveDeltaLotRequired"]);
                }

                if (!InventoryPostingUi.TryParseDate(line.ReceivedAtText, out var receivedAt))
                {
                    ModelState.AddModelError($"{nameof(CountLines)}[{i}].{nameof(CountAdjustmentLineInput.ReceivedAtText)}", L["Inventory:InvalidDateFormat"]);
                }

                if (!line.UnitCost.HasValue || line.UnitCost.Value <= 0)
                {
                    ModelState.AddModelError($"{nameof(CountLines)}[{i}].{nameof(CountAdjustmentLineInput.UnitCost)}", L["Inventory:PositiveDeltaUnitCostRequired"]);
                }

                if (ModelState.IsValid)
                {
                    increaseLines.Add(new ReceiptLineInput
                    {
                        StockItemId = line.StockItemId,
                        Quantity = delta,
                        LotNo = line.LotNo.Trim(),
                        ReceivedAt = receivedAt,
                        UnitCost = line.UnitCost!.Value
                    });
                }
            }
            else if (delta < 0)
            {
                decreaseLines.Add(new IssueLineInput
                {
                    StockItemId = line.StockItemId,
                    Quantity = Math.Abs(delta)
                });
            }
        }

        if (!increaseLines.Any() && !decreaseLines.Any() && ModelState.IsValid)
        {
            ModelState.AddModelError(string.Empty, L["Inventory:AdjustmentAllRowsZeroDelta"]);
            return result;
        }

        if (increaseLines.Any() && decreaseLines.Any() && ModelState.IsValid)
        {
            ModelState.AddModelError(string.Empty, L["Inventory:AdjustmentMixedDirectionsNotAtomic"]);
            return result;
        }

        if (!ModelState.IsValid)
        {
            return result;
        }

        if (increaseLines.Any())
        {
            result.Add(new PostAdjustmentDto
            {
                WarehouseId = WarehouseId,
                IdempotencyKey = IdempotencyKey,
                Type = InventoryTransactionType.AdjustmentIncrease,
                Reason = Reason,
                IncreaseLines = increaseLines
            });
        }

        if (decreaseLines.Any())
        {
            result.Add(new PostAdjustmentDto
            {
                WarehouseId = WarehouseId,
                IdempotencyKey = IdempotencyKey,
                Type = InventoryTransactionType.AdjustmentDecrease,
                Reason = Reason,
                DecreaseLines = decreaseLines
            });
        }

        return result;
    }

    public class CountAdjustmentLineInput
    {
        public Guid StockItemId { get; set; }
        public decimal CurrentQuantity { get; set; }
        public decimal? CountedQuantity { get; set; }
        public decimal Delta { get; set; }
        public string LotNo { get; set; } = string.Empty;
        public string ReceivedAtText { get; set; } = string.Empty;
        public decimal? UnitCost { get; set; }
    }

    public sealed record BalanceQuantityView(Guid WarehouseId, Guid StockItemId, string Quantity);
}
