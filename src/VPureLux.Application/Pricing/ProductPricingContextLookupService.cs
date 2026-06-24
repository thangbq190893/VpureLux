using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VPureLux.Bom;
using VPureLux.Catalog;
using VPureLux.Permissions;
using Volo.Abp.Authorization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;

namespace VPureLux.Pricing;

public interface IProductPricingContextLookupService
{
    Task<List<ProductPricingContextDto>> GetListAsync(DateTime pricingDate);

    Task<IReadOnlyDictionary<Guid, ProductPricingContextDto>> FindMapAsync(
        IReadOnlyCollection<Guid> productIds,
        DateTime pricingDate);
}

public class ProductPricingContextLookupService : IProductPricingContextLookupService, ITransientDependency
{
    private readonly IProductRepository _productRepository;
    private readonly IBomVersionRepository _bomVersionRepository;
    private readonly IComponentSuggestedSellingPriceLookupService _componentPriceLookupService;
    private readonly IProductSuggestedPriceVersionRepository _productPriceRepository;
    private readonly IPermissionChecker _permissionChecker;

    public ProductPricingContextLookupService(
        IProductRepository productRepository,
        IBomVersionRepository bomVersionRepository,
        IComponentSuggestedSellingPriceLookupService componentPriceLookupService,
        IProductSuggestedPriceVersionRepository productPriceRepository,
        IPermissionChecker permissionChecker)
    {
        _productRepository = productRepository;
        _bomVersionRepository = bomVersionRepository;
        _componentPriceLookupService = componentPriceLookupService;
        _productPriceRepository = productPriceRepository;
        _permissionChecker = permissionChecker;
    }

    public async Task<List<ProductPricingContextDto>> GetListAsync(DateTime pricingDate)
    {
        await EnsurePricingViewAsync();
        var products = (await _productRepository.GetListAsync())
            .OrderBy(x => x.Code)
            .ThenBy(x => x.Name)
            .ToList();

        var contextMap = await BuildMapAsync(products, pricingDate);
        return products.Select(x => contextMap[x.Id]).ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, ProductPricingContextDto>> FindMapAsync(
        IReadOnlyCollection<Guid> productIds,
        DateTime pricingDate)
    {
        await EnsurePricingViewAsync();
        if (productIds.Count == 0)
        {
            return new Dictionary<Guid, ProductPricingContextDto>();
        }

        var idSet = productIds.Distinct().ToHashSet();
        var products = (await _productRepository.GetListAsync(x => idSet.Contains(x.Id)))
            .OrderBy(x => x.Code)
            .ThenBy(x => x.Name)
            .ToList();

        return await BuildMapAsync(products, pricingDate);
    }

    private async Task<IReadOnlyDictionary<Guid, ProductPricingContextDto>> BuildMapAsync(
        IReadOnlyList<Product> products,
        DateTime pricingDate)
    {
        if (products.Count == 0)
        {
            return new Dictionary<Guid, ProductPricingContextDto>();
        }

        var productIds = products.Select(x => x.Id).ToArray();
        var productPrices = await _productPriceRepository.FindAtDateMapAsync(productIds, pricingDate);
        var publishedBoms = await _bomVersionRepository.GetPublishedMapByProductIdsAsync(productIds);
        var componentIds = publishedBoms.Values
            .SelectMany(x => x.Items)
            .Select(x => x.ComponentId)
            .Distinct()
            .ToArray();
        var componentPrices = await _componentPriceLookupService.FindCurrentMapAsync(componentIds, pricingDate);
        var result = new Dictionary<Guid, ProductPricingContextDto>(products.Count);

        foreach (var product in products)
        {
            var dto = new ProductPricingContextDto
            {
                ProductId = product.Id,
                ProductCode = product.Code,
                ProductName = product.Name
            };

            if (productPrices.TryGetValue(product.Id, out var productPrice))
            {
                dto.CurrentProductSuggestedPrice = productPrice.Price.Amount;
            }

            if (!publishedBoms.TryGetValue(product.Id, out var publishedBom))
            {
                result.Add(product.Id, dto);
                continue;
            }

            dto.HasPublishedBom = true;
            var componentBuildPrice = 0m;
            foreach (var item in publishedBom.Items)
            {
                if (!componentPrices.TryGetValue(item.ComponentId, out var componentPrice))
                {
                    dto.HasMissingComponentSuggestedPrices = true;
                    continue;
                }

                componentBuildPrice += item.Quantity * componentPrice.Price;
            }

            if (!dto.HasMissingComponentSuggestedPrices)
            {
                dto.ComponentBuildPrice = componentBuildPrice;
            }

            if (dto.ComponentBuildPrice.HasValue && dto.CurrentProductSuggestedPrice.HasValue)
            {
                dto.Difference = dto.CurrentProductSuggestedPrice.Value - dto.ComponentBuildPrice.Value;
            }

            result.Add(product.Id, dto);
        }

        return result;
    }

    private async Task EnsurePricingViewAsync()
    {
        if (!await _permissionChecker.IsGrantedAsync(VPureLuxPermissions.Pricing.View))
        {
            throw new AbpAuthorizationException();
        }
    }
}
