using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Customers.CustomerGroups;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace VPureLux.Customers;

[Route("api/customer-groups")]
public class CustomerGroupController : AbpControllerBase
{
    private readonly ICustomerGroupAppService _customerGroupAppService;

    public CustomerGroupController(ICustomerGroupAppService customerGroupAppService)
    {
        _customerGroupAppService = customerGroupAppService;
    }

    [HttpGet]
    public Task<PagedResultDto<CustomerGroupDto>> GetListAsync([FromQuery] GetCustomerGroupListInput input) =>
        _customerGroupAppService.GetListAsync(input);

    [HttpGet("{id:guid}")]
    public Task<CustomerGroupDto> GetAsync(Guid id) => _customerGroupAppService.GetAsync(id);

    [HttpPost]
    public Task<CustomerGroupDto> CreateAsync([FromBody] CreateCustomerGroupDto input) =>
        _customerGroupAppService.CreateAsync(input);

    [HttpPut("{id:guid}")]
    public Task<CustomerGroupDto> UpdateAsync(Guid id, [FromBody] UpdateCustomerGroupDto input) =>
        _customerGroupAppService.UpdateAsync(id, input);

    [HttpPost("{id:guid}/activate")]
    public Task ActivateAsync(Guid id) => _customerGroupAppService.ActivateAsync(id);

    [HttpPost("{id:guid}/deactivate")]
    public Task DeactivateAsync(Guid id) => _customerGroupAppService.DeactivateAsync(id);
}
