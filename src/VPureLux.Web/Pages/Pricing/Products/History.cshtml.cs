using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;
using VPureLux.Pricing;
using VPureLux.Web.Pages.Pricing;
using Volo.Abp;

namespace VPureLux.Web.Pages.Pricing.Products;

[Authorize(VPureLuxPermissions.Pricing.History)]
public class HistoryModel : VPureLuxPageModel
{
    private readonly IProductSuggestedPriceAppService _appService;
    private readonly IAuthorizationService _authorizationService;

    [BindProperty(SupportsGet = true)] public Guid ProductId { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? LookupDate { get; set; }
    [BindProperty(SupportsGet = true)] public string? LookupDateText { get; set; }
    public IReadOnlyList<ProductSuggestedPriceVersionDto> Versions { get; private set; } =
        Array.Empty<ProductSuggestedPriceVersionDto>();
    public ProductSuggestedPriceVersionDto? CurrentVersion { get; private set; }
    public ProductSuggestedPriceVersionDto? HistoricalVersion { get; private set; }
    public bool CanCreate { get; private set; }

    public HistoryModel(
        IProductSuggestedPriceAppService appService,
        IAuthorizationService authorizationService)
    {
        _appService = appService;
        _authorizationService = authorizationService;
    }

    public async Task OnGetAsync()
    {
        Versions = await _appService.GetHistoryAsync(ProductId);
        CurrentVersion = await TryGetAsync(() => _appService.GetCurrentAsync(ProductId));
        if (!string.IsNullOrWhiteSpace(LookupDateText))
        {
            if (PricingDateUi.TryParse(LookupDateText, out var lookupDate))
            {
                LookupDate = lookupDate;
                HistoricalVersion = await TryGetAsync(() => _appService.GetAtDateAsync(ProductId, lookupDate));
            }
            else
            {
                ModelState.AddModelError(nameof(LookupDateText), L["Pricing:InvalidDateFormat"]);
            }
        }
        else if (LookupDate.HasValue)
        {
            LookupDateText = PricingDateUi.Format(LookupDate.Value);
            HistoricalVersion = await TryGetAsync(() => _appService.GetAtDateAsync(ProductId, LookupDate.Value));
        }

        CanCreate = (await _authorizationService.AuthorizeAsync(
            User, VPureLuxPermissions.Pricing.ProductSuggestedPrices.Create)).Succeeded;
    }

    private static async Task<ProductSuggestedPriceVersionDto?> TryGetAsync(
        Func<Task<ProductSuggestedPriceVersionDto>> action)
    {
        try
        {
            return await action();
        }
        catch (BusinessException exception) when (exception.Code == VPureLuxDomainErrorCodes.PriceVersionNotFound)
        {
            return null;
        }
    }
}
