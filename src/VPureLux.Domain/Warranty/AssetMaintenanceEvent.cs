using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Warranty;

public class AssetMaintenanceEvent : CreationAuditedAggregateRoot<Guid>
{
    public Guid CustomerAssetId { get; private set; }
    public Guid? CustomerAssetComponentId { get; private set; }
    public AssetMaintenanceEventType EventType { get; private set; }
    public AssetMaintenanceSourceType SourceType { get; private set; }
    public Guid? SourceId { get; private set; }
    public Guid? ServiceOrderLineId { get; private set; }
    public Guid? ComponentId { get; private set; }
    public string? ComponentCodeSnapshot { get; private set; }
    public string? ComponentNameSnapshot { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string? Note { get; private set; }

    protected AssetMaintenanceEvent()
    {
    }

    public AssetMaintenanceEvent(
        Guid id,
        Guid customerAssetId,
        Guid? customerAssetComponentId,
        AssetMaintenanceEventType eventType,
        AssetMaintenanceSourceType sourceType,
        DateTime occurredAt,
        string idempotencyKey,
        Guid? sourceId = null,
        Guid? componentId = null,
        string? componentCode = null,
        string? componentName = null,
        string? note = null,
        Guid? serviceOrderLineId = null)
        : base(id)
    {
        CustomerAssetId = Check.NotDefaultOrNull<Guid>(customerAssetId, nameof(customerAssetId));
        CustomerAssetComponentId = customerAssetComponentId;
        EventType = eventType;
        SourceType = sourceType;
        SourceId = sourceId;
        ServiceOrderLineId = serviceOrderLineId;
        ComponentId = componentId;
        ComponentCodeSnapshot = Check.Length(componentCode, nameof(componentCode), WarrantyConsts.MaxCodeLength);
        ComponentNameSnapshot = Check.Length(componentName, nameof(componentName), WarrantyConsts.MaxNameLength);
        OccurredAt = occurredAt;
        IdempotencyKey = Check.NotNullOrWhiteSpace(
            idempotencyKey,
            nameof(idempotencyKey),
            WarrantyConsts.MaxIdempotencyKeyLength);
        Note = Check.Length(note, nameof(note), WarrantyConsts.MaxNoteLength);
    }
}
