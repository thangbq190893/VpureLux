using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Catalog.Products;
using VPureLux.Permissions;
using VPureLux.Pricing;

namespace VPureLux.Web.Pages.Catalog.Products;

public class IndexModel : VPureLuxPageModel
{
    private readonly IProductAppService _productAppService;
    private readonly IProductPricingContextLookupService _productPricingContextLookupService;
    private readonly IAuthorizationService _authorizationService;

    [BindProperty(SupportsGet = true)]
    public string? Keyword { get; set; }

    public IReadOnlyList<ProductDto> Products { get; private set; } = Array.Empty<ProductDto>();
    public IReadOnlyDictionary<Guid, ProductPricingContextDto> ProductPricingContexts { get; private set; } =
        new Dictionary<Guid, ProductPricingContextDto>();
    public bool CanCreate { get; private set; }
    public bool CanEdit { get; private set; }
    public bool CanViewPricingContext { get; private set; }

    public IndexModel(
        IProductAppService productAppService,
        IProductPricingContextLookupService productPricingContextLookupService,
        IAuthorizationService authorizationService)
    {
        _productAppService = productAppService;
        _productPricingContextLookupService = productPricingContextLookupService;
        _authorizationService = authorizationService;
    }

    public async Task OnGetAsync()
    {
        var result = await _productAppService.GetListAsync(new GetProductListInput
        {
            Keyword = Keyword,
            MaxResultCount = 100
        });

        Products = result.Items;
        CanCreate = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Catalog.Products.Create)).Succeeded;
        CanEdit = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Catalog.Products.Edit)).Succeeded;
        CanViewPricingContext = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Pricing.View)).Succeeded;
        if (CanViewPricingContext)
        {
            ProductPricingContexts = await _productPricingContextLookupService.FindMapAsync(
                Products.Select(x => x.Id).ToArray(),
                Clock.Now);
        }
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id)
    {
        await _productAppService.DeactivateAsync(id);
        StatusMessageKey = "Catalog:ProductDeactivatedSuccessfully";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostActivateAsync(Guid id)
    {
        await _productAppService.ActivateAsync(id);
        StatusMessageKey = "Catalog:ProductActivatedSuccessfully";
        return RedirectToPage();
    }

    [TempData] public string? StatusMessageKey { get; set; }

    public ProductPricingContextDto? GetPricingContext(Guid productId)
    {
        return ProductPricingContexts.TryGetValue(productId, out var context) ? context : null;
    }
}
