using System.ComponentModel.DataAnnotations;

namespace VPureLux.Catalog.Components;

public class UpdateComponentDto
{
    [Required]
    [StringLength(CatalogConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    [StringLength(CatalogConsts.MaxDescriptionLength)]
    public string? Description { get; set; }

    [Required]
    [StringLength(CatalogConsts.MaxUnitLength)]
    public string Unit { get; set; } = string.Empty;
}
