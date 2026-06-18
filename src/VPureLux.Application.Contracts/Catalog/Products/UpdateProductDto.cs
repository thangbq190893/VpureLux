using System.ComponentModel.DataAnnotations;

namespace VPureLux.Catalog.Products;

public class UpdateProductDto
{
    [Required]
    [StringLength(CatalogConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    [StringLength(CatalogConsts.MaxDescriptionLength)]
    public string? Description { get; set; }
}
