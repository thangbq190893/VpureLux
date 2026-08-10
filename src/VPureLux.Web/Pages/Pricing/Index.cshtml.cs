using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Bom;
using VPureLux.Catalog;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using VPureLux.Permissions;
using VPureLux.Pricing;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Pricing;

[Authorize(VPureLuxPermissions.Pricing.View)]
public class IndexModel : VPureLuxPageModel
{
    private const string DefaultSorting = "Code ASC";

    private readonly IComponentAppService _componentAppService;
    private readonly IComponentSuggestedSellingPriceLookupService _componentPriceLookupService;
    private readonly IProductAppService _productAppService;
    private readonly IProductPricingContextLookupService _productPricingContextLookupService;
    private readonly IBomVersionRepository _bomVersionRepository;
    private readonly IBomStandardCostLookupService _standardCostLookupService;
    private readonly IAuthorizationService _authorizationService;

    public bool CanViewComponentHistory { get; private set; }
    public bool CanViewProductHistory { get; private set; }
    public bool CanCreateComponentSuggestedPrice { get; private set; }
    public bool CanCreateProductSuggestedPrice { get; private set; }

    public IndexModel(
        IComponentAppService componentAppService,
        IComponentSuggestedSellingPriceLookupService componentPriceLookupService,
        IProductAppService productAppService,
        IProductPricingContextLookupService productPricingContextLookupService,
        IBomVersionRepository bomVersionRepository,
        IBomStandardCostLookupService standardCostLookupService,
        IAuthorizationService authorizationService)
    {
        _componentAppService = componentAppService;
        _componentPriceLookupService = componentPriceLookupService;
        _productAppService = productAppService;
        _productPricingContextLookupService = productPricingContextLookupService;
        _bomVersionRepository = bomVersionRepository;
        _standardCostLookupService = standardCostLookupService;
        _authorizationService = authorizationService;
    }

    public async Task OnGetAsync()
    {
        await SetPermissionsAsync();
    }

    public async Task<JsonResult> OnGetComponentListAsync(GetComponentListInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Sorting))
        {
            input.Sorting = DefaultSorting;
        }

        var components = await _componentAppService.GetListAsync(new GetComponentListInput
        {
            Keyword = input.Keyword,
            Status = CatalogItemStatus.Active,
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        });
        var currentPrices = await _componentPriceLookupService.FindCurrentMapAsync(
            components.Items.Select(x => x.Id).ToArray(),
            Clock.Now);

        var rows = components.Items.Select(component =>
        {
            currentPrices.TryGetValue(component.Id, out var price);
            return new ComponentPricingListRow(
                component.Id,
                component.Code,
                component.Name,
                price?.Price,
                price?.Currency ?? PricingConsts.Currency,
                price == null ? string.Empty : PricingDateUi.Format(price.EffectiveFrom),
                price != null,
                component.Status == CatalogItemStatus.Active);
        }).ToList();

        return new JsonResult(new PagedResultDto<ComponentPricingListRow>(components.TotalCount, rows));
    }

    public async Task<JsonResult> OnGetProductListAsync(GetProductListInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Sorting))
        {
            input.Sorting = DefaultSorting;
        }

        var products = await _productAppService.GetListAsync(new GetProductListInput
        {
            Keyword = input.Keyword,
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        });
        var contexts = await _productPricingContextLookupService.FindMapAsync(
            products.Items.Select(x => x.Id).ToArray(),
            Clock.Now);
        var publishedBomMap = await _bomVersionRepository.GetPublishedMapByProductIdsAsync(
            products.Items.Select(x => x.Id).ToArray());
        var costMap = await _standardCostLookupService.FindMapAsync(publishedBomMap.Values.Select(x => x.Id).ToArray());

        var rows = products.Items.Select(product =>
        {
            contexts.TryGetValue(product.Id, out var context);
            publishedBomMap.TryGetValue(product.Id, out var bom);
            var standardCostRange = FormatStandardCost(bom, costMap);
            context ??= new ProductPricingContextDto
            {
                ProductId = product.Id,
                ProductCode = product.Code,
                ProductName = product.Name
            };

            return new ProductPricingListRow(
                context.ProductId,
                context.ProductCode,
                context.ProductName,
                context.HasPublishedBom,
                context.HasMissingComponentSuggestedPrices,
                standardCostRange,
                context.CurrentProductSuggestedPrice,
                product.Status == CatalogItemStatus.Active);
        }).ToList();

        return new JsonResult(new PagedResultDto<ProductPricingListRow>(products.TotalCount, rows));
    }

    private async Task SetPermissionsAsync()
    {
        CanViewComponentHistory = (await _authorizationService.AuthorizeAsync(
            User,
            null,
            VPureLuxPermissions.Pricing.ComponentSuggestedSellingPrices.History)).Succeeded;
        CanViewProductHistory = (await _authorizationService.AuthorizeAsync(
            User,
            null,
            VPureLuxPermissions.Pricing.History)).Succeeded;
        CanCreateComponentSuggestedPrice = (await _authorizationService.AuthorizeAsync(
            User,
            null,
            VPureLuxPermissions.Pricing.ComponentSuggestedSellingPrices.Create)).Succeeded;
        CanCreateProductSuggestedPrice = (await _authorizationService.AuthorizeAsync(
            User,
            null,
            VPureLuxPermissions.Pricing.ProductSuggestedPrices.Create)).Succeeded;
    }

    private string FormatStandardCost(
        BomVersion? bom,
        IReadOnlyDictionary<Guid, BomStandardCostRangeDto> costMap)
    {
        if (bom == null)
        {
            return L["Bom:NoPublishedBom"].Value;
        }

        costMap.TryGetValue(bom.Id, out var cost);
        if (cost?.HasCompleteCost != true)
        {
            return L["Bom:MissingInventoryCost", cost?.MissingComponentCount ?? 0].Value;
        }

        return VPureLux.Web.Pages.Bom.BomUi.FormatMoneyRange(cost.MinTotalCost!.Value, cost.MaxTotalCost!.Value);
    }

    public sealed record ComponentPricingListRow(
        Guid ComponentId,
        string Code,
        string Name,
        decimal? CurrentSuggestedSellingPrice,
        string Currency,
        string EffectiveFrom,
        bool HasCurrentSuggestedSellingPrice,
        bool CanCreateSuggestedPrice);

    public sealed record ProductPricingListRow(
        Guid ProductId,
        string ProductCode,
        string ProductName,
        bool HasPublishedBom,
        bool HasMissingComponentSuggestedPrices,
        string StandardCostRange,
        decimal? CurrentProductSuggestedPrice,
        bool CanCreateSuggestedPrice);
}
