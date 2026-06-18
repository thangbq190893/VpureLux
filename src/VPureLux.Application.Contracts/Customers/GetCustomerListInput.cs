using System;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Customers;

public class GetCustomerListInput : PagedAndSortedResultRequestDto
{
    public string? SearchText { get; set; }
    public Guid? CustomerGroupId { get; set; }
    public CustomerStatus? Status { get; set; }
}
