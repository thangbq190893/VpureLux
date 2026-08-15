using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.OperatingCosts;
using VPureLux.Permissions;
using Volo.Abp;

namespace VPureLux.Web.Pages.OperatingCosts;

[Authorize(VPureLuxPermissions.OperatingCosts.ManageCategories)]
public class EditCategoryModel : VPureLuxPageModel
{
    private readonly IOperatingCostAppService _appService;

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public UpdateOperatingCostCategoryDto Input { get; set; } = new();

    public EditCategoryModel(IOperatingCostAppService appService)
    {
        _appService = appService;
    }

    public async Task OnGetAsync()
    {
        var category = await _appService.GetCategoryAsync(Id);
        Input = new UpdateOperatingCostCategoryDto
        {
            Code = category.Code,
            Name = category.Name,
            IsActive = category.IsActive
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await _appService.UpdateCategoryAsync(Id, Input);
        }
        catch (BusinessException exception)
        {
            AddBusinessError(exception);
            return Page();
        }

        TempData["StatusMessageKey"] = "OperatingCosts:CategoryUpdatedSuccessfully";
        return RedirectToPage("/OperatingCosts/Categories");
    }
}
