using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Catalog;
using VPureLux.Catalog.Components;
using VPureLux.Permissions;
using VPureLux.Pricing;
using VPureLux.Web.Pages.Pricing;
using Volo.Abp;

namespace VPureLux.Web.Pages.Pricing.Components;

[Authorize(VPureLuxPermissions.Pricing.ComponentSuggestedSellingPrices.History)]
public class HistoryModel : VPureLuxPageModel
{
    private readonly IComponentSuggestedSellingPriceAppService _appService;
    private readonly IComponentAppService _componentAppService;
    private readonly IAuthorizationService _authorizationService;

    [BindProperty(SupportsGet = true)] public Guid ComponentId { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? LookupDate { get; set; }
    [BindProperty(SupportsGet = true)] public string? LookupDateText { get; set; }
    public IReadOnlyList<ComponentSuggestedSellingPriceVersionDto> Versions { get; private set; } =
        Array.Empty<ComponentSuggestedSellingPriceVersionDto>();
    public ComponentSuggestedSellingPriceVersionDto? CurrentVersion { get; private set; }
    public ComponentSuggestedSellingPriceVersionDto? HistoricalVersion { get; private set; }
    public bool CanCreate { get; private set; }
    public string ComponentLabel { get; private set; } = string.Empty;

    public HistoryModel(
        IComponentSuggestedSellingPriceAppService appService,
        IComponentAppService componentAppService,
        IAuthorizationService authorizationService)
    {
        _appService = appService;
        _componentAppService = componentAppService;
        _authorizationService = authorizationService;
    }

    public async Task OnGetAsync()
    {
        var component = await _componentAppService.GetAsync(ComponentId);
        ComponentLabel = $"{component.Code} - {component.Name}";

        Versions = await _appService.GetHistoryAsync(ComponentId);
        CurrentVersion = await TryGetAsync(() => _appService.GetCurrentAsync(ComponentId));
        if (!string.IsNullOrWhiteSpace(LookupDateText))
        {
            if (PricingDateUi.TryParse(LookupDateText, out var lookupDate))
            {
                LookupDate = lookupDate;
                HistoricalVersion = await TryGetAsync(() => _appService.GetAtDateAsync(ComponentId, lookupDate));
            }
            else
            {
                ModelState.AddModelError(nameof(LookupDateText), L["Pricing:InvalidDateFormat"]);
            }
        }
        else if (LookupDate.HasValue)
        {
            LookupDateText = PricingDateUi.Format(LookupDate.Value);
            HistoricalVersion = await TryGetAsync(() => _appService.GetAtDateAsync(ComponentId, LookupDate.Value));
        }

        CanCreate = (await _authorizationService.AuthorizeAsync(
            User, VPureLuxPermissions.Pricing.ComponentSuggestedSellingPrices.Create)).Succeeded &&
            component.Status == CatalogItemStatus.Active;
    }

    private static async Task<ComponentSuggestedSellingPriceVersionDto?> TryGetAsync(
        Func<Task<ComponentSuggestedSellingPriceVersionDto>> action)
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
