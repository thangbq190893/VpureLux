using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Customers;
using VPureLux.Customers.CustomerGroups;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.CustomerGroups;

[Authorize(VPureLuxPermissions.CustomerGroups.View)]
public class IndexModel : VPureLuxPageModel
{
    private readonly ICustomerGroupAppService _appService;
    private readonly IAuthorizationService _authorizationService;
    [BindProperty(SupportsGet = true)] public string? SearchText { get; set; }
    [BindProperty(SupportsGet = true)] public CustomerGroupStatus? Status { get; set; }
    public IReadOnlyList<CustomerGroupDto> CustomerGroups { get; private set; } = Array.Empty<CustomerGroupDto>();
    public bool CanCreate { get; private set; }
    public bool CanEdit { get; private set; }
    public bool CanManageStatus { get; private set; }
    [TempData] public string? StatusMessageKey { get; set; }
    public IndexModel(ICustomerGroupAppService appService, IAuthorizationService authorizationService) { _appService = appService; _authorizationService = authorizationService; }
    public async Task OnGetAsync()
    {
        CustomerGroups = (await _appService.GetListAsync(new GetCustomerGroupListInput { SearchText = SearchText, Status = Status, MaxResultCount = 100 })).Items;
        CanCreate = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.CustomerGroups.Create)).Succeeded;
        CanEdit = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.CustomerGroups.Edit)).Succeeded;
        CanManageStatus = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.CustomerGroups.ManageStatus)).Succeeded;
    }
    public async Task<IActionResult> OnPostActivateAsync(Guid id)
    {
        await _appService.ActivateAsync(id);
        StatusMessageKey = "CustomerGroups:ActivatedSuccessfully";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id)
    {
        await _appService.DeactivateAsync(id);
        StatusMessageKey = "CustomerGroups:DeactivatedSuccessfully";
        return RedirectToPage();
    }
}
