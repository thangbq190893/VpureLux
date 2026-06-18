using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VPureLux.Bom;
using VPureLux.Catalog;
using VPureLux.Catalog.Components;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.Bom;

[Authorize(VPureLuxPermissions.Bom.Create)]
public class EditModel : VPureLuxPageModel
{
    private readonly IBomAppService _bomAppService;
    private readonly IComponentAppService _componentAppService;

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public List<BomItemSelectionModel> Items { get; set; } = new();

    public List<SelectListItem> ComponentOptions { get; private set; } = new();

    public EditModel(IBomAppService bomAppService, IComponentAppService componentAppService)
    {
        _bomAppService = bomAppService;
        _componentAppService = componentAppService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadComponentOptionsAsync();

        var bomVersion = await _bomAppService.GetAsync(Id);
        if (bomVersion.Status != BomStatus.Draft)
        {
            return RedirectToPage("/Bom/Details", new { id = Id });
        }

        Items = bomVersion.Items.Select(x => new BomItemSelectionModel
        {
            ComponentId = x.ComponentId,
            Quantity = x.Quantity
        }).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadComponentOptionsAsync();

        var items = Items.Where(x => x.ComponentId.HasValue).ToList();
        if (items.Count == 0)
        {
            ModelState.AddModelError(string.Empty, L["Bom:AtLeastOneItem"]);
            return Page();
        }

        await _bomAppService.UpdateAsync(Id, new UpdateBomVersionDto
        {
            Items = items.Select(x => new CreateBomItemDto
            {
                ComponentId = x.ComponentId!.Value,
                Quantity = x.Quantity
            }).ToList()
        });

        return RedirectToPage("/Bom/Details", new { id = Id });
    }

    private async Task LoadComponentOptionsAsync()
    {
        var components = await _componentAppService.GetListAsync(new GetComponentListInput
        {
            MaxResultCount = 1000
        });

        ComponentOptions = components.Items
            .Where(x => x.Status == CatalogItemStatus.Active)
            .OrderBy(x => x.Code)
            .ThenBy(x => x.Name)
            .Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString()))
            .ToList();
    }
}
