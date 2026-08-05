using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Customers;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.Customers;

[Authorize(VPureLuxPermissions.Customers.View)]
public class IndexModel : VPureLuxPageModel
{
    private const string DefaultSorting = "CreationTime DESC";

    private readonly ICustomerAppService _customerAppService;
    private readonly IAuthorizationService _authorizationService;

    [BindProperty(SupportsGet = true)] public string? SearchText { get; set; }
    [BindProperty(SupportsGet = true)] public CustomerStatus? Status { get; set; }
    public bool CanCreate { get; private set; }
    public bool CanEdit { get; private set; }
    public bool CanManageStatus { get; private set; }
    [TempData] public string? StatusMessageKey { get; set; }

    public IndexModel(ICustomerAppService customerAppService, IAuthorizationService authorizationService)
    {
        _customerAppService = customerAppService;
        _authorizationService = authorizationService;
    }

    public async Task OnGetAsync()
    {
        await SetPermissionsAsync();
    }

    public async Task<JsonResult> OnGetListAsync(GetCustomerListInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Sorting))
        {
            input.Sorting = DefaultSorting;
        }

        return new JsonResult(await _customerAppService.GetListAsync(new GetCustomerListInput
        {
            SearchText = input.SearchText,
            CustomerGroupId = input.CustomerGroupId,
            Status = input.Status,
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        }));
    }

    private async Task SetPermissionsAsync()
    {
        CanCreate = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Customers.Create)).Succeeded;
        CanEdit = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Customers.Edit)).Succeeded;
        CanManageStatus = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Customers.ManageStatus)).Succeeded;
    }

    public async Task<IActionResult> OnPostActivateAsync(Guid id)
    {
        await _customerAppService.ActivateAsync(id);
        StatusMessageKey = "Customers:ActivatedSuccessfully";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id)
    {
        await _customerAppService.DeactivateAsync(id);
        StatusMessageKey = "Customers:DeactivatedSuccessfully";
        return RedirectToPage();
    }
}
