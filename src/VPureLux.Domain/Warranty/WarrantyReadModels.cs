using System;
using System.Collections.Generic;
using VPureLux.Catalog;

namespace VPureLux.Warranty;

public class WarrantyPolicyFilter
{
    public string? SearchText { get; set; }
    public bool? IsEnabled { get; set; }
    public string? Sorting { get; set; }
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; } = 10;
}

public class WarrantyPolicyListItem
{
    public Guid ComponentId { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public string ComponentUnit { get; set; } = string.Empty;
    public Guid? PolicyId { get; set; }
    public bool IsEnabled { get; set; }
    public int? CycleMonths { get; set; }
    public int? WarningDaysBeforeDue { get; set; }
    public string? Note { get; set; }
}

public class ProductMachineSettingFilter
{
    public string? SearchText { get; set; }
    public bool? IsMachine { get; set; }
    public string? Sorting { get; set; }
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; } = 10;
}

public class ProductMachineSettingListItem
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public CatalogItemStatus ProductStatus { get; set; }
    public Guid? SettingId { get; set; }
    public bool IsMachine { get; set; }
    public string? Note { get; set; }
}

public sealed record CustomerCareSalesIntakeBomItem(
    Guid Id,
    Guid ComponentId,
    string ComponentCode,
    string ComponentName,
    string Unit,
    decimal QuantityPerProduct);

public sealed record CustomerCareSalesIntakeCandidate(
    Guid SalesOrderId,
    Guid SalesOrderLineId,
    int SalesOrderLineNo,
    string OrderNo,
    Guid CustomerId,
    string CustomerCode,
    string CustomerName,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    decimal Quantity,
    DateTime SoldAt,
    IReadOnlyList<CustomerCareSalesIntakeBomItem> BomItems);

public class CustomerCareSyncFailureFilter
{
    public string? SearchText { get; set; }
    public CustomerCareSyncFailureStatus? Status { get; set; }
    public string? Sorting { get; set; }
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; } = 10;
}

public class CustomerCareSyncFailureListItem
{
    public Guid Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public int? LineNo { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public string? ErrorCode { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string? ErrorContext { get; set; }
    public int AttemptCount { get; set; }
    public DateTime LastOccurredAt { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public CustomerCareSyncFailureStatus Status { get; set; }
}

public class PendingInstallationFilter
{
    public string? SearchText { get; set; }
    public string? Sorting { get; set; }
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; } = 10;
}

public class PendingInstallationListItem
{
    public Guid Id { get; set; }
    public string AssetNo { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string OrderNo { get; set; } = string.Empty;
    public int? LineNo { get; set; }
    public int? UnitIndex { get; set; }
    public DateTime? SoldDate { get; set; }
    public int PositionCount { get; set; }
}

public class CustomerAssetFilter
{
    public string? SearchText { get; set; }
    public CustomerAssetSource? Source { get; set; }
    public string? Sorting { get; set; }
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; } = 10;
}

public class CustomerAssetListItem
{
    public Guid Id { get; set; }
    public string AssetNo { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public CustomerAssetSource? Source { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNo { get; set; }
    public CustomerAssetStatus Status { get; set; }
    public int PositionCount { get; set; }
    public DateTime? NextDueDate { get; set; }
}

public class WarrantyReminderFilter
{
    public string? SearchText { get; set; }
    public AssetReplacementReminderStatus? Status { get; set; }
    public WarrantyReminderTimingStatus? TimingStatus { get; set; }
    public DateTime AsOfDate { get; set; }
    public DateTime? DueFrom { get; set; }
    public DateTime? DueTo { get; set; }
    public string? Sorting { get; set; }
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; } = 10;
}

public class WarrantyNotificationSummary
{
    public long WarningCount { get; set; }
    public long OverdueCount { get; set; }
    public long TotalCount => WarningCount + OverdueCount;
}

public class WarrantyReminderListItem
{
    public Guid Id { get; set; }
    public Guid CustomerAssetId { get; set; }
    public string AssetNo { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public string ComponentUnit { get; set; } = string.Empty;
    public decimal QuantityPerProduct { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? WarningDate { get; set; }
    public int CycleMonths { get; set; }
    public int WarningDaysBeforeDue { get; set; }
    public AssetReplacementReminderStatus Status { get; set; }
    public WarrantyReminderTimingStatus TimingStatus { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public int LineNo { get; set; }
    public string? Note { get; set; }
}

public class AssetMaintenanceHistoryFilter
{
    public Guid CustomerAssetId { get; set; }
    public string? Sorting { get; set; }
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; } = 10;
}

public class AssetMaintenanceEventListItem
{
    public Guid Id { get; set; }
    public Guid CustomerAssetId { get; set; }
    public Guid? CustomerAssetComponentId { get; set; }
    public AssetMaintenanceEventType EventType { get; set; }
    public AssetMaintenanceSourceType SourceType { get; set; }
    public string? ComponentCode { get; set; }
    public string? ComponentName { get; set; }
    public DateTime OccurredAt { get; set; }
    public Guid? CreatorId { get; set; }
    public string? Note { get; set; }
}
