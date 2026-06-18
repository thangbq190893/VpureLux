using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Services;
using Volo.Abp.Guids;

namespace VPureLux.Customers;

public class CustomerManager : DomainService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerGroupRepository _customerGroupRepository;
    private readonly IGuidGenerator _guidGenerator;

    public CustomerManager(
        ICustomerRepository customerRepository,
        ICustomerGroupRepository customerGroupRepository,
        IGuidGenerator guidGenerator)
    {
        _customerRepository = customerRepository;
        _customerGroupRepository = customerGroupRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task<Customer> CreateAsync(
        string code,
        string name,
        Guid customerGroupId,
        string? phoneNumber = null,
        string? email = null,
        string? address = null,
        string? taxCode = null,
        string? notes = null)
    {
        code = Customer.NormalizeCode(code);
        await EnsureCodeIsUniqueAsync(code);
        await EnsureGroupIsActiveAsync(customerGroupId);

        return new Customer(
            _guidGenerator.Create(),
            code,
            name,
            customerGroupId,
            phoneNumber,
            email,
            address,
            taxCode,
            notes);
    }

    public async Task AssignGroupAsync(Customer customer, Guid customerGroupId)
    {
        Check.NotNull(customer, nameof(customer));
        await EnsureGroupIsActiveAsync(customerGroupId);
        customer.AssignGroup(customerGroupId);
    }

    public async Task EnsureGroupIsActiveAsync(Guid customerGroupId)
    {
        var customerGroup = await _customerGroupRepository.FindAsync(customerGroupId);
        if (customerGroup == null)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.CustomerGroupNotFound)
                .WithData(nameof(customerGroupId), customerGroupId);
        }

        if (customerGroup.Status != CustomerGroupStatus.Active)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.CustomerGroupInactive)
                .WithData(nameof(customerGroupId), customerGroupId);
        }
    }

    private async Task EnsureCodeIsUniqueAsync(string code)
    {
        if (await _customerRepository.CodeExistsAsync(code))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.CustomerCodeAlreadyExists)
                .WithData(nameof(code), code);
        }
    }
}
