using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VPureLux.Localization;
using VPureLux.OperatingCosts;
using VPureLux.Permissions;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.OperatingCosts;

[Authorize(VPureLuxPermissions.OperatingCosts.View)]
public class CategoriesModel : VPureLuxPageModel
{
    private readonly IOperatingCostAppService _appService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IStringLocalizer<VPureLuxResource> _localizer;

    [BindProperty(SupportsGet = true)] public string? SearchText { get; set; }
    [BindProperty(SupportsGet = true)] public bool? IsActive { get; set; }

    public bool CanManageCategories { get; private set; }

    [TempData] public string? StatusMessageKey { get; set; }

    public CategoriesModel(
        IOperatingCostAppService appService,
        IAuthorizationService authorizationService,
        IStringLocalizer<VPureLuxResource> localizer)
    {
        _appService = appService;
        _authorizationService = authorizationService;
        _localizer = localizer;
    }

    public async Task OnGetAsync()
    {
        CanManageCategories = (await _authorizationService.AuthorizeAsync(
            User,
            VPureLuxPermissions.OperatingCosts.ManageCategories)).Succeeded;
    }

    public async Task<JsonResult> OnGetListAsync(GetOperatingCostCategoryListInput input)
    {
        var result = await _appService.GetCategoryListAsync(input);
        return new JsonResult(new PagedResultDto<OperatingCostCategoryRow>(
            result.TotalCount,
            result.Items.Select(ToRow).ToList()));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _appService.DeleteCategoryAsync(id);
        return new JsonResult(new { success = true });
    }

    private OperatingCostCategoryRow ToRow(OperatingCostCategoryDto category)
    {
        return new OperatingCostCategoryRow(
            category.Id,
            category.Code,
            category.Name,
            category.IsActive ? _localizer["Status:Active"].Value : _localizer["Status:Inactive"].Value,
            category.IsActive ? "text-bg-success" : "text-bg-secondary");
    }

    public sealed record OperatingCostCategoryRow(
        Guid Id,
        string Code,
        string Name,
        string Status,
        string StatusBadgeClass);
}
