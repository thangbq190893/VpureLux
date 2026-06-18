using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Services;
using Volo.Abp.Guids;

namespace VPureLux.Customers;

public class CustomerGroupManager : DomainService
{
    private readonly ICustomerGroupRepository _customerGroupRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IGuidGenerator _guidGenerator;

    public CustomerGroupManager(
        ICustomerGroupRepository customerGroupRepository,
        ICustomerRepository customerRepository,
        IGuidGenerator guidGenerator)
    {
        _customerGroupRepository = customerGroupRepository;
        _customerRepository = customerRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task<CustomerGroup> CreateAsync(
        string code,
        string name,
        string? description = null,
        int sortOrder = 0)
    {
        code = CustomerGroup.NormalizeCode(code);
        if (await _customerGroupRepository.CodeExistsAsync(code))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.CustomerGroupCodeAlreadyExists)
                .WithData(nameof(code), code);
        }

        return new CustomerGroup(_guidGenerator.Create(), code, name, description, sortOrder);
    }

    public async Task EnsureCanDeleteAsync(CustomerGroup customerGroup)
    {
        Check.NotNull(customerGroup, nameof(customerGroup));

        if (await _customerRepository.HasCustomersInGroupAsync(customerGroup.Id))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.CustomerGroupIsInUse)
                .WithData(nameof(customerGroup.Id), customerGroup.Id);
        }
    }
}
