using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace VPureLux.Inventory;

[Route("api/inventory/stock-items")]
public class StockItemController : AbpControllerBase
{
    private readonly IStockItemAppService _appService;
    public StockItemController(IStockItemAppService appService) => _appService = appService;
    [HttpGet] public Task<PagedResultDto<StockItemDto>> GetListAsync([FromQuery] GetInventoryListInput input) => _appService.GetListAsync(input);
    [HttpGet("{id:guid}")] public Task<StockItemDto> GetAsync(Guid id) => _appService.GetAsync(id);
}
