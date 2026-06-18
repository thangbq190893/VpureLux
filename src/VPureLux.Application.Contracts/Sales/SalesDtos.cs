using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Sales;

public class SalesOrderBomSnapshotItemDto : EntityDto<Guid>
{
    public Guid ComponentId { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal QuantityPerProduct { get; set; }
    public decimal TotalRequiredQuantity { get; set; }
}

public class SalesOrderLineDto : EntityDto<Guid>
{
    public int LineNo { get; set; }
    public Guid ProductId { get; set; }
    public Guid? BomVersionId { get; set; }
    public int? BomVersionNoSnapshot { get; set; }
    public string ItemCodeSnapshot { get; set; } = string.Empty;
    public string ItemNameSnapshot { get; set; } = string.Empty;
    public string UnitSnapshot { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public Guid? SuggestedPriceVersionId { get; set; }
    public decimal? SuggestedPriceSnapshot { get; set; }
    public decimal ActualSellingPrice { get; set; }
    public string? OverrideReason { get; set; }
    public Guid? InventoryTransactionId { get; set; }
    public decimal? RevenueAmount { get; set; }
    public decimal? CostPriceSnapshot { get; set; }
    public decimal? CostAmountSnapshot { get; set; }
    public decimal? ProfitAmount { get; set; }
    public decimal? MarginPercent { get; set; }
    public List<SalesOrderBomSnapshotItemDto> BomSnapshotItems { get; set; } = new();
}

public class SalesOrderDto : EntityDto<Guid>
{
    public string OrderNo { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid WarehouseId { get; set; }
    public DateTime OrderDate { get; set; }
    public SalesOrderStatus Status { get; set; }
    public string Currency { get; set; } = SalesConsts.Currency;
    public string CustomerCodeSnapshot { get; set; } = string.Empty;
    public string CustomerNameSnapshot { get; set; } = string.Empty;
    public Guid? CustomerGroupIdSnapshot { get; set; }
    public string CustomerGroupCodeSnapshot { get; set; } = string.Empty;
    public string CustomerGroupNameSnapshot { get; set; } = string.Empty;
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public decimal TotalRevenueAmount { get; set; }
    public decimal? TotalCostAmount { get; set; }
    public decimal? TotalProfitAmount { get; set; }
    public List<SalesOrderLineDto> Lines { get; set; } = new();
}

public class CustomerPurchaseHistoryDto
{
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public DateTime LastPurchaseDate { get; set; }
    public decimal LastPurchasePrice { get; set; }
    public decimal AveragePurchasePrice { get; set; }
    public decimal Revenue { get; set; }
    public decimal Profit { get; set; }
}

public class ConfirmSalesOrderResultDto
{
    public Guid SalesOrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public decimal TotalRevenueAmount { get; set; }
    public decimal? TotalCostAmount { get; set; }
    public decimal? TotalProfitAmount { get; set; }
}
