using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace VPureLux.Customers.CustomerGroups;

public interface ICustomerGroupAppService : IApplicationService
{
    Task<PagedResultDto<CustomerGroupDto>> GetListAsync(GetCustomerGroupListInput input);

    Task<CustomerGroupDto> GetAsync(Guid id);

    Task<CustomerGroupDto> CreateAsync(CreateCustomerGroupDto input);

    Task<CustomerGroupDto> UpdateAsync(Guid id, UpdateCustomerGroupDto input);

    Task ActivateAsync(Guid id);

    Task DeactivateAsync(Guid id);
}
