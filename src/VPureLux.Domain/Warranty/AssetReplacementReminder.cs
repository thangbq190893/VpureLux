using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Warranty;

public class AssetReplacementReminder : FullAuditedAggregateRoot<Guid>
{
    public Guid CustomerAssetId { get; private set; }
    public Guid? CustomerAssetComponentId { get; private set; }
    public Guid ComponentId { get; private set; }
    public Guid? SalesOrderId { get; private set; }
    public Guid? SalesOrderLineId { get; private set; }
    public string ComponentCodeSnapshot { get; private set; } = string.Empty;
    public string ComponentNameSnapshot { get; private set; } = string.Empty;
    public string ComponentUnitSnapshot { get; private set; } = string.Empty;
    public decimal QuantityPerProductSnapshot { get; private set; }
    public DateTime DueDate { get; private set; }
    public DateTime? WarningDate { get; private set; }
    public int CycleMonthsSnapshot { get; private set; }
    public int WarningDaysBeforeDueSnapshot { get; private set; }
    public ReplacementReminderTriggerSource? TriggerSource { get; private set; }
    public string? SourceReferenceType { get; private set; }
    public Guid? SourceReferenceId { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public AssetReplacementReminderStatus Status { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public Guid? CompletedByUserId { get; private set; }
    public Guid? NextReminderId { get; private set; }
    public Guid? CompletionEventId { get; private set; }
    public string? CloseReason { get; private set; }
    public string? Note { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    protected AssetReplacementReminder()
    {
    }

    public AssetReplacementReminder(
        Guid id,
        Guid customerAssetId,
        Guid componentId,
        Guid? salesOrderId,
        Guid? salesOrderLineId,
        string componentCodeSnapshot,
        string componentNameSnapshot,
        string componentUnitSnapshot,
        decimal quantityPerProductSnapshot,
        DateTime dueDate,
        int cycleMonthsSnapshot,
        int warningDaysBeforeDueSnapshot,
        string? note = null)
        : this(
            id,
            customerAssetId,
            null,
            componentId,
            salesOrderId,
            salesOrderLineId,
            componentCodeSnapshot,
            componentNameSnapshot,
            componentUnitSnapshot,
            quantityPerProductSnapshot,
            dueDate,
            cycleMonthsSnapshot,
            warningDaysBeforeDueSnapshot,
            ReplacementReminderTriggerSource.LegacySales,
            "SalesOrder",
            salesOrderId,
            null,
            note,
            initialize: true)
    {
    }

    public AssetReplacementReminder(
        Guid id,
        Guid customerAssetId,
        Guid customerAssetComponentId,
        Guid componentId,
        Guid? salesOrderId,
        Guid? salesOrderLineId,
        string componentCodeSnapshot,
        string componentNameSnapshot,
        string componentUnitSnapshot,
        decimal quantityPerProductSnapshot,
        DateTime dueDate,
        int cycleMonthsSnapshot,
        int warningDaysBeforeDueSnapshot,
        ReplacementReminderTriggerSource triggerSource,
        string sourceReferenceType,
        Guid? sourceReferenceId,
        string idempotencyKey,
        string? note = null)
        : this(
            id,
            customerAssetId,
            customerAssetComponentId,
            componentId,
            salesOrderId,
            salesOrderLineId,
            componentCodeSnapshot,
            componentNameSnapshot,
            componentUnitSnapshot,
            quantityPerProductSnapshot,
            dueDate,
            cycleMonthsSnapshot,
            warningDaysBeforeDueSnapshot,
            triggerSource,
            sourceReferenceType,
            sourceReferenceId,
            idempotencyKey,
            note,
            initialize: true)
    {
    }

    private AssetReplacementReminder(
        Guid id,
        Guid customerAssetId,
        Guid? customerAssetComponentId,
        Guid componentId,
        Guid? salesOrderId,
        Guid? salesOrderLineId,
        string componentCodeSnapshot,
        string componentNameSnapshot,
        string componentUnitSnapshot,
        decimal quantityPerProductSnapshot,
        DateTime dueDate,
        int cycleMonthsSnapshot,
        int warningDaysBeforeDueSnapshot,
        ReplacementReminderTriggerSource triggerSource,
        string? sourceReferenceType,
        Guid? sourceReferenceId,
        string? idempotencyKey,
        string? note,
        bool initialize)
        : base(id)
    {
        CustomerAssetId = Check.NotDefaultOrNull<Guid>(customerAssetId, nameof(customerAssetId));
        CustomerAssetComponentId = customerAssetComponentId;
        ComponentId = Check.NotDefaultOrNull<Guid>(componentId, nameof(componentId));
        SalesOrderId = salesOrderId;
        SalesOrderLineId = salesOrderLineId;
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
        WarningDate = DueDate.AddDays(-warningDaysBeforeDueSnapshot);
        TriggerSource = triggerSource;
        SourceReferenceType = Check.Length(sourceReferenceType, nameof(sourceReferenceType), WarrantyConsts.MaxSourceTypeLength);
        SourceReferenceId = sourceReferenceId;
        IdempotencyKey = Check.Length(idempotencyKey, nameof(idempotencyKey), WarrantyConsts.MaxIdempotencyKeyLength);
        Note = Check.Length(note, nameof(note), WarrantyConsts.MaxNoteLength);
        Status = AssetReplacementReminderStatus.Pending;
    }

    public void Complete(DateTime completedAt, Guid? completedByUserId, Guid? nextReminderId, string? note,
        Guid? completionEventId = null)
    {
        EnsurePending();
        CompletedAt = completedAt;
        CompletedByUserId = completedByUserId;
        NextReminderId = nextReminderId;
        CompletionEventId = completionEventId;
        Note = Check.Length(note, nameof(note), WarrantyConsts.MaxNoteLength);
        CloseReason = Check.Length(note, nameof(note), WarrantyConsts.MaxCloseReasonLength);
        Status = AssetReplacementReminderStatus.Completed;
    }

    public void Skip(string? note)
    {
        EnsurePending();
        Note = Check.Length(note, nameof(note), WarrantyConsts.MaxNoteLength);
        CloseReason = Check.Length(note, nameof(note), WarrantyConsts.MaxCloseReasonLength);
        Status = AssetReplacementReminderStatus.Skipped;
    }

    public void Cancel(string reason)
    {
        EnsurePending();
        CloseReason = Check.NotNullOrWhiteSpace(
            reason,
            nameof(reason),
            WarrantyConsts.MaxCloseReasonLength);
        Note = Check.Length(reason, nameof(reason), WarrantyConsts.MaxNoteLength);
        Status = AssetReplacementReminderStatus.Cancelled;
    }

    public void Reschedule(DateTime dueDate, string? note)
    {
        EnsurePending();
        SetDueDate(dueDate);
        WarningDate = DueDate.AddDays(-WarningDaysBeforeDueSnapshot);
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
