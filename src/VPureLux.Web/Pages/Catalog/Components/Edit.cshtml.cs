using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using VPureLux.Web.Pages.Catalog;
using VPureLux.Catalog.Components;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.Catalog.Components;

[Authorize(VPureLuxPermissions.Catalog.Components.Edit)]
public class EditModel : VPureLuxPageModel
{
    private readonly IComponentAppService _componentAppService;

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    [BindProperty]
    public UpdateComponentDto Input { get; set; } = new();

    [BindProperty]
    public IFormFile? Image { get; set; }

    public bool HasImage { get; private set; }

    public EditModel(IComponentAppService componentAppService)
    {
        _componentAppService = componentAppService;
    }

    public async Task OnGetAsync()
    {
        var component = await _componentAppService.GetAsync(Id);
        Code = component.Code;
        HasImage = component.HasImage;
        Input = new UpdateComponentDto
        {
            Name = component.Name,
            Description = component.Description,
            Unit = component.Unit,
            ReplacementPolicy = new ComponentReplacementPolicyInputDto
            {
                IsEnabled = component.IsReplacementTracked,
                CycleMonths = component.ReplacementCycleMonths ?? 3,
                WarningDaysBeforeDue = component.WarningDaysBeforeDue ?? 7,
                Note = component.ReplacementPolicyNote
            }
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _componentAppService.UpdateAsync(Id, Input);
        var image = await CatalogImageUploadHelper.ToDtoAsync(Image);
        if (image != null)
        {
            await _componentAppService.SetImageAsync(Id, image);
        }

        return RedirectToPage("/Catalog/Components/Index");
    }

    public async Task<IActionResult> OnPostRemoveImageAsync()
    {
        await _componentAppService.RemoveImageAsync(Id);
        TempData["CatalogImageSuccessMessage"] = "Catalog:ImageRemovedSuccessfully";
        return RedirectToPage(new { id = Id });
    }
}
