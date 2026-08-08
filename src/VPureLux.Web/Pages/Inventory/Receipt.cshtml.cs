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

    [BindProperty] public List<string> ReceivedAtTexts { get; set; } = new();
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
        ParseReceiptDates();
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
            Input.Lines.Add(new ReceiptLineInput { ReceivedAt = Clock.Now.Date });
        }

        if (string.IsNullOrWhiteSpace(Input.IdempotencyKey))
        {
            Input.IdempotencyKey = Guid.NewGuid().ToString("N");
        }

        while (ReceivedAtTexts.Count < Input.Lines.Count)
        {
            var line = Input.Lines[ReceivedAtTexts.Count];
            var date = line.ReceivedAt == default ? Clock.Now.Date : line.ReceivedAt;
            ReceivedAtTexts.Add(InventoryPostingUi.FormatDate(date));
        }
    }

    private void SyncReceiptDateTextFromInput()
    {
        ReceivedAtTexts = Input.Lines
            .Select(x => InventoryPostingUi.FormatDate(x.ReceivedAt == default ? Clock.Now.Date : x.ReceivedAt))
            .ToList();
    }

    private void ParseReceiptDates()
    {
        for (var i = 0; i < Input.Lines.Count; i++)
        {
            var text = i < ReceivedAtTexts.Count ? ReceivedAtTexts[i] : null;
            if (!InventoryPostingUi.TryParseDate(text, out var receivedAt))
            {
                ModelState.AddModelError($"{nameof(ReceivedAtTexts)}[{i}]", L["Inventory:InvalidDateFormat"]);
                continue;
            }

            Input.Lines[i].ReceivedAt = receivedAt;
        }
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
