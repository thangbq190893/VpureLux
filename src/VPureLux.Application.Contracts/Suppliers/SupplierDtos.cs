using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Suppliers;

public class SupplierDto : EntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? TaxCode { get; set; }
    public string? Address { get; set; }
    public string? Note { get; set; }
    public DateTime CreationTime { get; set; }
}

public class UpdateSupplierDto
{
    [Required, StringLength(SupplierConsts.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    [StringLength(SupplierConsts.MaxContactNameLength)]
    public string? ContactName { get; set; }

    [StringLength(SupplierConsts.MaxPhoneLength)]
    public string? Phone { get; set; }

    [EmailAddress, StringLength(SupplierConsts.MaxEmailLength)]
    public string? Email { get; set; }

    [StringLength(SupplierConsts.MaxTaxCodeLength)]
    public string? TaxCode { get; set; }

    [StringLength(SupplierConsts.MaxAddressLength)]
    public string? Address { get; set; }

    [StringLength(SupplierConsts.MaxNoteLength)]
    public string? Note { get; set; }
}

public class CreateSupplierDto : UpdateSupplierDto
{
    [Required, StringLength(SupplierConsts.MaxCodeLength)]
    public string Code { get; set; } = string.Empty;
}

public class GetSupplierListInput : PagedAndSortedResultRequestDto
{
    public string? SearchText { get; set; }
}
