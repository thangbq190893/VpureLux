using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Customers.CustomerGroups;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.CustomerGroups;

[Authorize(VPureLuxPermissions.CustomerGroups.View)]
public class DetailsModel : VPureLuxPageModel
{
    private readonly ICustomerGroupAppService _appService;
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    public CustomerGroupDto CustomerGroup { get; private set; } = new();
    public DetailsModel(ICustomerGroupAppService appService) { _appService = appService; }
    public async Task OnGetAsync() { CustomerGroup = await _appService.GetAsync(Id); }
}
