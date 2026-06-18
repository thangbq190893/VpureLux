using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Customers.CustomerGroups;
using VPureLux.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace VPureLux.Customers;

[Authorize(VPureLuxPermissions.CustomerGroups.Default)]
public class CustomerGroupAppService : ApplicationService, ICustomerGroupAppService
{
    private readonly ICustomerGroupRepository _customerGroupRepository;
    private readonly CustomerGroupManager _customerGroupManager;
    private readonly CustomerApplicationMapper _mapper;

    public CustomerGroupAppService(
        ICustomerGroupRepository customerGroupRepository,
        CustomerGroupManager customerGroupManager,
        CustomerApplicationMapper mapper)
    {
        _customerGroupRepository = customerGroupRepository;
        _customerGroupManager = customerGroupManager;
        _mapper = mapper;
    }

    [Authorize(VPureLuxPermissions.CustomerGroups.View)]
    public async Task<PagedResultDto<CustomerGroupDto>> GetListAsync(GetCustomerGroupListInput input)
    {
        var totalCount = await _customerGroupRepository.GetCountAsync(input.SearchText, input.Status);
        var customerGroups = await _customerGroupRepository.GetListAsync(
            input.SearchText,
            input.Status,
            input.Sorting,
            input.MaxResultCount,
            input.SkipCount);

        return new PagedResultDto<CustomerGroupDto>(
            totalCount,
            customerGroups.Select(_mapper.ToDto).ToList());
    }

    [Authorize(VPureLuxPermissions.CustomerGroups.View)]
    public async Task<CustomerGroupDto> GetAsync(Guid id)
    {
        return _mapper.ToDto(await GetCustomerGroupAsync(id));
    }

    [Authorize(VPureLuxPermissions.CustomerGroups.Create)]
    public async Task<CustomerGroupDto> CreateAsync(CreateCustomerGroupDto input)
    {
        var customerGroup = await _customerGroupManager.CreateAsync(
            input.Code,
            input.Name,
            input.Description,
            input.SortOrder);

        await _customerGroupRepository.InsertAsync(customerGroup, autoSave: true);
        return _mapper.ToDto(customerGroup);
    }

    [Authorize(VPureLuxPermissions.CustomerGroups.Edit)]
    public async Task<CustomerGroupDto> UpdateAsync(Guid id, UpdateCustomerGroupDto input)
    {
        var customerGroup = await GetCustomerGroupAsync(id);
        customerGroup.UpdateInfo(input.Name, input.Description, input.SortOrder);
        await _customerGroupRepository.UpdateAsync(customerGroup, autoSave: true);
        return _mapper.ToDto(customerGroup);
    }

    [Authorize(VPureLuxPermissions.CustomerGroups.ManageStatus)]
    public async Task ActivateAsync(Guid id)
    {
        var customerGroup = await GetCustomerGroupAsync(id);
        customerGroup.Activate();
        await _customerGroupRepository.UpdateAsync(customerGroup, autoSave: true);
    }

    [Authorize(VPureLuxPermissions.CustomerGroups.ManageStatus)]
    public async Task DeactivateAsync(Guid id)
    {
        var customerGroup = await GetCustomerGroupAsync(id);
        customerGroup.Deactivate();
        await _customerGroupRepository.UpdateAsync(customerGroup, autoSave: true);
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
}
