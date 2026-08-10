using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Bom;
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
    private readonly IProductPricingContextLookupService _productPricingContextLookupService;
    private readonly IBomVersionRepository _bomVersionRepository;
    private readonly IBomStandardCostLookupService _standardCostLookupService;

    [BindProperty(SupportsGet = true)] public Guid ProductId { get; set; }
    [BindProperty] public CreateProductSuggestedPriceVersionDto Input { get; set; } = new();
    [BindProperty] public string EffectiveFromText { get; set; } = string.Empty;
    public string ProductLabel { get; private set; } = string.Empty;
    public string StandardCostRangeText { get; private set; } = string.Empty;
    public bool HasStandardCostRange { get; private set; }
    public int MissingInventoryCostCount { get; private set; }
    public bool HasPublishedBom { get; private set; }
    public bool HasMissingComponentSuggestedPrices { get; private set; }

    public CreateModel(
        IProductSuggestedPriceAppService appService,
        IProductAppService productAppService,
        IProductPricingContextLookupService productPricingContextLookupService,
        IBomVersionRepository bomVersionRepository,
        IBomStandardCostLookupService standardCostLookupService)
    {
        _appService = appService;
        _productAppService = productAppService;
        _productPricingContextLookupService = productPricingContextLookupService;
        _bomVersionRepository = bomVersionRepository;
        _standardCostLookupService = standardCostLookupService;
    }

    public async Task OnGetAsync()
    {
        await LoadProductContextAsync();
        Input.EffectiveFrom = Clock.Now.Date;
        EffectiveFromText = PricingDateUi.Format(Input.EffectiveFrom);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadProductContextAsync();
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

    private async Task LoadProductContextAsync()
    {
        var product = await _productAppService.GetAsync(ProductId);
        ProductLabel = $"{product.Code} - {product.Name}";

        var context = (await _productPricingContextLookupService.FindMapAsync([ProductId], Clock.Now))
            .Values
            .FirstOrDefault();
        if (context == null)
        {
            return;
        }

        HasPublishedBom = context.HasPublishedBom;
        HasMissingComponentSuggestedPrices = context.HasMissingComponentSuggestedPrices;
        await LoadStandardCostRangeAsync();
    }

    private async Task LoadStandardCostRangeAsync()
    {
        var bom = (await _bomVersionRepository.GetListByProductIdAsync(ProductId))
            .FirstOrDefault(x => x.Status == BomStatus.Published);
        if (bom == null)
        {
            HasPublishedBom = false;
            return;
        }

        HasPublishedBom = true;
        var cost = await _standardCostLookupService.GetAsync(bom.Id);
        if (!cost.HasCompleteCost)
        {
            MissingInventoryCostCount = cost.MissingComponentCount;
            return;
        }

        HasStandardCostRange = true;
        StandardCostRangeText = VPureLux.Web.Pages.Bom.BomUi.FormatMoneyRange(
            cost.MinTotalCost!.Value,
            cost.MaxTotalCost!.Value);
    }
}
