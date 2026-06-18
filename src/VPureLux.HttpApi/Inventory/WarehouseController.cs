using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace VPureLux.Inventory;

[Route("api/inventory/warehouses")]
public class WarehouseController : AbpControllerBase
{
    private readonly IWarehouseAppService _appService;
    public WarehouseController(IWarehouseAppService appService) => _appService = appService;
    [HttpGet] public Task<PagedResultDto<WarehouseDto>> GetListAsync([FromQuery] GetInventoryListInput input) => _appService.GetListAsync(input);
    [HttpGet("{id:guid}")] public Task<WarehouseDto> GetAsync(Guid id) => _appService.GetAsync(id);
    [HttpPost] public Task<WarehouseDto> CreateAsync([FromBody] CreateWarehouseDto input) => _appService.CreateAsync(input);
    [HttpPut("{id:guid}")] public Task<WarehouseDto> UpdateAsync(Guid id, [FromBody] UpdateWarehouseDto input) => _appService.UpdateAsync(id, input);
    [HttpPost("{id:guid}/activate")] public Task ActivateAsync(Guid id) => _appService.ActivateAsync(id);
    [HttpPost("{id:guid}/deactivate")] public Task DeactivateAsync(Guid id) => _appService.DeactivateAsync(id);
}
