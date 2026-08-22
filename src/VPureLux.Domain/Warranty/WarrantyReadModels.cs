using System;

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

public class WarrantyReminderFilter
{
    public string? SearchText { get; set; }
    public AssetReplacementReminderStatus? Status { get; set; }
    public DateTime? DueFrom { get; set; }
    public DateTime? DueTo { get; set; }
    public string? Sorting { get; set; }
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; } = 10;
}

public class WarrantyReminderListItem
{
    public Guid Id { get; set; }
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
    public int CycleMonths { get; set; }
    public int WarningDaysBeforeDue { get; set; }
    public AssetReplacementReminderStatus Status { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public int LineNo { get; set; }
    public string? Note { get; set; }
}
