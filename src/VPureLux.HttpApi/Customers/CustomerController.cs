using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace VPureLux.Customers;

[Route("api/customers")]
public class CustomerController : AbpControllerBase
{
    private readonly ICustomerAppService _customerAppService;

    public CustomerController(ICustomerAppService customerAppService)
    {
        _customerAppService = customerAppService;
    }

    [HttpGet]
    public Task<PagedResultDto<CustomerDto>> GetListAsync([FromQuery] GetCustomerListInput input) =>
        _customerAppService.GetListAsync(input);

    [HttpGet("{id:guid}")]
    public Task<CustomerDto> GetAsync(Guid id) => _customerAppService.GetAsync(id);

    [HttpPost]
    public Task<CustomerDto> CreateAsync([FromBody] CreateCustomerDto input) => _customerAppService.CreateAsync(input);

    [HttpPut("{id:guid}")]
    public Task<CustomerDto> UpdateAsync(Guid id, [FromBody] UpdateCustomerDto input) =>
        _customerAppService.UpdateAsync(id, input);

    [HttpPost("{id:guid}/activate")]
    public Task ActivateAsync(Guid id) => _customerAppService.ActivateAsync(id);

    [HttpPost("{id:guid}/deactivate")]
    public Task DeactivateAsync(Guid id) => _customerAppService.DeactivateAsync(id);
}
