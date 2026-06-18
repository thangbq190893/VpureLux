using Riok.Mapperly.Abstractions;
using VPureLux.Customers.CustomerGroups;
using Volo.Abp.DependencyInjection;

namespace VPureLux.Customers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class CustomerApplicationMapper : ITransientDependency
{
    public partial CustomerGroupDto ToDto(CustomerGroup customerGroup);

    public CustomerDto ToDto(Customer customer, CustomerGroup customerGroup)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            Code = customer.Code,
            Name = customer.Name,
            CustomerGroupId = customer.CustomerGroupId,
            CustomerGroupCode = customerGroup.Code,
            CustomerGroupName = customerGroup.Name,
            Status = customer.Status,
            PhoneNumber = customer.PhoneNumber,
            Email = customer.Email,
            Address = customer.Address,
            TaxCode = customer.TaxCode,
            Notes = customer.Notes
        };
    }
}
