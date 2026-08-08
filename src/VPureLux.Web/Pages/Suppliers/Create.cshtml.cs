using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;
using VPureLux.Suppliers;
using Volo.Abp;

namespace VPureLux.Web.Pages.Suppliers;

[Authorize(VPureLuxPermissions.Suppliers.Create)]
public class CreateModel : VPureLuxPageModel
{
    private readonly ISupplierAppService _appService;

    [BindProperty]
    public CreateSupplierDto Input { get; set; } = new();

    public CreateModel(ISupplierAppService appService)
    {
        _appService = appService;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await _appService.CreateAsync(Input);
        }
        catch (BusinessException exception)
        {
            AddBusinessError(exception);
            return Page();
        }

        TempData["StatusMessageKey"] = "Suppliers:CreatedSuccessfully";
        return RedirectToPage("/Suppliers/Index");
    }
}
