using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.OperatingCosts;
using VPureLux.Permissions;
using Volo.Abp;

namespace VPureLux.Web.Pages.OperatingCosts;

[Authorize(VPureLuxPermissions.OperatingCosts.ManageCategories)]
public class CreateCategoryModel : VPureLuxPageModel
{
    private readonly IOperatingCostAppService _appService;

    [BindProperty]
    public CreateOperatingCostCategoryDto Input { get; set; } = new();

    public CreateCategoryModel(IOperatingCostAppService appService)
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
            await _appService.CreateCategoryAsync(Input);
        }
        catch (BusinessException exception)
        {
            AddBusinessError(exception);
            return Page();
        }

        TempData["StatusMessageKey"] = "OperatingCosts:CategoryCreatedSuccessfully";
        return RedirectToPage("/OperatingCosts/Categories");
    }
}
