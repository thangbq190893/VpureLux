using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace VPureLux.Sales;

public interface ISalesOrderAppService : IApplicationService
{
    Task<PagedResultDto<SalesOrderDto>> GetListAsync(GetSalesOrderListInput input);
    Task<SalesOrderDto> GetAsync(Guid id);
    Task<SalesOrderDto> CreateAsync(CreateSalesOrderDto input);
    Task<SalesOrderDto> AddLineAsync(Guid id, CreateSalesOrderLineDto input);
    Task<SalesOrderDto> UpdateLineAsync(Guid id, Guid lineId, UpdateSalesOrderLineDto input);
    Task<SalesOrderDto> RemoveLineAsync(Guid id, Guid lineId);
    Task<ConfirmSalesOrderResultDto> ConfirmAsync(Guid id, ConfirmSalesOrderDto input);
    Task CancelAsync(Guid id);
    Task<List<CustomerPurchaseHistoryDto>> GetCustomerHistoryAsync(Guid customerId);
}
