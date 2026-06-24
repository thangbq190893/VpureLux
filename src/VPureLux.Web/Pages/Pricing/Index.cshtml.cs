using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VPureLux.Catalog;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Catalog.Components;
using VPureLux.Permissions;
using VPureLux.Pricing;

namespace VPureLux.Web.Pages.Pricing;

[Authorize(VPureLuxPermissions.Pricing.View)]
public class IndexModel : VPureLuxPageModel
{
    private readonly IComponentAppService _componentAppService;
    private readonly IComponentSuggestedSellingPriceLookupService _componentPriceLookupService;
    private readonly IProductPricingContextAppService _productPricingContextAppService;

    public IReadOnlyList<ComponentPricingRow> Components { get; private set; } = Array.Empty<ComponentPricingRow>();
    public IReadOnlyList<ProductPricingContextDto> ProductPricingContexts { get; private set; } =
        Array.Empty<ProductPricingContextDto>();

    public IndexModel(
        IComponentAppService componentAppService,
        IComponentSuggestedSellingPriceLookupService componentPriceLookupService,
        IProductPricingContextAppService productPricingContextAppService)
    {
        _componentAppService = componentAppService;
        _componentPriceLookupService = componentPriceLookupService;
        _productPricingContextAppService = productPricingContextAppService;
    }

    public async Task OnGetAsync()
    {
        var activeComponents = (await _componentAppService.GetListAsync(new GetComponentListInput
        {
            MaxResultCount = 100
        })).Items.Where(x => x.Status == CatalogItemStatus.Active).ToList();

        var currentPrices = await _componentPriceLookupService.FindCurrentMapAsync(
            activeComponents.Select(x => x.Id).ToArray(),
            Clock.Now);

        var componentRows = new List<ComponentPricingRow>(activeComponents.Count);
        foreach (var component in activeComponents)
        {
            componentRows.Add(new ComponentPricingRow(
                component,
                currentPrices.GetValueOrDefault(component.Id)));
        }

        Components = componentRows;
        ProductPricingContexts = await _productPricingContextAppService.GetListAsync();
    }

    public sealed record ComponentPricingRow(
        ComponentDto Component,
        ComponentSuggestedSellingPriceVersionDto? CurrentSuggestedSellingPrice);
}
