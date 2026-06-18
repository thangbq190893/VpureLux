using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using VPureLux.Web.Pages.Catalog;
using VPureLux.Catalog.Components;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.Catalog.Components;

[Authorize(VPureLuxPermissions.Catalog.Components.Create)]
public class CreateModel : VPureLuxPageModel
{
    private readonly IComponentAppService _componentAppService;

    [BindProperty]
    public CreateComponentDto Input { get; set; } = new();

    [BindProperty]
    public IFormFile? Image { get; set; }

    public CreateModel(IComponentAppService componentAppService)
    {
        _componentAppService = componentAppService;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var component = await _componentAppService.CreateAsync(Input);
        var image = await CatalogImageUploadHelper.ToDtoAsync(Image);
        if (image != null)
        {
            await _componentAppService.SetImageAsync(component.Id, image);
        }

        return RedirectToPage("/Catalog/Components/Index");
    }
}
