using System.ComponentModel.DataAnnotations;
using VPureLux.Warranty;

namespace VPureLux.Catalog.Components;

public class CreateComponentDto
{
    [StringLength(CatalogConsts.MaxCodeLength)]
    public string? Code { get; set; }

    [Required]
    [StringLength(CatalogConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    [StringLength(CatalogConsts.MaxDescriptionLength)]
    public string? Description { get; set; }

    [Required]
    [StringLength(CatalogConsts.MaxUnitLength)]
    public string Unit { get; set; } = string.Empty;

    public ComponentReplacementPolicyInputDto? ReplacementPolicy { get; set; }
}

public class ComponentReplacementPolicyInputDto
{
    [Display(Name = "Warranty:TrackedForReplacement")]
    public bool IsEnabled { get; set; }

    [Required]
    [Range(1, 120)]
    [Display(Name = "Warranty:CycleMonths")]
    public int CycleMonths { get; set; } = 3;

    [Required]
    [Range(0, 365)]
    [Display(Name = "Warranty:WarningDaysBeforeDue")]
    public int WarningDaysBeforeDue { get; set; } = 7;

    [StringLength(WarrantyConsts.MaxNoteLength)]
    [Display(Name = "Warranty:Note")]
    public string? Note { get; set; }
}
