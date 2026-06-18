using System;
using System.Collections.Generic;
using System.Linq;
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
    private readonly IWarehouseAppService _warehouseAppService;
    private readonly IStockItemAppService _stockItemAppService;

    [BindProperty] public Guid WarehouseId { get; set; }
    [BindProperty] public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString("N");
    [BindProperty] public InventoryTransactionType Type { get; set; } = InventoryTransactionType.AdjustmentDecrease;
    [BindProperty] public string Reason { get; set; } = string.Empty;
    [BindProperty] public List<ReceiptLineInput> IncreaseLines { get; set; } = [new ReceiptLineInput()];
    [BindProperty] public List<IssueLineInput> DecreaseLines { get; set; } = [new IssueLineInput()];
    [BindProperty] public List<string> IncreaseReceivedAtTexts { get; set; } = new();
    public List<SelectListItem> WarehouseOptions { get; private set; } = new();
    public List<SelectListItem> StockItemOptions { get; private set; } = new();
    public string DefaultDateText => InventoryPostingUi.FormatDate(Clock.Now.Date);

    public AdjustmentModel(
        IInventoryTransactionAppService service,
        IWarehouseAppService warehouseAppService,
        IStockItemAppService stockItemAppService)
    {
        _service = service;
        _warehouseAppService = warehouseAppService;
        _stockItemAppService = stockItemAppService;
    }

    public async Task OnGetAsync()
    {
        EnsureAdjustmentLines();
        SyncIncreaseDateTextFromInput();
        await LoadOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        EnsureAdjustmentLines();
        RemoveUnusedAdjustmentModelState();
        ParseIncreaseDatesIfNeeded();
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        var input = new PostAdjustmentDto
        {
            WarehouseId = WarehouseId,
            IdempotencyKey = IdempotencyKey,
            Type = Type,
            Reason = Reason
        };
        if (Type == InventoryTransactionType.AdjustmentIncrease)
        {
            input.IncreaseLines.AddRange(IncreaseLines);
        }
        else
        {
            input.DecreaseLines.AddRange(DecreaseLines);
        }

        try
        {
            await _service.PostAdjustmentAsync(input);
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

    private void EnsureAdjustmentLines()
    {
        if (string.IsNullOrWhiteSpace(IdempotencyKey))
        {
            IdempotencyKey = Guid.NewGuid().ToString("N");
        }

        if (IncreaseLines.Count == 0)
        {
            IncreaseLines.Add(new ReceiptLineInput { ReceivedAt = Clock.Now.Date });
        }

        if (DecreaseLines.Count == 0)
        {
            DecreaseLines.Add(new IssueLineInput());
        }

        while (IncreaseReceivedAtTexts.Count < IncreaseLines.Count)
        {
            var line = IncreaseLines[IncreaseReceivedAtTexts.Count];
            var date = line.ReceivedAt == default ? Clock.Now.Date : line.ReceivedAt;
            IncreaseReceivedAtTexts.Add(InventoryPostingUi.FormatDate(date));
        }
    }

    private void SyncIncreaseDateTextFromInput()
    {
        IncreaseReceivedAtTexts = IncreaseLines
            .Select(x => InventoryPostingUi.FormatDate(x.ReceivedAt == default ? Clock.Now.Date : x.ReceivedAt))
            .ToList();
    }

    private void RemoveUnusedAdjustmentModelState()
    {
        var unusedPrefixes = Type == InventoryTransactionType.AdjustmentIncrease
            ? [nameof(DecreaseLines)]
            : new[] { nameof(IncreaseLines), nameof(IncreaseReceivedAtTexts) };

        foreach (var key in ModelState.Keys.Where(key => unusedPrefixes.Any(key.StartsWith)).ToList())
        {
            ModelState.Remove(key);
        }
    }

    private void ParseIncreaseDatesIfNeeded()
    {
        if (Type != InventoryTransactionType.AdjustmentIncrease)
        {
            return;
        }

        for (var i = 0; i < IncreaseLines.Count; i++)
        {
            var text = i < IncreaseReceivedAtTexts.Count ? IncreaseReceivedAtTexts[i] : null;
            if (!InventoryPostingUi.TryParseDate(text, out var receivedAt))
            {
                ModelState.AddModelError($"{nameof(IncreaseReceivedAtTexts)}[{i}]", L["Inventory:InvalidDateFormat"]);
                continue;
            }

            IncreaseLines[i].ReceivedAt = receivedAt;
        }
    }
}
