using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Catalog.Products;
using VPureLux.Permissions;
using VPureLux.Pricing;
using VPureLux.Web.Pages.Pricing;
using Volo.Abp;

namespace VPureLux.Web.Pages.Pricing.Products;

[Authorize(VPureLuxPermissions.Pricing.ProductSuggestedPrices.Create)]
public class CreateModel : VPureLuxPageModel
{
    private readonly IProductSuggestedPriceAppService _appService;
    private readonly IProductAppService _productAppService;

    [BindProperty(SupportsGet = true)] public Guid ProductId { get; set; }
    [BindProperty] public CreateProductSuggestedPriceVersionDto Input { get; set; } = new();
    [BindProperty] public string EffectiveFromText { get; set; } = string.Empty;
    public string ProductLabel { get; private set; } = string.Empty;

    public CreateModel(IProductSuggestedPriceAppService appService, IProductAppService productAppService)
    {
        _appService = appService;
        _productAppService = productAppService;
    }

    public async Task OnGetAsync()
    {
        await LoadProductLabelAsync();
        Input.EffectiveFrom = Clock.Now.Date;
        EffectiveFromText = PricingDateUi.Format(Input.EffectiveFrom);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadProductLabelAsync();
        if (!PricingDateUi.TryParse(EffectiveFromText, out var effectiveFrom))
        {
            ModelState.AddModelError(nameof(EffectiveFromText), L["Pricing:InvalidDateFormat"]);
            return Page();
        }

        Input.EffectiveFrom = effectiveFrom;
        try
        {
            await _appService.CreateAsync(ProductId, Input);
            return RedirectToPage("/Pricing/Products/History", new { productId = ProductId });
        }
        catch (BusinessException exception) when (exception.Code == VPureLuxDomainErrorCodes.BackdatedPriceVersionNotAllowed)
        {
            ModelState.AddModelError(string.Empty, L[exception.Code]);
            return Page();
        }
    }

    private async Task LoadProductLabelAsync()
    {
        var product = await _productAppService.GetAsync(ProductId);
        ProductLabel = $"{product.Code} - {product.Name}";
    }
}
