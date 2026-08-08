using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VPureLux.Inventory;
using VPureLux.Permissions;
using VPureLux.Suppliers;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Inventory;

[Authorize(VPureLuxPermissions.Inventory.Receive)]
public class ReceiptModel : VPureLuxPageModel
{
    private readonly IInventoryTransactionAppService _service;
    private readonly IWarehouseAppService _warehouseAppService;
    private readonly IStockItemAppService _stockItemAppService;
    private readonly ISupplierRepository _supplierRepository;

    [BindProperty]
    public PostReceiptDto Input { get; set; } = new()
    {
        IdempotencyKey = Guid.NewGuid().ToString("N"),
        Lines = [new ReceiptLineInput()]
    };

    [BindProperty] public string ReceivedAtText { get; set; } = string.Empty;
    [BindProperty] public List<string> UnitCostTexts { get; set; } = new();
    public List<SelectListItem> WarehouseOptions { get; private set; } = new();
    public List<SelectListItem> StockItemOptions { get; private set; } = new();
    public List<SelectListItem> SupplierOptions { get; private set; } = new();
    public string DefaultDateText => InventoryPostingUi.FormatDate(Clock.Now.Date);

    public ReceiptModel(
        IInventoryTransactionAppService service,
        IWarehouseAppService warehouseAppService,
        IStockItemAppService stockItemAppService,
        ISupplierRepository supplierRepository)
    {
        _service = service;
        _warehouseAppService = warehouseAppService;
        _stockItemAppService = stockItemAppService;
        _supplierRepository = supplierRepository;
    }

    public async Task OnGetAsync()
    {
        EnsureReceiptLine();
        SyncReceiptDateTextFromInput();
        await LoadOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        EnsureReceiptLine();
        ParseReceiptDate();
        ParseUnitCosts();
        ValidateReceiptLines();
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        try
        {
            await _service.PostReceiptAsync(Input);
        }
        catch (BusinessException exception)
        {
            AddBusinessError(exception);
            await LoadOptionsAsync();
            return Page();
        }

        TempData["InventoryPostSuccessMessage"] = "Inventory:ReceiptPostedSuccessfully";
        return RedirectToPage();
    }

    public string GetPostedFieldValue(string key, decimal value)
    {
        if (ModelState.TryGetValue(key, out var state) &&
            state.RawValue is string attemptedValue)
        {
            return attemptedValue;
        }

        return value == 0
            ? string.Empty
            : value.ToString("0.####", CultureInfo.InvariantCulture);
    }

    public string GetPostedMoneyFieldValue(string key, decimal value)
    {
        if (ModelState.TryGetValue(key, out var state) &&
            state.RawValue is string attemptedValue)
        {
            return attemptedValue;
        }

        return value == 0
            ? string.Empty
            : value.ToString("#,0.##", CultureInfo.GetCultureInfo("vi-VN"));
    }

    private async Task LoadOptionsAsync()
    {
        (WarehouseOptions, StockItemOptions) = await InventoryPostingUi.LoadSelectorOptionsAsync(
            _warehouseAppService,
            _stockItemAppService);

        SupplierOptions = (await _supplierRepository.GetListAsync(
                maxResultCount: LimitedResultRequestDto.MaxMaxResultCount,
                sorting: "Code ASC"))
            .Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString()))
            .ToList();
    }

    private void EnsureReceiptLine()
    {
        if (Input.Lines.Count == 0)
        {
            Input.Lines.Add(new ReceiptLineInput());
        }

        if (Input.ReceivedAt == default)
        {
            Input.ReceivedAt = Clock.Now.Date;
        }

        if (string.IsNullOrWhiteSpace(ReceivedAtText))
        {
            ReceivedAtText = InventoryPostingUi.FormatDate(Input.ReceivedAt);
        }

        if (string.IsNullOrWhiteSpace(Input.IdempotencyKey))
        {
            Input.IdempotencyKey = Guid.NewGuid().ToString("N");
        }

        while (UnitCostTexts.Count < Input.Lines.Count)
        {
            var line = Input.Lines[UnitCostTexts.Count];
            UnitCostTexts.Add(line.UnitCost == 0 ? string.Empty : line.UnitCost.ToString("#,0.##", CultureInfo.GetCultureInfo("vi-VN")));
        }

        if (UnitCostTexts.Count > Input.Lines.Count)
        {
            UnitCostTexts = UnitCostTexts.Take(Input.Lines.Count).ToList();
        }
    }

    private void SyncReceiptDateTextFromInput()
    {
        ReceivedAtText = InventoryPostingUi.FormatDate(Input.ReceivedAt == default ? Clock.Now.Date : Input.ReceivedAt);
    }

    private void ParseReceiptDate()
    {
        if (!InventoryPostingUi.TryParseDate(ReceivedAtText, out var receivedAt))
        {
            ModelState.AddModelError(nameof(ReceivedAtText), L["Inventory:InvalidDateFormat"]);
            return;
        }

        Input.ReceivedAt = receivedAt;
    }

    private void ParseUnitCosts()
    {
        for (var i = 0; i < Input.Lines.Count; i++)
        {
            var key = $"{nameof(UnitCostTexts)}[{i}]";
            var dtoKey = $"{nameof(Input)}.{nameof(Input.Lines)}[{i}].{nameof(ReceiptLineInput.UnitCost)}";
            var text = i < UnitCostTexts.Count ? UnitCostTexts[i] : null;
            ModelState.Remove(dtoKey);

            if (!TryParseVndAmount(text, out var unitCost) || unitCost <= 0)
            {
                ModelState.AddModelError(key, L["Inventory:UnitCostPositive"]);
                continue;
            }

            Input.Lines[i].UnitCost = unitCost;
        }
    }

    private static bool TryParseVndAmount(string? text, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Trim()
            .Replace("₫", string.Empty)
            .Replace("VNĐ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("VND", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty);

        if (normalized.Contains('.') && normalized.Contains(','))
        {
            normalized = normalized.LastIndexOf(',') > normalized.LastIndexOf('.')
                ? normalized.Replace(".", string.Empty).Replace(",", ".")
                : normalized.Replace(",", string.Empty);
        }
        else if (normalized.Contains('.'))
        {
            normalized = LooksLikeThousandGroups(normalized, '.') ? normalized.Replace(".", string.Empty) : normalized;
        }
        else if (normalized.Contains(','))
        {
            normalized = LooksLikeThousandGroups(normalized, ',')
                ? normalized.Replace(",", string.Empty)
                : normalized.Replace(",", ".");
        }

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private static bool LooksLikeThousandGroups(string value, char separator)
    {
        var parts = value.Split(separator);
        return parts.Length > 1 &&
               parts[0].Length is >= 1 and <= 3 &&
               parts.Skip(1).All(x => x.Length == 3 && x.All(char.IsDigit));
    }

    private void ValidateReceiptLines()
    {
        for (var i = 0; i < Input.Lines.Count; i++)
        {
            var quantityKey = $"{nameof(Input)}.{nameof(Input.Lines)}[{i}].{nameof(ReceiptLineInput.Quantity)}";
            if (Input.Lines[i].Quantity > 0)
            {
                continue;
            }

            ModelState.Remove(quantityKey);
            ModelState.AddModelError(quantityKey, L["Inventory:ReceiptQuantityPositive"]);
        }
    }
}
