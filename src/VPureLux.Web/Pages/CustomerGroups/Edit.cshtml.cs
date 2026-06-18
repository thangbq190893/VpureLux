using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Customers.CustomerGroups;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.CustomerGroups;

[Authorize(VPureLuxPermissions.CustomerGroups.Edit)]
public class EditModel : VPureLuxPageModel
{
    private readonly ICustomerGroupAppService _appService;
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    public string Code { get; private set; } = string.Empty;
    [BindProperty] public UpdateCustomerGroupDto Input { get; set; } = new();
    public EditModel(ICustomerGroupAppService appService) { _appService = appService; }
    public async Task OnGetAsync() { var group = await _appService.GetAsync(Id); Code = group.Code; Input = new UpdateCustomerGroupDto { Name = group.Name, Description = group.Description, SortOrder = group.SortOrder }; }
    public async Task<IActionResult> OnPostAsync() { await _appService.UpdateAsync(Id, Input); return RedirectToPage("/CustomerGroups/Index"); }
}
