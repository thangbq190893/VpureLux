using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Catalog.Components;
using VPureLux.Permissions;
using VPureLux.Pricing;

namespace VPureLux.Web.Pages.Catalog.Components;

public class IndexModel : VPureLuxPageModel
{
    private readonly IComponentAppService _componentAppService;
    private readonly IComponentSuggestedSellingPriceLookupService _componentPriceLookupService;
    private readonly IAuthorizationService _authorizationService;

    [BindProperty(SupportsGet = true)]
    public string? Keyword { get; set; }

    public IReadOnlyList<ComponentCatalogRow> Components { get; private set; } = Array.Empty<ComponentCatalogRow>();
    public bool CanCreate { get; private set; }
    public bool CanEdit { get; private set; }
    public bool CanViewPricingContext { get; private set; }

    public IndexModel(
        IComponentAppService componentAppService,
        IComponentSuggestedSellingPriceLookupService componentPriceLookupService,
        IAuthorizationService authorizationService)
    {
        _componentAppService = componentAppService;
        _componentPriceLookupService = componentPriceLookupService;
        _authorizationService = authorizationService;
    }

    public async Task OnGetAsync()
    {
        var result = await _componentAppService.GetListAsync(new GetComponentListInput
        {
            Keyword = Keyword,
            MaxResultCount = 100
        });

        CanCreate = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Catalog.Components.Create)).Succeeded;
        CanEdit = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Catalog.Components.Edit)).Succeeded;
        CanViewPricingContext = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Pricing.View)).Succeeded;

        var currentPrices = CanViewPricingContext
            ? await _componentPriceLookupService.FindCurrentMapAsync(
                result.Items.Select(x => x.Id).ToArray(),
                Clock.Now)
            : new Dictionary<Guid, ComponentSuggestedSellingPriceVersionDto>();

        var rows = new List<ComponentCatalogRow>(result.Items.Count);
        foreach (var component in result.Items)
        {
            rows.Add(new ComponentCatalogRow(
                component,
                currentPrices.GetValueOrDefault(component.Id)));
        }

        Components = rows;
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id)
    {
        await _componentAppService.DeactivateAsync(id);
        StatusMessageKey = "Catalog:ComponentDeactivatedSuccessfully";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostActivateAsync(Guid id)
    {
        await _componentAppService.ActivateAsync(id);
        StatusMessageKey = "Catalog:ComponentActivatedSuccessfully";
        return RedirectToPage();
    }

    [TempData] public string? StatusMessageKey { get; set; }

    public sealed record ComponentCatalogRow(
        ComponentDto Component,
        ComponentSuggestedSellingPriceVersionDto? CurrentSuggestedSellingPrice);
}
