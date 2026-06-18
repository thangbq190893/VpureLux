using System;
using System.ComponentModel.DataAnnotations;

namespace VPureLux.Customers;

public class CreateCustomerDto
{
    [Required]
    [StringLength(CustomerConsts.MaxCodeLength)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(CustomerConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public Guid CustomerGroupId { get; set; }

    [StringLength(CustomerConsts.MaxPhoneNumberLength)]
    public string? PhoneNumber { get; set; }

    [EmailAddress]
    [StringLength(CustomerConsts.MaxEmailLength)]
    public string? Email { get; set; }

    [StringLength(CustomerConsts.MaxAddressLength)]
    public string? Address { get; set; }

    [StringLength(CustomerConsts.MaxTaxCodeLength)]
    public string? TaxCode { get; set; }

    [StringLength(CustomerConsts.MaxNotesLength)]
    public string? Notes { get; set; }
}
