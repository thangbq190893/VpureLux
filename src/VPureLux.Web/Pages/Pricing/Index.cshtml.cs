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
    private readonly IProductPricingContextAppService _productPricingContextAppService;

    public IReadOnlyList<ComponentDto> Components { get; private set; } = Array.Empty<ComponentDto>();
    public IReadOnlyList<ProductPricingContextDto> ProductPricingContexts { get; private set; } =
        Array.Empty<ProductPricingContextDto>();

    public IndexModel(
        IComponentAppService componentAppService,
        IProductPricingContextAppService productPricingContextAppService)
    {
        _componentAppService = componentAppService;
        _productPricingContextAppService = productPricingContextAppService;
    }

    public async Task OnGetAsync()
    {
        Components = (await _componentAppService.GetListAsync(new GetComponentListInput
        {
            MaxResultCount = 100
        })).Items.Where(x => x.Status == CatalogItemStatus.Active).ToList();
        ProductPricingContexts = await _productPricingContextAppService.GetListAsync();
    }
}
