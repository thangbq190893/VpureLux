using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace VPureLux.Service;

public interface IServiceAppService : IApplicationService
{
    Task<PagedResultDto<ServiceOrderListDto>> GetListAsync(GetServiceOrderListInput input);
    Task<ServiceOrderDto> GetAsync(Guid id);
    Task<ServiceOrderDto> CreateAsync(CreateServiceOrderDto input);
    Task<ServiceOrderDto> UpdateAsync(Guid id, UpdateServiceOrderDto input);
    Task<ServiceOrderDto> ConfirmAsync(Guid id);
    Task<ServiceOrderDto> StartAsync(Guid id);
    Task<ServiceOrderDto> CompleteAsync(Guid id, CompleteServiceOrderDto input);
    Task CancelAsync(Guid id);
    Task<ServicePaymentDto> AddPaymentAsync(Guid id, CreateServicePaymentDto input);
    Task VoidPaymentAsync(Guid id, Guid paymentId);
    Task<List<ServicePaymentDto>> GetPaymentsAsync(Guid id);
    Task<List<ServiceAssetOptionDto>> GetAssetOptionsAsync(ServiceLookupInput input);
    Task<ServiceAssetOptionDto> GetAssetOptionAsync(Guid id);
    Task<List<ServiceAssetPositionOptionDto>> GetAssetPositionOptionsAsync(Guid customerAssetId);
    Task<List<ServiceMaterialOptionDto>> GetMaterialOptionsAsync(ServiceLookupInput input);
    Task<List<ServiceWorkOptionDto>> GetWorkOptionsAsync(ServiceLookupInput input);
    Task<List<ServiceWarehouseOptionDto>> GetWarehouseOptionsAsync();
}

public interface IServiceWorkAppService : IApplicationService
{
    Task<PagedResultDto<ServiceWorkDto>> GetListAsync(GetServiceWorkListInput input);
    Task<ServiceWorkDto> GetAsync(Guid id);
    Task<ServiceWorkDto> CreateAsync(CreateUpdateServiceWorkDto input);
    Task<ServiceWorkDto> UpdateAsync(Guid id, CreateUpdateServiceWorkDto input);
    Task SetActiveAsync(Guid id, bool isActive);
}
