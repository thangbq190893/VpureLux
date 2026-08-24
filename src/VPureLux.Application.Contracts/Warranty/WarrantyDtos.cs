using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using VPureLux.Catalog;
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

public class ProductMachineSettingDto
{
    public Guid? Id { get; set; }
    public Guid ProductId { get; set; }
    public bool IsMachine { get; set; }
    public string? Note { get; set; }
}

public class ProductMachineSettingListDto
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public CatalogItemStatus ProductStatus { get; set; }
    public Guid? SettingId { get; set; }
    public bool IsMachine { get; set; }
    public string? Note { get; set; }
}

public class SetProductMachineSettingDto
{
    [Display(Name = "Warranty:IsMachine")]
    public bool IsMachine { get; set; }

    [Display(Name = "Warranty:Note")]
    [StringLength(WarrantyConsts.MaxNoteLength)]
    public string? Note { get; set; }
}

public class WarrantyReminderListDto : EntityDto<Guid>
{
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

public class WarrantyNotificationSummaryDto
{
    public long WarningCount { get; set; }
    public long OverdueCount { get; set; }
    public long TotalCount => WarningCount + OverdueCount;
}

public class SetComponentReplacementPolicyDto
{
    [Display(Name = "Warranty:IsEnabled")]
    public bool IsEnabled { get; set; } = true;

    [Required]
    [Range(1, 120)]
    [Display(Name = "Warranty:CycleMonths")]
    public int CycleMonths { get; set; } = 3;

    [Required]
    [Range(0, 365)]
    [Display(Name = "Warranty:WarningDaysBeforeDue")]
    public int WarningDaysBeforeDue { get; set; } = 7;

    [StringLength(WarrantyConsts.MaxNoteLength)]
    [Display(Name = "Warranty:Note")]
    public string? Note { get; set; }
}

public class CompleteReplacementReminderDto
{
    public DateTime? CompletedAt { get; set; }

    [Required]
    [StringLength(WarrantyConsts.MaxNoteLength)]
    public string? Note { get; set; }

    [Required]
    [StringLength(WarrantyConsts.MaxIdempotencyKeyLength)]
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class SkipReplacementReminderDto
{
    [Required]
    [StringLength(WarrantyConsts.MaxNoteLength)]
    public string? Note { get; set; }

    [Required]
    [StringLength(WarrantyConsts.MaxIdempotencyKeyLength)]
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class RescheduleReplacementReminderDto
{
    [Required]
    public DateTime DueDate { get; set; }

    [StringLength(WarrantyConsts.MaxNoteLength)]
    [Required]
    public string? Note { get; set; }

    [Required]
    [StringLength(WarrantyConsts.MaxIdempotencyKeyLength)]
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class SuspendCustomerAssetDto
{
    [Required]
    [StringLength(WarrantyConsts.MaxNoteLength)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    [StringLength(WarrantyConsts.MaxIdempotencyKeyLength)]
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class AssetMaintenanceEventListDto : EntityDto<Guid>
{
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

public class GetAssetMaintenanceHistoryInput : PagedAndSortedResultRequestDto
{
    public Guid CustomerAssetId { get; set; }
}

public class GetWarrantyPolicyListInput : PagedAndSortedResultRequestDto
{
    public string? SearchText { get; set; }
    public bool? IsEnabled { get; set; }
}

public class GetProductMachineSettingListInput : PagedAndSortedResultRequestDto
{
    public string? SearchText { get; set; }
    public bool? IsMachine { get; set; }
}

public class CustomerCareSyncFailureListDto : EntityDto<Guid>
{
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

public class GetCustomerCareSyncFailureListInput : PagedAndSortedResultRequestDto
{
    public string? SearchText { get; set; }
    public CustomerCareSyncFailureStatus? Status { get; set; }
}

public class PendingInstallationListDto : EntityDto<Guid>
{
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

public class GetPendingInstallationListInput : PagedAndSortedResultRequestDto
{
    public string? SearchText { get; set; }
}

public class AssetInstallationPositionDto
{
    public Guid? Id { get; set; }

    [Required]
    [StringLength(WarrantyConsts.MaxPositionCodeLength)]
    public string PositionCode { get; set; } = string.Empty;

    [Required]
    [StringLength(WarrantyConsts.MaxNameLength)]
    public string PositionName { get; set; } = string.Empty;

    public Guid? ComponentId { get; set; }
    public string? ComponentCode { get; set; }
    public string? ComponentName { get; set; }
    public string? ComponentUnit { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    public bool IsIncluded { get; set; } = true;

    [StringLength(WarrantyConsts.MaxNoteLength)]
    public string? Note { get; set; }
}

public class AssetInstallationEditorDto : EntityDto<Guid>
{
    public string AssetNo { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string OrderNo { get; set; } = string.Empty;
    public string? SerialNo { get; set; }
    public string? InstallationAddress { get; set; }
    public DateTime? InstalledAt { get; set; }
    public CustomerAssetStatus Status { get; set; }
    public List<AssetInstallationPositionDto> Positions { get; set; } = [];
}

public class ConfirmAssetInstallationDto
{
    [Required]
    public DateTime InstalledAt { get; set; }

    [StringLength(WarrantyConsts.MaxSerialNoLength)]
    public string? SerialNo { get; set; }

    [StringLength(WarrantyConsts.MaxAddressLength)]
    public string? InstallationAddress { get; set; }

    [Required]
    [StringLength(WarrantyConsts.MaxIdempotencyKeyLength)]
    public string IdempotencyKey { get; set; } = string.Empty;

    [MinLength(1)]
    public List<AssetInstallationPositionDto> Positions { get; set; } = [];
}

public class ConfirmAssetInstallationResultDto
{
    public Guid AssetId { get; set; }
    public int ActivePositionCount { get; set; }
    public int CreatedReminderCount { get; set; }
    public bool IsReplay { get; set; }
}

public class CustomerAssetListDto : EntityDto<Guid>
{
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

public class GetCustomerAssetListInput : PagedAndSortedResultRequestDto
{
    public string? SearchText { get; set; }
    public CustomerAssetSource? Source { get; set; }
}

public class ExternalAssetPositionInput
{
    public Guid? Id { get; set; }

    [Required]
    [StringLength(WarrantyConsts.MaxPositionCodeLength)]
    public string PositionCode { get; set; } = string.Empty;

    [Required]
    [StringLength(WarrantyConsts.MaxNameLength)]
    public string PositionName { get; set; } = string.Empty;

    public Guid? ComponentId { get; set; }
    public string? ComponentCode { get; set; }
    public string? ComponentName { get; set; }
    public string? ComponentUnit { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    public DateTime? ReplacementBaselineDate { get; set; }

    [StringLength(WarrantyConsts.MaxNoteLength)]
    public string? Note { get; set; }
}

public class CreateExternalCustomerAssetDto
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    [StringLength(WarrantyConsts.MaxModelLength)]
    public string Model { get; set; } = string.Empty;

    [StringLength(WarrantyConsts.MaxBrandLength)]
    public string? Brand { get; set; }

    [StringLength(WarrantyConsts.MaxSerialNoLength)]
    public string? SerialNo { get; set; }

    [StringLength(WarrantyConsts.MaxAddressLength)]
    public string? InstallationAddress { get; set; }

    [StringLength(WarrantyConsts.MaxCodeLength)]
    public string? ExternalReference { get; set; }

    [StringLength(WarrantyConsts.MaxNoteLength)]
    public string? Note { get; set; }

    [Required]
    [StringLength(WarrantyConsts.MaxIdempotencyKeyLength)]
    public string IdempotencyKey { get; set; } = string.Empty;

    [MinLength(1)]
    public List<ExternalAssetPositionInput> Positions { get; set; } = [];
}

public class ExternalCustomerAssetResultDto
{
    public Guid AssetId { get; set; }
    public string AssetNo { get; set; } = string.Empty;
    public int PositionCount { get; set; }
    public int CreatedReminderCount { get; set; }
    public List<string> DuplicateSerialAssetNos { get; set; } = [];
}

public class UpdateExternalCustomerAssetDto
{
    [Required]
    [StringLength(WarrantyConsts.MaxModelLength)]
    public string Model { get; set; } = string.Empty;

    [StringLength(WarrantyConsts.MaxBrandLength)]
    public string? Brand { get; set; }

    [StringLength(WarrantyConsts.MaxSerialNoLength)]
    public string? SerialNo { get; set; }

    [StringLength(WarrantyConsts.MaxAddressLength)]
    public string? InstallationAddress { get; set; }

    [StringLength(WarrantyConsts.MaxCodeLength)]
    public string? ExternalReference { get; set; }

    [StringLength(WarrantyConsts.MaxNoteLength)]
    public string? Note { get; set; }

    [Required]
    [StringLength(WarrantyConsts.MaxIdempotencyKeyLength)]
    public string IdempotencyKey { get; set; } = string.Empty;

    [MinLength(1)]
    public List<ExternalAssetPositionInput> Positions { get; set; } = [];
}

public class CustomerAssetDetailDto : EntityDto<Guid>
{
    public string AssetNo { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public CustomerAssetSource? Source { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNo { get; set; }
    public string? InstallationAddress { get; set; }
    public string? ExternalReference { get; set; }
    public CustomerAssetStatus Status { get; set; }
    public string? Note { get; set; }
    public List<ExternalAssetPositionInput> Positions { get; set; } = [];
}

public class GetWarrantyReminderListInput : PagedAndSortedResultRequestDto
{
    public string? SearchText { get; set; }
    public AssetReplacementReminderStatus? Status { get; set; }
    public WarrantyReminderTimingStatus? TimingStatus { get; set; }
    public DateTime? DueFrom { get; set; }
    public DateTime? DueTo { get; set; }
}
