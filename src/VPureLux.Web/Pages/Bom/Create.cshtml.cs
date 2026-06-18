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
public class CreateModel : VPureLuxPageModel
{
    private readonly IBomAppService _bomAppService;
    private readonly IComponentAppService _componentAppService;

    [BindProperty(SupportsGet = true)]
    public Guid ProductId { get; set; }

    [BindProperty]
    public string EffectiveFromText { get; set; } = string.Empty;

    [BindProperty]
    public List<BomItemSelectionModel> Items { get; set; } = new() { new() };

    public List<SelectListItem> ComponentOptions { get; private set; } = new();

    public CreateModel(IBomAppService bomAppService, IComponentAppService componentAppService)
    {
        _bomAppService = bomAppService;
        _componentAppService = componentAppService;
    }

    public async Task OnGetAsync()
    {
        EffectiveFromText = BomUi.FormatDate(DateTime.Today);
        await LoadComponentOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadComponentOptionsAsync();

        if (!BomUi.TryParseDate(EffectiveFromText, out var effectiveFrom))
        {
            ModelState.AddModelError(nameof(EffectiveFromText), L["Bom:InvalidDateFormat"]);
            return Page();
        }

        var items = Items.Where(x => x.ComponentId.HasValue).ToList();
        if (items.Count == 0)
        {
            ModelState.AddModelError(string.Empty, L["Bom:AtLeastOneItem"]);
            return Page();
        }

        var result = await _bomAppService.CreateAsync(ProductId, new CreateBomVersionDto
        {
            EffectiveFrom = effectiveFrom,
            Items = items.Select(x => new CreateBomItemDto
            {
                ComponentId = x.ComponentId!.Value,
                Quantity = x.Quantity
            }).ToList()
        });

        return RedirectToPage("/Bom/Details", new { id = result.Id });
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
