using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Catalog.Components;
using VPureLux.Permissions;
using VPureLux.Pricing;
using VPureLux.Web.Pages.Pricing;
using Volo.Abp;

namespace VPureLux.Web.Pages.Pricing.Components;

[Authorize(VPureLuxPermissions.Pricing.ComponentSuggestedSellingPrices.Create)]
public class CreateModel : VPureLuxPageModel
{
    private readonly IComponentSuggestedSellingPriceAppService _appService;
    private readonly IComponentAppService _componentAppService;

    [BindProperty(SupportsGet = true)] public Guid ComponentId { get; set; }
    [BindProperty] public CreateComponentSuggestedSellingPriceVersionDto Input { get; set; } = new();
    [BindProperty] public string EffectiveFromText { get; set; } = string.Empty;
    public string ComponentLabel { get; private set; } = string.Empty;

    public CreateModel(
        IComponentSuggestedSellingPriceAppService appService,
        IComponentAppService componentAppService)
    {
        _appService = appService;
        _componentAppService = componentAppService;
    }

    public async Task OnGetAsync()
    {
        await LoadComponentContextAsync();
        Input.EffectiveFrom = Clock.Now.Date;
        EffectiveFromText = PricingDateUi.Format(Input.EffectiveFrom);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadComponentContextAsync();
        if (!PricingDateUi.TryParse(EffectiveFromText, out var effectiveFrom))
        {
            ModelState.AddModelError(nameof(EffectiveFromText), L["Pricing:InvalidDateFormat"]);
            return Page();
        }

        Input.EffectiveFrom = effectiveFrom;
        try
        {
            await _appService.CreateAsync(ComponentId, Input);
            return RedirectToPage("/Pricing/Components/History", new { componentId = ComponentId });
        }
        catch (BusinessException exception) when (
            exception.Code == VPureLuxDomainErrorCodes.ComponentNotActive ||
            exception.Code == VPureLuxDomainErrorCodes.BackdatedPriceVersionNotAllowed)
        {
            ModelState.AddModelError(string.Empty, L[exception.Code]);
            return Page();
        }
    }

    private async Task LoadComponentContextAsync()
    {
        var component = await _componentAppService.GetAsync(ComponentId);
        ComponentLabel = $"{component.Code} - {component.Name}";
    }
}
