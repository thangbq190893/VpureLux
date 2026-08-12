using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.OperatingCosts;
using VPureLux.Permissions;
using Volo.Abp;

namespace VPureLux.Web.Pages.OperatingCosts;

[Authorize(VPureLuxPermissions.OperatingCosts.ManageEntries)]
public class CreateModel : VPureLuxPageModel
{
    private readonly IOperatingCostAppService _appService;

    [BindProperty]
    public CreateOperatingCostEntryDto Input { get; set; } = new()
    {
        EntryDate = DateTime.Today,
        PaymentDate = DateTime.Today,
        PaymentStatus = OperatingCostPaymentStatus.Paid
    };

    public EntryFormViewModel Form { get; private set; } = new();

    public CreateModel(IOperatingCostAppService appService)
    {
        _appService = appService;
    }

    public async Task OnGetAsync()
    {
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
            await _appService.CreateEntryAsync(Input);
        }
        catch (BusinessException exception)
        {
            AddBusinessError(exception);
            await LoadFormAsync();
            return Page();
        }

        TempData["StatusMessageKey"] = "OperatingCosts:EntryCreatedSuccessfully";
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
