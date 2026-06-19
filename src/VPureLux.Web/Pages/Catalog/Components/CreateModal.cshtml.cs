using System.Threading.Tasks;
using global::VPureLux.Catalog.Components;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.Catalog.Components;

[Authorize(VPureLuxPermissions.Catalog.Components.Create)]
public class CreateModalModel : VPureLuxPageModel
{
    private readonly IComponentAppService _componentAppService;

    [BindProperty] public CreateComponentDto Input { get; set; } = new();

    public CreateModalModel(IComponentAppService componentAppService)
    {
        _componentAppService = componentAppService;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _componentAppService.CreateAsync(Input);
        return NoContent();
    }
}
