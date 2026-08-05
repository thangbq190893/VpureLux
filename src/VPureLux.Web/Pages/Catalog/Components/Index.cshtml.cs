using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Catalog.Components;
using VPureLux.Permissions;
using VPureLux.Pricing;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Catalog.Components;

public class IndexModel : VPureLuxPageModel
{
    private const string DefaultSorting = "CreationTime DESC";

    private readonly IComponentAppService _componentAppService;
    private readonly IComponentSuggestedSellingPriceLookupService _componentPriceLookupService;
    private readonly IAuthorizationService _authorizationService;

    [BindProperty(SupportsGet = true)]
    public string? Keyword { get; set; }

    public bool CanCreate { get; private set; }
    public bool CanEdit { get; private set; }
    public bool CanViewPricingContext { get; private set; }

    public IndexModel(
        IComponentAppService componentAppService,
        IComponentSuggestedSellingPriceLookupService componentPriceLookupService,
        IAuthorizationService authorizationService)
    {
        _componentAppService = componentAppService;
        _componentPriceLookupService = componentPriceLookupService;
        _authorizationService = authorizationService;
    }

    public async Task OnGetAsync()
    {
        await SetPermissionsAsync();
    }

    public async Task<JsonResult> OnGetListAsync(GetComponentListInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Sorting))
        {
            input.Sorting = DefaultSorting;
        }

        var result = await _componentAppService.GetListAsync(new GetComponentListInput
        {
            Keyword = input.Keyword,
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        });

        var canViewPricingContext = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Pricing.View)).Succeeded;

        var currentPrices = canViewPricingContext
            ? await _componentPriceLookupService.FindCurrentMapAsync(
                result.Items.Select(x => x.Id).ToArray(),
                Clock.Now)
            : new Dictionary<Guid, ComponentSuggestedSellingPriceVersionDto>();

        return new JsonResult(new PagedResultDto<ComponentCatalogRow>(
            result.TotalCount,
            result.Items.Select(component =>
            {
                var currentPrice = currentPrices.GetValueOrDefault(component.Id);
                return new ComponentCatalogRow(
                    component.Id,
                    component.Code,
                    component.Name,
                    component.Unit,
                    component.Status.ToString(),
                    component.CreationTime,
                    component.HasImage,
                    component.ImageHash,
                    currentPrice?.Price);
            }).ToList()));
    }

    public async Task<IActionResult> OnPostDeactivateAsync(Guid id)
    {
        await _componentAppService.DeactivateAsync(id);
        StatusMessageKey = "Catalog:ComponentDeactivatedSuccessfully";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostActivateAsync(Guid id)
    {
        await _componentAppService.ActivateAsync(id);
        StatusMessageKey = "Catalog:ComponentActivatedSuccessfully";
        return RedirectToPage();
    }

    [TempData] public string? StatusMessageKey { get; set; }

    private async Task SetPermissionsAsync()
    {
        CanCreate = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Catalog.Components.Create)).Succeeded;
        CanEdit = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Catalog.Components.Edit)).Succeeded;
        CanViewPricingContext = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Pricing.View)).Succeeded;
    }

    public sealed record ComponentCatalogRow(
        Guid Id,
        string Code,
        string Name,
        string Unit,
        string Status,
        DateTime CreationTime,
        bool HasImage,
        string? ImageHash,
        decimal? CurrentSuggestedSellingPrice);
}
