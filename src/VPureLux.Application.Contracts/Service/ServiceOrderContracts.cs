using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace VPureLux.Service;

public interface IServiceOrderAppService : IApplicationService
{
    Task<PagedResultDto<ServiceOrderListDto>> GetListAsync(GetServiceOrderListInput input);
    Task<ServiceOrderDto> GetAsync(Guid id);
    Task<ServiceOrderDto> CreateAsync(CreateServiceOrderDto input);
    Task<ServiceOrderDto> UpdateAsync(Guid id, UpdateServiceOrderDto input);
    Task<ServiceOrderDto> ConfirmAsync(Guid id, ServiceOrderTransitionDto input);
    Task<ServiceOrderDto> StartAsync(Guid id, ServiceOrderTransitionDto input);
    Task<ServiceOrderDto> CancelAsync(Guid id, CancelServiceOrderDto input);
    Task<ServiceCompletionResultDto> CompleteAsync(Guid id, CompleteServiceOrderDto input);
    Task<PagedResultDto<ServiceAssetOptionDto>> GetAssetOptionsAsync(ServiceLookupInput input);
    Task<ServiceAssetOptionDto> GetAssetOptionAsync(Guid id);
    Task<List<ServiceAssetPositionOptionDto>> GetAssetPositionOptionsAsync(Guid customerAssetId);
    Task<PagedResultDto<ServiceMaterialOptionDto>> GetMaterialOptionsAsync(ServiceLookupInput input);
    Task<PagedResultDto<ServiceWorkOptionDto>> GetWorkOptionsAsync(ServiceLookupInput input);
    Task<PagedResultDto<ServiceTechnicianOptionDto>> GetTechnicianOptionsAsync(ServiceLookupInput input);
    Task<List<ServiceWarehouseOptionDto>> GetWarehouseOptionsAsync();
}

public class GetServiceOrderListInput : PagedAndSortedResultRequestDto
{
    [StringLength(256)] public string? SearchText { get; set; }
    public Guid? CustomerId { get; set; }
    public ServiceOrderStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class ServiceLookupInput : PagedResultRequestDto
{
    [StringLength(256)] public string? SearchText { get; set; }
    public Guid? CustomerId { get; set; }
}

public class ServiceOrderLineInput
{
    public Guid? Id { get; set; }
    [EnumDataType(typeof(ServiceOrderLineType))] public ServiceOrderLineType LineType { get; set; }
    public Guid CatalogItemId { get; set; }
    public Guid? CustomerAssetComponentId { get; set; }
    [Range(1, 100000)] public int Quantity { get; set; } = 1;
    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal UnitPrice { get; set; }
    [StringLength(ServiceConsts.MaxNoteLength)] public string? Note { get; set; }
}

public class CreateServiceOrderDto
{
    public Guid CustomerAssetId { get; set; }
    public Guid WarehouseId { get; set; }
    public DateTime? OrderDate { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public Guid? TechnicianUserId { get; set; }
    [StringLength(ServiceConsts.MaxAddressLength)] public string? ServiceAddress { get; set; }
    [StringLength(ServiceConsts.MaxNoteLength)] public string? Note { get; set; }
    [Required, MinLength(1)] public List<ServiceOrderLineInput> Lines { get; set; } = [];
}

public class UpdateServiceOrderDto
{
    public DateTime OrderDate { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public Guid? TechnicianUserId { get; set; }
    [StringLength(ServiceConsts.MaxAddressLength)] public string? ServiceAddress { get; set; }
    [StringLength(ServiceConsts.MaxNoteLength)] public string? Note { get; set; }
    [Required, MinLength(1)] public List<ServiceOrderLineInput> Lines { get; set; } = [];
    [Required, StringLength(40)] public string ConcurrencyStamp { get; set; } = string.Empty;
}

public class ServiceOrderTransitionDto
{
    [Required, StringLength(40)] public string ConcurrencyStamp { get; set; } = string.Empty;
}

public class CancelServiceOrderDto : ServiceOrderTransitionDto
{
    [Required, StringLength(ServiceConsts.MaxNoteLength)] public string Reason { get; set; } = string.Empty;
}

public class CompleteServiceOrderDto : ServiceOrderTransitionDto
{
    [Required, StringLength(64)] public string IdempotencyKey { get; set; } = string.Empty;
    public DateTimeOffset CompletedAt { get; set; }
    [Required, MinLength(1)] public List<CompleteServiceLineDto> Lines { get; set; } = [];
}

public class CompleteServiceLineDto
{
    public Guid LineId { get; set; }
    [Range(0, 100000)] public int ActualQuantity { get; set; }
}

public class ServiceCompletionResultDto
{
    public Guid ServiceOrderId { get; set; }
    public Guid? InventoryTransactionId { get; set; }
    public DateTime CompletedAt { get; set; }
    public decimal Revenue { get; set; }
    public decimal? ActualCost { get; set; }
    public decimal? ActualProfit { get; set; }
    public List<CompleteServiceLineDto> Lines { get; set; } = [];
}

public class ServiceOrderListDto : EntityDto<Guid>
{
    public string OrderNo { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public ServiceOrderStatus Status { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public Guid CustomerAssetId { get; set; }
    public string AssetNo { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public decimal PlannedAmount { get; set; }
    public int LineCount { get; set; }
}

public class ServiceOrderLineDto : EntityDto<Guid>
{
    public string? PositionName { get; set; }
    public int LineNo { get; set; }
    public ServiceOrderLineType LineType { get; set; }
    public Guid? ComponentId { get; set; }
    public Guid? CustomerAssetComponentId { get; set; }
    public Guid? ServiceWorkId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int PlannedQuantity { get; set; }
    public int ActualQuantity { get; set; }
    public decimal? ActualCostAmount { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? StandardCostSnapshot { get; set; }
    public string? Note { get; set; }
}

public class ServiceOrderDto : EntityDto<Guid>
{
    public decimal? ActualRevenueAmount { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid CustomerAssetId { get; set; }
    public Guid WarehouseId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public Guid? TechnicianUserId { get; set; }
    public string? TechnicianName { get; set; }
    public ServiceOrderStatus Status { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string AssetNo { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string? ServiceAddress { get; set; }
    public string? Note { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public decimal PlannedAmount { get; set; }
    public string ConcurrencyStamp { get; set; } = string.Empty;
    public List<ServiceOrderLineDto> Lines { get; set; } = [];
}

public class ServiceAssetOptionDto : EntityDto<Guid>
{
    public Guid CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string AssetNo { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string? ServiceAddress { get; set; }
}

public class ServiceAssetPositionOptionDto : EntityDto<Guid>
{
    public string PositionCode { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public Guid? ComponentId { get; set; }
    public string? ComponentCode { get; set; }
    public string? ComponentName { get; set; }
}

public class ServiceMaterialOptionDto : EntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal? SuggestedPrice { get; set; }
}

public class ServiceWorkOptionDto : EntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal DefaultPrice { get; set; }
    public decimal? StandardCost { get; set; }
}

public class ServiceTechnicianOptionDto : EntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
}

public class ServiceWarehouseOptionDto : EntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
