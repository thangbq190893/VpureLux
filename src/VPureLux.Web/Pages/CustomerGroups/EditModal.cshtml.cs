using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Customers.CustomerGroups;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.CustomerGroups;

[Authorize(VPureLuxPermissions.CustomerGroups.Edit)]
public class EditModalModel : VPureLuxPageModel
{
    private readonly ICustomerGroupAppService _appService;

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    public string Code { get; private set; } = string.Empty;
    [BindProperty] public UpdateCustomerGroupDto Input { get; set; } = new();

    public EditModalModel(ICustomerGroupAppService appService)
    {
        _appService = appService;
    }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadCodeAsync();
            return Page();
        }

        await _appService.UpdateAsync(Id, Input);
        return NoContent();
    }

    private async Task LoadAsync()
    {
        var group = await _appService.GetAsync(Id);
        Code = group.Code;
        Input = new UpdateCustomerGroupDto
        {
            Name = group.Name,
            Description = group.Description,
            SortOrder = group.SortOrder
        };
    }

    private async Task LoadCodeAsync()
    {
        Code = (await _appService.GetAsync(Id)).Code;
    }
}
