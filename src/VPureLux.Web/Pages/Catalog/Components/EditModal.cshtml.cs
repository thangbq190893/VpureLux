using System;
using System.Threading.Tasks;
using global::VPureLux.Catalog.Components;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.Catalog.Components;

[Authorize(VPureLuxPermissions.Catalog.Components.Edit)]
public class EditModalModel : VPureLuxPageModel
{
    private readonly IComponentAppService _componentAppService;

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public string Code { get; set; } = string.Empty;
    [BindProperty] public UpdateComponentDto Input { get; set; } = new();

    public EditModalModel(IComponentAppService componentAppService)
    {
        _componentAppService = componentAppService;
    }

    public async Task OnGetAsync()
    {
        var component = await _componentAppService.GetAsync(Id);
        Code = component.Code;
        Input = new UpdateComponentDto
        {
            Name = component.Name,
            Description = component.Description,
            Unit = component.Unit
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _componentAppService.UpdateAsync(Id, Input);
        return NoContent();
    }
}
