using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Service;

public class ServiceOrderListDto : EntityDto<Guid>
{
    public string OrderNo { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ServiceOrderStatus Status { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public Guid CustomerAssetId { get; set; }
    public string AssetNo { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public decimal TotalRevenueAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount => TotalRevenueAmount - PaidAmount;
    public int LineCount { get; set; }
}

public class ServiceOrderLineDto : EntityDto<Guid>
{
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
    public decimal UnitPrice { get; set; }
    public decimal RevenueAmount { get; set; }
    public decimal CostAmount { get; set; }
    public string? Note { get; set; }
}

public class ServiceOrderDto : EntityDto<Guid>
{
    public string OrderNo { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid CustomerAssetId { get; set; }
    public Guid WarehouseId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public Guid? TechnicianUserId { get; set; }
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
    public decimal TotalRevenueAmount { get; set; }
    public decimal TotalCostAmount { get; set; }
    public decimal TotalProfitAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public List<ServiceOrderLineDto> Lines { get; set; } = [];
}

public class ServicePaymentDto : EntityDto<Guid>
{
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public VPureLux.Sales.SalesPaymentMethod PaymentMethod { get; set; }
    public string ReferenceNo { get; set; } = string.Empty;
    public string? Note { get; set; }
    public ServicePaymentStatus Status { get; set; }
}

public class ServiceWorkDto : EntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal DefaultPrice { get; set; }
    public ServiceWorkStatus Status { get; set; }
    public string? Note { get; set; }
}

public class ServiceAssetOptionDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string AssetNo { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string? ServiceAddress { get; set; }
}

public class ServiceAssetPositionOptionDto
{
    public Guid Id { get; set; }
    public string PositionCode { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public Guid? ComponentId { get; set; }
    public string? ComponentCode { get; set; }
    public string? ComponentName { get; set; }
}

public class ServiceMaterialOptionDto
{
    public Guid ComponentId { get; set; }
    public Guid StockItemId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal? SuggestedPrice { get; set; }
}

public class ServiceWorkOptionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal DefaultPrice { get; set; }
}

public class ServiceWarehouseOptionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
