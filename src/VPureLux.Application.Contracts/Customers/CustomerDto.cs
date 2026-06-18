using System;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Customers;

public class CustomerDto : EntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid CustomerGroupId { get; set; }
    public string CustomerGroupCode { get; set; } = string.Empty;
    public string CustomerGroupName { get; set; } = string.Empty;
    public CustomerStatus Status { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public string? Notes { get; set; }
}
