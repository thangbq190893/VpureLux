using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Warranty;

public class AssetReplacementReminder : FullAuditedAggregateRoot<Guid>
{
    public Guid CustomerAssetId { get; private set; }
    public Guid ComponentId { get; private set; }
    public Guid SalesOrderId { get; private set; }
    public Guid SalesOrderLineId { get; private set; }
    public string ComponentCodeSnapshot { get; private set; } = string.Empty;
    public string ComponentNameSnapshot { get; private set; } = string.Empty;
    public string ComponentUnitSnapshot { get; private set; } = string.Empty;
    public decimal QuantityPerProductSnapshot { get; private set; }
    public DateTime DueDate { get; private set; }
    public int CycleMonthsSnapshot { get; private set; }
    public int WarningDaysBeforeDueSnapshot { get; private set; }
    public AssetReplacementReminderStatus Status { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public Guid? CompletedByUserId { get; private set; }
    public Guid? NextReminderId { get; private set; }
    public string? Note { get; private set; }

    protected AssetReplacementReminder()
    {
    }

    public AssetReplacementReminder(
        Guid id,
        Guid customerAssetId,
        Guid componentId,
        Guid salesOrderId,
        Guid salesOrderLineId,
        string componentCodeSnapshot,
        string componentNameSnapshot,
        string componentUnitSnapshot,
        decimal quantityPerProductSnapshot,
        DateTime dueDate,
        int cycleMonthsSnapshot,
        int warningDaysBeforeDueSnapshot,
        string? note = null)
        : base(id)
    {
        CustomerAssetId = Check.NotDefaultOrNull<Guid>(customerAssetId, nameof(customerAssetId));
        ComponentId = Check.NotDefaultOrNull<Guid>(componentId, nameof(componentId));
        SalesOrderId = Check.NotDefaultOrNull<Guid>(salesOrderId, nameof(salesOrderId));
        SalesOrderLineId = Check.NotDefaultOrNull<Guid>(salesOrderLineId, nameof(salesOrderLineId));
        ComponentCodeSnapshot = Check.NotNullOrWhiteSpace(componentCodeSnapshot, nameof(componentCodeSnapshot), WarrantyConsts.MaxCodeLength);
        ComponentNameSnapshot = Check.NotNullOrWhiteSpace(componentNameSnapshot, nameof(componentNameSnapshot), WarrantyConsts.MaxNameLength);
        ComponentUnitSnapshot = Check.NotNullOrWhiteSpace(componentUnitSnapshot, nameof(componentUnitSnapshot), WarrantyConsts.MaxUnitLength);
        QuantityPerProductSnapshot = NormalizeQuantity(quantityPerProductSnapshot);
        SetDueDate(dueDate);
        if (cycleMonthsSnapshot <= 0 || warningDaysBeforeDueSnapshot < 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        CycleMonthsSnapshot = cycleMonthsSnapshot;
        WarningDaysBeforeDueSnapshot = warningDaysBeforeDueSnapshot;
        Note = Check.Length(note, nameof(note), WarrantyConsts.MaxNoteLength);
        Status = AssetReplacementReminderStatus.Pending;
    }

    public void Complete(DateTime completedAt, Guid? completedByUserId, Guid? nextReminderId, string? note)
    {
        EnsurePending();
        CompletedAt = completedAt;
        CompletedByUserId = completedByUserId;
        NextReminderId = nextReminderId;
        Note = Check.Length(note, nameof(note), WarrantyConsts.MaxNoteLength);
        Status = AssetReplacementReminderStatus.Completed;
    }

    public void Skip(string? note)
    {
        EnsurePending();
        Note = Check.Length(note, nameof(note), WarrantyConsts.MaxNoteLength);
        Status = AssetReplacementReminderStatus.Skipped;
    }

    public void Reschedule(DateTime dueDate, string? note)
    {
        EnsurePending();
        SetDueDate(dueDate);
        Note = Check.Length(note, nameof(note), WarrantyConsts.MaxNoteLength);
    }

    private void EnsurePending()
    {
        if (Status != AssetReplacementReminderStatus.Pending)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }
    }

    private void SetDueDate(DateTime dueDate)
    {
        DueDate = dueDate.Date;
    }

    private static decimal NormalizeQuantity(decimal value)
    {
        value = decimal.Round(value, WarrantyConsts.MaxQuantityScale, MidpointRounding.AwayFromZero);
        if (value <= 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        return value;
    }
}
