using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Bom;
using VPureLux.Catalog.Products;
using VPureLux.Permissions;
using VPureLux.Pricing;

namespace VPureLux.Web.Pages.Bom;

[Authorize(VPureLuxPermissions.Bom.View)]
public class ProductModel : VPureLuxPageModel
{
    private readonly IBomAppService _bomAppService;
    private readonly IProductAppService _productAppService;
    private readonly IProductPricingContextAppService _productPricingContextAppService;
    private readonly IAuthorizationService _authorizationService;

    [BindProperty(SupportsGet = true)]
    public Guid ProductId { get; set; }

    public IReadOnlyList<BomVersionDto> Versions { get; private set; } = Array.Empty<BomVersionDto>();
    public string ProductLabel { get; private set; } = string.Empty;
    public ProductPricingContextDto? PricingContext { get; private set; }
    public bool CanCreate { get; private set; }
    public bool CanPublish { get; private set; }
    public bool CanArchive { get; private set; }
    [TempData] public string? StatusMessageKey { get; set; }

    public ProductModel(
        IBomAppService bomAppService,
        IProductAppService productAppService,
        IProductPricingContextAppService productPricingContextAppService,
        IAuthorizationService authorizationService)
    {
        _bomAppService = bomAppService;
        _productAppService = productAppService;
        _productPricingContextAppService = productPricingContextAppService;
        _authorizationService = authorizationService;
    }

    public async Task OnGetAsync()
    {
        await LoadProductLabelAsync();
        await LoadPricingContextAsync();
        Versions = await _bomAppService.GetListAsync(ProductId);
        await SetPermissionsAsync();
    }

    public async Task<IActionResult> OnPostPublishAsync(Guid id)
    {
        await _bomAppService.PublishAsync(id);
        StatusMessageKey = "Bom:PublishedSuccessfully";
        return RedirectToPage(new { productId = ProductId });
    }

    public async Task<IActionResult> OnPostArchiveAsync(Guid id)
    {
        await _bomAppService.ArchiveAsync(id);
        StatusMessageKey = "Bom:ArchivedSuccessfully";
        return RedirectToPage(new { productId = ProductId });
    }

    private async Task SetPermissionsAsync()
    {
        CanCreate = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Bom.Create)).Succeeded;
        CanPublish = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Bom.Publish)).Succeeded;
        CanArchive = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Bom.Archive)).Succeeded;
    }

    private async Task LoadProductLabelAsync()
    {
        var product = await _productAppService.GetAsync(ProductId);
        ProductLabel = $"{product.Code} - {product.Name}";
    }

    private async Task LoadPricingContextAsync()
    {
        PricingContext = (await _productPricingContextAppService.GetListAsync())
            .FirstOrDefault(x => x.ProductId == ProductId);
    }
}
