using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace VPureLux.Sales;

[Route("api/sales/orders")]
public class SalesOrderController : AbpControllerBase
{
    private readonly ISalesOrderAppService _appService;

    public SalesOrderController(ISalesOrderAppService appService) => _appService = appService;

    [HttpGet]
    public Task<PagedResultDto<SalesOrderDto>> GetListAsync([FromQuery] GetSalesOrderListInput input) =>
        _appService.GetListAsync(input);

    [HttpGet("{id:guid}")]
    public Task<SalesOrderDto> GetAsync(Guid id) => _appService.GetAsync(id);

    [HttpPost]
    public Task<SalesOrderDto> CreateAsync([FromBody] CreateSalesOrderDto input) => _appService.CreateAsync(input);

    [HttpPost("{id:guid}/lines")]
    public Task<SalesOrderDto> AddLineAsync(Guid id, [FromBody] CreateSalesOrderLineDto input) =>
        _appService.AddLineAsync(id, input);

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public Task<SalesOrderDto> UpdateLineAsync(Guid id, Guid lineId, [FromBody] UpdateSalesOrderLineDto input) =>
        _appService.UpdateLineAsync(id, lineId, input);

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public Task<SalesOrderDto> RemoveLineAsync(Guid id, Guid lineId) => _appService.RemoveLineAsync(id, lineId);

    [HttpPost("{id:guid}/confirm")]
    public Task<ConfirmSalesOrderResultDto> ConfirmAsync(Guid id, [FromBody] ConfirmSalesOrderDto input) =>
        _appService.ConfirmAsync(id, input);

    [HttpPost("{id:guid}/cancel")]
    public Task CancelAsync(Guid id) => _appService.CancelAsync(id);

    [HttpGet("/api/sales/customers/{customerId:guid}/purchase-history")]
    public Task<List<CustomerPurchaseHistoryDto>> GetCustomerHistoryAsync(Guid customerId) =>
        _appService.GetCustomerHistoryAsync(customerId);
}
