using System;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Catalog.Products;

public class ProductDto : EntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CatalogItemStatus Status { get; set; }
    public bool HasImage { get; set; }
    public string? ImageHash { get; set; }
}
