using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace VPureLux.Customers;

public interface ICustomerAppService : IApplicationService
{
    Task<PagedResultDto<CustomerDto>> GetListAsync(GetCustomerListInput input);

    Task<CustomerDto> GetAsync(Guid id);

    Task<CustomerDto> CreateAsync(CreateCustomerDto input);

    Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerDto input);

    Task ActivateAsync(Guid id);

    Task DeactivateAsync(Guid id);
}
