using System;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Customers.CustomerGroups;

public class CustomerGroupDto : EntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CustomerGroupStatus Status { get; set; }
    public int SortOrder { get; set; }
}
