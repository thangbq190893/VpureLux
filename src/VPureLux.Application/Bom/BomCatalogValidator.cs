using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VPureLux.Catalog;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace VPureLux.Bom;

public class BomCatalogValidator : ITransientDependency
{
    private readonly IProductRepository _productRepository;
    private readonly IComponentRepository _componentRepository;

    public BomCatalogValidator(
        IProductRepository productRepository,
        IComponentRepository componentRepository)
    {
        _productRepository = productRepository;
        _componentRepository = componentRepository;
    }

    public async Task ValidateActiveProductAsync(Guid productId)
    {
        var product = await GetProductAsync(productId);
        if (product.Status != CatalogItemStatus.Active)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed)
                .WithData(nameof(productId), productId)
                .WithData("Reason", "Product must be active.");
        }
    }

    public async Task ValidateProductExistsAsync(Guid productId)
    {
        await GetProductAsync(productId);
    }

    private async Task<Product> GetProductAsync(Guid productId)
    {
        var product = await _productRepository.FindAsync(productId);
        if (product == null)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ProductNotFound)
                .WithData(nameof(productId), productId);
        }

        return product;
    }

    public async Task ValidateActiveComponentsAsync(IEnumerable<Guid> componentIds)
    {
        var ids = componentIds.Distinct().ToList();
        var components = await _componentRepository.GetListAsync(x => ids.Contains(x.Id));
        var componentsById = components.ToDictionary(x => x.Id);

        foreach (var componentId in ids)
        {
            if (!componentsById.TryGetValue(componentId, out var component))
            {
                throw new BusinessException(VPureLuxDomainErrorCodes.ComponentNotFound)
                    .WithData(nameof(componentId), componentId);
            }

            if (component.Status != CatalogItemStatus.Active)
            {
                throw new BusinessException(VPureLuxDomainErrorCodes.ComponentNotActive)
                    .WithData(nameof(componentId), componentId);
            }
        }
    }
}
