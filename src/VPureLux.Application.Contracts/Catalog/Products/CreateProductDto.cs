using System.ComponentModel.DataAnnotations;

namespace VPureLux.Catalog.Products;

public class CreateProductDto
{
    [Required(ErrorMessage = "Vui lòng nhập mã sản phẩm.")]
    [StringLength(CatalogConsts.MaxCodeLength)]
    public string? Code { get; set; }

    [Required]
    [StringLength(CatalogConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    [StringLength(CatalogConsts.MaxDescriptionLength)]
    public string? Description { get; set; }
}
