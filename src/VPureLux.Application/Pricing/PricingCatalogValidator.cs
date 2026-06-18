using System;
using System.Threading.Tasks;
using VPureLux.Catalog;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace VPureLux.Pricing;

public class PricingCatalogValidator : ITransientDependency
{
    private readonly IComponentRepository _componentRepository;
    private readonly IProductRepository _productRepository;

    public PricingCatalogValidator(
        IComponentRepository componentRepository,
        IProductRepository productRepository)
    {
        _componentRepository = componentRepository;
        _productRepository = productRepository;
    }

    public async Task ValidateActiveComponentAsync(Guid componentId)
    {
        var component = await GetComponentAsync(componentId);

        if (component.Status != CatalogItemStatus.Active)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ComponentNotActive)
                .WithData(nameof(componentId), componentId);
        }
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

    public async Task ValidateComponentExistsAsync(Guid componentId)
    {
        await GetComponentAsync(componentId);
    }

    public async Task ValidateProductExistsAsync(Guid productId)
    {
        await GetProductAsync(productId);
    }

    private async Task<Component> GetComponentAsync(Guid componentId)
    {
        var component = await _componentRepository.FindAsync(componentId);
        if (component == null)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ComponentNotFound)
                .WithData(nameof(componentId), componentId);
        }

        return component;
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
}
