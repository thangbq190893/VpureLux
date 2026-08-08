using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;
using VPureLux.Suppliers;

namespace VPureLux.Web.Pages.Suppliers;

[Authorize(VPureLuxPermissions.Suppliers.View)]
public class IndexModel : VPureLuxPageModel
{
    private const string DefaultSorting = "CreationTime DESC";

    private readonly ISupplierAppService _appService;
    private readonly IAuthorizationService _authorizationService;

    [BindProperty(SupportsGet = true)]
    public string? SearchText { get; set; }

    public bool CanCreate { get; private set; }
    public bool CanEdit { get; private set; }
    public bool CanDelete { get; private set; }

    [TempData]
    public string? StatusMessageKey { get; set; }

    public IndexModel(ISupplierAppService appService, IAuthorizationService authorizationService)
    {
        _appService = appService;
        _authorizationService = authorizationService;
    }

    public async Task OnGetAsync()
    {
        await SetPermissionsAsync();
    }

    public async Task<JsonResult> OnGetListAsync(GetSupplierListInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Sorting))
        {
            input.Sorting = DefaultSorting;
        }

        return new JsonResult(await _appService.GetListAsync(new GetSupplierListInput
        {
            SearchText = input.SearchText,
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        }));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _appService.DeleteAsync(id);
        return new JsonResult(new { success = true });
    }

    private async Task SetPermissionsAsync()
    {
        CanCreate = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Suppliers.Create)).Succeeded;
        CanEdit = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Suppliers.Edit)).Succeeded;
        CanDelete = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Suppliers.Delete)).Succeeded;
    }
}
