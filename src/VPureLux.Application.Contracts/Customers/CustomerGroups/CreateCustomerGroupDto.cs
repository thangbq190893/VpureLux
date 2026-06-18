using System.ComponentModel.DataAnnotations;

namespace VPureLux.Customers.CustomerGroups;

public class CreateCustomerGroupDto
{
    [Required]
    [StringLength(CustomerGroupConsts.MaxCodeLength)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(CustomerGroupConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    [StringLength(CustomerGroupConsts.MaxDescriptionLength)]
    public string? Description { get; set; }

    [Range(0, int.MaxValue)]
    public int SortOrder { get; set; }
}
