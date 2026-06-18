using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Customers.CustomerGroups;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.CustomerGroups;

[Authorize(VPureLuxPermissions.CustomerGroups.Create)]
public class CreateModel : VPureLuxPageModel
{
    private readonly ICustomerGroupAppService _appService;
    [BindProperty] public CreateCustomerGroupDto Input { get; set; } = new();
    public CreateModel(ICustomerGroupAppService appService) { _appService = appService; }
    public void OnGet() { }
    public async Task<IActionResult> OnPostAsync() { await _appService.CreateAsync(Input); return RedirectToPage("/CustomerGroups/Index"); }
}
