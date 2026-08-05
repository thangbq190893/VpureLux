using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Catalog.Products;
using VPureLux.Permissions;
using VPureLux.Pricing;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Catalog.Products;

public class IndexModel : VPureLuxPageModel
{
    private const string DefaultSorting = "CreationTime DESC";

    private readonly IProductAppService _productAppService;
    private readonly IProductPricingContextLookupService _productPricingContextLookupService;
    private readonly IAuthorizationService _authorizationService;

    [BindProperty(SupportsGet = true)]
    public string? Keyword { get; set; }

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
        await SetPermissionsAsync();
    }

    public async Task<JsonResult> OnGetListAsync(GetProductListInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Sorting))
        {
            input.Sorting = DefaultSorting;
        }

        var result = await _productAppService.GetListAsync(new GetProductListInput
        {
            Keyword = input.Keyword,
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        });

        var canViewPricingContext = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Pricing.View)).Succeeded;
        var contexts = canViewPricingContext
            ? await _productPricingContextLookupService.FindMapAsync(
                result.Items.Select(x => x.Id).ToArray(),
                Clock.Now)
            : new Dictionary<Guid, ProductPricingContextDto>();

        return new JsonResult(new PagedResultDto<ProductCatalogRow>(
            result.TotalCount,
            result.Items.Select(product =>
            {
                contexts.TryGetValue(product.Id, out var context);
                return new ProductCatalogRow(
                    product.Id,
                    product.Code,
                    product.Name,
                    product.Status.ToString(),
                    product.CreationTime,
                    product.HasImage,
                    product.ImageHash,
                    context?.CurrentProductSuggestedPrice,
                    context?.HasPublishedBom == true);
            }).ToList()));
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

    private async Task SetPermissionsAsync()
    {
        CanCreate = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Catalog.Products.Create)).Succeeded;
        CanEdit = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Catalog.Products.Edit)).Succeeded;
        CanViewPricingContext = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Pricing.View)).Succeeded;
    }

    public sealed record ProductCatalogRow(
        Guid Id,
        string Code,
        string Name,
        string Status,
        DateTime CreationTime,
        bool HasImage,
        string? ImageHash,
        decimal? CurrentProductSuggestedPrice,
        bool HasPublishedBom);
}
