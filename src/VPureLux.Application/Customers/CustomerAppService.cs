using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace VPureLux.Customers;

[Authorize(VPureLuxPermissions.Customers.Default)]
public class CustomerAppService : ApplicationService, ICustomerAppService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerGroupRepository _customerGroupRepository;
    private readonly CustomerManager _customerManager;
    private readonly CustomerApplicationMapper _mapper;

    public CustomerAppService(
        ICustomerRepository customerRepository,
        ICustomerGroupRepository customerGroupRepository,
        CustomerManager customerManager,
        CustomerApplicationMapper mapper)
    {
        _customerRepository = customerRepository;
        _customerGroupRepository = customerGroupRepository;
        _customerManager = customerManager;
        _mapper = mapper;
    }

    [Authorize(VPureLuxPermissions.Customers.View)]
    public async Task<PagedResultDto<CustomerDto>> GetListAsync(GetCustomerListInput input)
    {
        var totalCount = await _customerRepository.GetCountAsync(
            input.SearchText,
            input.CustomerGroupId,
            input.Status);

        var customers = await _customerRepository.GetListAsync(
            input.SearchText,
            input.CustomerGroupId,
            input.Status,
            input.Sorting,
            input.MaxResultCount,
            input.SkipCount);

        var customerGroups = await GetCustomerGroupsAsync(customers.Select(x => x.CustomerGroupId));
        return new PagedResultDto<CustomerDto>(
            totalCount,
            customers.Select(x => _mapper.ToDto(x, customerGroups[x.CustomerGroupId])).ToList());
    }

    [Authorize(VPureLuxPermissions.Customers.View)]
    public async Task<CustomerDto> GetAsync(Guid id)
    {
        var customer = await GetCustomerAsync(id);
        var customerGroup = await GetCustomerGroupAsync(customer.CustomerGroupId);
        return _mapper.ToDto(customer, customerGroup);
    }

    [Authorize(VPureLuxPermissions.Customers.Create)]
    public async Task<CustomerDto> CreateAsync(CreateCustomerDto input)
    {
        var customer = await _customerManager.CreateAsync(
            input.Code,
            input.Name,
            input.CustomerGroupId,
            input.PhoneNumber,
            input.Email,
            input.Address,
            input.TaxCode,
            input.Notes);

        await _customerRepository.InsertAsync(customer, autoSave: true);
        return _mapper.ToDto(customer, await GetCustomerGroupAsync(customer.CustomerGroupId));
    }

    [Authorize(VPureLuxPermissions.Customers.Edit)]
    public async Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerDto input)
    {
        var customer = await GetCustomerAsync(id);
        await _customerManager.AssignGroupAsync(customer, input.CustomerGroupId);
        customer.UpdateInfo(
            input.Name,
            input.PhoneNumber,
            input.Email,
            input.Address,
            input.TaxCode,
            input.Notes);

        await _customerRepository.UpdateAsync(customer, autoSave: true);
        return _mapper.ToDto(customer, await GetCustomerGroupAsync(customer.CustomerGroupId));
    }

    [Authorize(VPureLuxPermissions.Customers.ManageStatus)]
    public async Task ActivateAsync(Guid id)
    {
        var customer = await GetCustomerAsync(id);
        await _customerManager.EnsureGroupIsActiveAsync(customer.CustomerGroupId);
        customer.Activate();
        await _customerRepository.UpdateAsync(customer, autoSave: true);
    }

    [Authorize(VPureLuxPermissions.Customers.ManageStatus)]
    public async Task DeactivateAsync(Guid id)
    {
        var customer = await GetCustomerAsync(id);
        customer.Deactivate();
        await _customerRepository.UpdateAsync(customer, autoSave: true);
    }

    private async Task<Customer> GetCustomerAsync(Guid id)
    {
        var customer = await _customerRepository.FindAsync(id);
        if (customer == null)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.CustomerNotFound)
                .WithData(nameof(id), id);
        }

        return customer;
    }

    private async Task<CustomerGroup> GetCustomerGroupAsync(Guid id)
    {
        var customerGroup = await _customerGroupRepository.FindAsync(id);
        if (customerGroup == null)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.CustomerGroupNotFound)
                .WithData(nameof(id), id);
        }

        return customerGroup;
    }

    private async Task<IReadOnlyDictionary<Guid, CustomerGroup>> GetCustomerGroupsAsync(IEnumerable<Guid> ids)
    {
        var idSet = ids.Distinct().ToHashSet();
        var queryable = await _customerGroupRepository.GetQueryableAsync();
        var customerGroups = await AsyncExecuter.ToListAsync(queryable.Where(x => idSet.Contains(x.Id)));

        var result = customerGroups.ToDictionary(x => x.Id);
        if (result.Count != idSet.Count)
        {
            var missingId = idSet.First(id => !result.ContainsKey(id));
            throw new BusinessException(VPureLuxDomainErrorCodes.CustomerGroupNotFound)
                .WithData(nameof(missingId), missingId);
        }

        return result;
    }
}
