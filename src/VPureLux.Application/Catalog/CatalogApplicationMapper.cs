using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using Volo.Abp.DependencyInjection;

namespace VPureLux.Catalog;

public class CatalogApplicationMapper : ITransientDependency
{
    public ComponentDto ToDto(Component component) => new()
    {
        Id = component.Id,
        Code = component.Code,
        Name = component.Name,
        Description = component.Description,
        Unit = component.Unit,
        Status = component.Status,
        HasImage = component.Image != null,
        ImageHash = component.Image?.ImageHash
    };

    public ProductDto ToDto(Product product) => new()
    {
        Id = product.Id,
        Code = product.Code,
        Name = product.Name,
        Description = product.Description,
        Status = product.Status,
        HasImage = product.Image != null,
        ImageHash = product.Image?.ImageHash
    };
}
