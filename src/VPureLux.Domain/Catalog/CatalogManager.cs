using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Services;
using Volo.Abp.Guids;

namespace VPureLux.Catalog;

public class CatalogManager : DomainService
{
    private readonly IComponentRepository _componentRepository;
    private readonly IProductRepository _productRepository;
    private readonly IGuidGenerator _guidGenerator;

    public CatalogManager(
        IComponentRepository componentRepository,
        IProductRepository productRepository,
        IGuidGenerator guidGenerator)
    {
        _componentRepository = componentRepository;
        _productRepository = productRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task<Component> CreateComponentAsync(string code, string name, string? description, string unit)
    {
        if (await _componentRepository.CodeExistsAsync(code))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ComponentCodeAlreadyExists)
                .WithData("Code", code);
        }

        return new Component(_guidGenerator.Create(), code, name, description, unit);
    }

    public async Task UpdateComponentAsync(Component component, string name, string? description, string unit)
    {
        await Task.CompletedTask;
        component.UpdateInfo(name, description, unit);
    }

    public async Task<Product> CreateProductAsync(string code, string name, string? description)
    {
        if (await _productRepository.CodeExistsAsync(code))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ProductCodeAlreadyExists)
                .WithData("Code", code);
        }

        return new Product(_guidGenerator.Create(), code, name, description);
    }

    public async Task UpdateProductAsync(Product product, string name, string? description)
    {
        await Task.CompletedTask;
        product.UpdateInfo(name, description);
    }
}
