using System;

namespace VPureLux.Service;

public class ServiceOrderFilter
{
    public string? SearchText { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? CustomerAssetId { get; set; }
    public ServiceOrderStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Sorting { get; set; }
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; } = 10;
}

public class ServiceOrderListItem
{
    public Guid Id { get; set; }
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
    public int LineCount { get; set; }
}

public class ServiceAssetOption
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string AssetNo { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string? ServiceAddress { get; set; }
}

public class ServiceMaterialOption
{
    public Guid ComponentId { get; set; }
    public Guid StockItemId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal? SuggestedPrice { get; set; }
}

public class ServiceWorkOption
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal DefaultPrice { get; set; }
}
