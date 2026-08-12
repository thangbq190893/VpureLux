using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.OperatingCosts;
using VPureLux.Permissions;
using Volo.Abp;

namespace VPureLux.Web.Pages.OperatingCosts;

[Authorize(VPureLuxPermissions.OperatingCosts.ManageEntries)]
public class EditModel : VPureLuxPageModel
{
    private readonly IOperatingCostAppService _appService;

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public UpdateOperatingCostEntryDto Input { get; set; } = new();

    public EntryFormViewModel Form { get; private set; } = new();

    public EditModel(IOperatingCostAppService appService)
    {
        _appService = appService;
    }

    public async Task OnGetAsync()
    {
        var entry = await _appService.GetEntryAsync(Id);
        Input = new UpdateOperatingCostEntryDto
        {
            EntryDate = entry.EntryDate,
            Direction = entry.Direction,
            CategoryId = entry.CategoryId,
            Amount = entry.Amount,
            PaymentStatus = entry.PaymentStatus,
            DueDate = entry.DueDate,
            PaymentDate = entry.PaymentDate,
            Counterparty = entry.Counterparty,
            ReferenceNo = entry.ReferenceNo,
            Description = entry.Description,
            Note = entry.Note
        };
        await LoadFormAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadFormAsync();
            return Page();
        }

        try
        {
            await _appService.UpdateEntryAsync(Id, Input);
        }
        catch (BusinessException exception)
        {
            AddBusinessError(exception);
            await LoadFormAsync();
            return Page();
        }

        TempData["StatusMessageKey"] = "OperatingCosts:EntryUpdatedSuccessfully";
        return RedirectToPage("/OperatingCosts/Index");
    }

    private async Task LoadFormAsync()
    {
        Form = new EntryFormViewModel
        {
            Input = Input,
            Categories = await _appService.GetActiveCategoriesAsync()
        };
    }
}
