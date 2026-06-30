using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using VPureLux.Bom;
using VPureLux.Catalog;
using VPureLux.Catalog.Components;
using VPureLux.Localization;
using VPureLux.Permissions;
using Volo.Abp;
using Volo.Abp.Data;

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

        try
        {
            await _bomAppService.UpdateAsync(Id, new UpdateBomVersionDto
            {
                Items = items.Select(x => new CreateBomItemDto
                {
                    ComponentId = x.ComponentId!.Value,
                    Quantity = x.Quantity
                }).ToList()
            });
        }
        catch (AbpDbConcurrencyException)
        {
            ModelState.AddModelError(string.Empty, Localize("Bom:ConcurrencyError"));
            return Page();
        }
        catch (BusinessException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Code == null ? exception.Message : Localize(exception.Code));
            return Page();
        }

        return RedirectToPage("/Bom/Details", new { id = Id });
    }

    private string Localize(string key)
    {
        var localizer = HttpContext?.RequestServices.GetService<IStringLocalizer<VPureLuxResource>>();
        return localizer?[key].Value ?? L[key].Value;
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
