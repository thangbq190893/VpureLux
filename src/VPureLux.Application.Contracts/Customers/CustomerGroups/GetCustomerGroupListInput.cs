using Volo.Abp.Application.Dtos;

namespace VPureLux.Customers.CustomerGroups;

public class GetCustomerGroupListInput : PagedAndSortedResultRequestDto
{
    public string? SearchText { get; set; }
    public CustomerGroupStatus? Status { get; set; }
}
