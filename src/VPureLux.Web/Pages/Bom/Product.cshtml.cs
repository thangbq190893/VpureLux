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
using Volo.Abp;

namespace VPureLux.Web.Pages.Bom;

[Authorize(VPureLuxPermissions.Bom.View)]
public class ProductModel : VPureLuxPageModel
{
    private readonly IBomAppService _bomAppService;
    private readonly IProductAppService _productAppService;
    private readonly IProductPricingContextLookupService _productPricingContextLookupService;
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
        IProductPricingContextLookupService productPricingContextLookupService,
        IAuthorizationService authorizationService)
    {
        _bomAppService = bomAppService;
        _productAppService = productAppService;
        _productPricingContextLookupService = productPricingContextLookupService;
        _authorizationService = authorizationService;
    }

    public async Task OnGetAsync()
    {
        await LoadPageAsync();
    }

    public async Task<IActionResult> OnPostPublishAsync(Guid id)
    {
        try
        {
            await _bomAppService.PublishAsync(id);
            StatusMessageKey = "Bom:PublishedSuccessfully";
            return RedirectToPage(new { productId = ProductId });
        }
        catch (BusinessException exception)
        {
            AddBusinessError(exception);
            await LoadPageAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostArchiveAsync(Guid id)
    {
        try
        {
            await _bomAppService.ArchiveAsync(id);
            StatusMessageKey = "Bom:ArchivedSuccessfully";
            return RedirectToPage(new { productId = ProductId });
        }
        catch (BusinessException exception)
        {
            AddBusinessError(exception);
            await LoadPageAsync();
            return Page();
        }
    }

    private async Task LoadPageAsync()
    {
        await LoadProductLabelAsync();
        await LoadPricingContextAsync();
        Versions = await _bomAppService.GetListAsync(ProductId);
        await SetPermissionsAsync();
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
        var contexts = await _productPricingContextLookupService.FindMapAsync([ProductId], Clock.Now);
        PricingContext = contexts.GetValueOrDefault(ProductId);
    }
}
