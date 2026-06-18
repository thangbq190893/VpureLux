using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VPureLux.Inventory;
using VPureLux.Permissions;
using Volo.Abp;

namespace VPureLux.Web.Pages.Inventory;

[Authorize(VPureLuxPermissions.Inventory.Issue)]
public class IssueModel : VPureLuxPageModel
{
    private readonly IInventoryTransactionAppService _service;
    private readonly IWarehouseAppService _warehouseAppService;
    private readonly IStockItemAppService _stockItemAppService;

    [BindProperty]
    public PostIssueDto Input { get; set; } = new()
    {
        IdempotencyKey = Guid.NewGuid().ToString("N"),
        Lines = [new IssueLineInput()]
    };

    public List<SelectListItem> WarehouseOptions { get; private set; } = new();
    public List<SelectListItem> StockItemOptions { get; private set; } = new();

    public IssueModel(
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
        EnsureIssueLine();
        await LoadOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        EnsureIssueLine();
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        try
        {
            await _service.PostIssueAsync(Input);
        }
        catch (BusinessException exception)
        {
            AddBusinessError(exception);
            await LoadOptionsAsync();
            return Page();
        }

        TempData["InventoryPostSuccessMessage"] = "Inventory:IssuePostedSuccessfully";
        return RedirectToPage();
    }

    private async Task LoadOptionsAsync()
    {
        (WarehouseOptions, StockItemOptions) = await InventoryPostingUi.LoadSelectorOptionsAsync(
            _warehouseAppService,
            _stockItemAppService);
    }

    private void EnsureIssueLine()
    {
        if (Input.Lines.Count == 0)
        {
            Input.Lines.Add(new IssueLineInput());
        }

        if (string.IsNullOrWhiteSpace(Input.IdempotencyKey))
        {
            Input.IdempotencyKey = Guid.NewGuid().ToString("N");
        }
    }
}
