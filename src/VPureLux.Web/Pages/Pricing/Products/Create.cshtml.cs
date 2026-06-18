using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;
using VPureLux.Pricing;
using VPureLux.Web.Pages.Pricing;
using Volo.Abp;

namespace VPureLux.Web.Pages.Pricing.Products;

[Authorize(VPureLuxPermissions.Pricing.ProductSuggestedPrices.Create)]
public class CreateModel : VPureLuxPageModel
{
    private readonly IProductSuggestedPriceAppService _appService;

    [BindProperty(SupportsGet = true)] public Guid ProductId { get; set; }
    [BindProperty] public CreateProductSuggestedPriceVersionDto Input { get; set; } = new();
    [BindProperty] public string EffectiveFromText { get; set; } = string.Empty;

    public CreateModel(IProductSuggestedPriceAppService appService)
    {
        _appService = appService;
    }

    public void OnGet()
    {
        Input.EffectiveFrom = Clock.Now.Date;
        EffectiveFromText = PricingDateUi.Format(Input.EffectiveFrom);
    }

    public async Task<IActionResult> OnPostAsync()
    {
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
}
