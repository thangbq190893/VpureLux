using System;

namespace VPureLux.Service;

public class ServiceOrderFilter
{
    public string? SearchText { get; set; }
    public Guid? CustomerId { get; set; }
    public ServiceOrderStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Sorting { get; set; }
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; }
}

public sealed record ServiceOrderListItem(
    Guid Id,
    string OrderNo,
    DateTime OrderDate,
    DateTime? ScheduledAt,
    ServiceOrderStatus Status,
    Guid CustomerId,
    string CustomerCode,
    string CustomerName,
    Guid CustomerAssetId,
    string AssetNo,
    string AssetName,
    decimal PlannedAmount,
    int LineCount);

public sealed record ServiceAssetOption(
    Guid Id,
    Guid CustomerId,
    string CustomerCode,
    string CustomerName,
    string AssetNo,
    string AssetName,
    string? ServiceAddress);

public sealed record ServiceMaterialOption(Guid Id, string Code, string Name, string Unit, decimal? SuggestedPrice);
public sealed record ServiceWorkOption(Guid Id, string Code, string Name, string Unit, decimal DefaultPrice, decimal? StandardCost);
public sealed record ServiceTechnicianOption(Guid Id, string Name);
