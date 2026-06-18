using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Bom;
using VPureLux.Catalog;
using VPureLux.Permissions;
using Volo.Abp.Application.Services;

namespace VPureLux.Pricing;

[Authorize(VPureLuxPermissions.Pricing.View)]
public class ProductPricingContextAppService : ApplicationService, IProductPricingContextAppService
{
    private readonly IProductRepository _productRepository;
    private readonly IBomVersionRepository _bomVersionRepository;
    private readonly IComponentSuggestedSellingPriceVersionRepository _componentPriceRepository;
    private readonly IProductSuggestedPriceVersionRepository _productPriceRepository;

    public ProductPricingContextAppService(
        IProductRepository productRepository,
        IBomVersionRepository bomVersionRepository,
        IComponentSuggestedSellingPriceVersionRepository componentPriceRepository,
        IProductSuggestedPriceVersionRepository productPriceRepository)
    {
        _productRepository = productRepository;
        _bomVersionRepository = bomVersionRepository;
        _componentPriceRepository = componentPriceRepository;
        _productPriceRepository = productPriceRepository;
    }

    public async Task<List<ProductPricingContextDto>> GetListAsync()
    {
        var now = Clock.Now;
        var products = (await _productRepository.GetListAsync())
            .OrderBy(x => x.Code)
            .ThenBy(x => x.Name)
            .ToList();
        var result = new List<ProductPricingContextDto>(products.Count);

        foreach (var product in products)
        {
            var dto = new ProductPricingContextDto
            {
                ProductId = product.Id,
                ProductCode = product.Code,
                ProductName = product.Name
            };

            var productPrice = await _productPriceRepository.FindAtDateAsync(product.Id, now);
            dto.CurrentProductSuggestedPrice = productPrice?.Price.Amount;

            var publishedBom = (await _bomVersionRepository.GetListByProductIdAsync(product.Id))
                .FirstOrDefault(x => x.Status == BomStatus.Published);

            if (publishedBom == null)
            {
                result.Add(dto);
                continue;
            }

            dto.HasPublishedBom = true;
            var componentBuildPrice = 0m;
            foreach (var item in publishedBom.Items)
            {
                var componentPrice = await _componentPriceRepository.FindAtDateAsync(item.ComponentId, now);
                if (componentPrice == null)
                {
                    dto.HasMissingComponentSuggestedPrices = true;
                    continue;
                }

                componentBuildPrice += item.Quantity * componentPrice.Price.Amount;
            }

            if (!dto.HasMissingComponentSuggestedPrices)
            {
                dto.ComponentBuildPrice = componentBuildPrice;
            }

            if (dto.ComponentBuildPrice.HasValue && dto.CurrentProductSuggestedPrice.HasValue)
            {
                dto.Difference = dto.CurrentProductSuggestedPrice.Value - dto.ComponentBuildPrice.Value;
            }

            result.Add(dto);
        }

        return result;
    }
}
