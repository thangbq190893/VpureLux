using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Warranty;

public class ComponentReplacementPolicyDto : EntityDto<Guid>
{
    public Guid ComponentId { get; set; }
    public bool IsEnabled { get; set; }
    public int CycleMonths { get; set; }
    public int WarningDaysBeforeDue { get; set; }
    public string? Note { get; set; }
}

public class WarrantyPolicyListDto
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

public class WarrantyReminderListDto : EntityDto<Guid>
{
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

public class SetComponentReplacementPolicyDto
{
    public bool IsEnabled { get; set; } = true;

    [Required]
    [Range(1, 120)]
    public int CycleMonths { get; set; } = 3;

    [Required]
    [Range(0, 365)]
    public int WarningDaysBeforeDue { get; set; } = 7;

    [StringLength(WarrantyConsts.MaxNoteLength)]
    public string? Note { get; set; }
}

public class CompleteReplacementReminderDto
{
    public DateTime? CompletedAt { get; set; }

    [StringLength(WarrantyConsts.MaxNoteLength)]
    public string? Note { get; set; }
}

public class SkipReplacementReminderDto
{
    [StringLength(WarrantyConsts.MaxNoteLength)]
    public string? Note { get; set; }
}

public class RescheduleReplacementReminderDto
{
    [Required]
    public DateTime DueDate { get; set; }

    [StringLength(WarrantyConsts.MaxNoteLength)]
    public string? Note { get; set; }
}

public class GetWarrantyPolicyListInput : PagedAndSortedResultRequestDto
{
    public string? SearchText { get; set; }
    public bool? IsEnabled { get; set; }
}

public class GetWarrantyReminderListInput : PagedAndSortedResultRequestDto
{
    public string? SearchText { get; set; }
    public AssetReplacementReminderStatus? Status { get; set; }
    public DateTime? DueFrom { get; set; }
    public DateTime? DueTo { get; set; }
}
