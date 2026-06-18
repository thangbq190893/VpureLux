using System;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace VPureLux.Audit;

public class BusinessAuditLog : BasicAggregateRoot<Guid>, IMultiTenant
{
    public Guid EventId { get; private set; }
    public Guid? TenantId { get; private set; }
    public string Module { get; private set; } = string.Empty;
    public string EventName { get; private set; } = string.Empty;
    public int EventVersion { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string? EntityDisplay { get; private set; }
    public AuditActorType ActorType { get; private set; }
    public Guid? UserId { get; private set; }
    public string? UserNameSnapshot { get; private set; }
    public DateTime EventTime { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public string CausationId { get; private set; } = string.Empty;
    public Guid? TechnicalAuditLogId { get; private set; }
    public string? OldValueJson { get; private set; }
    public string? NewValueJson { get; private set; }
    public string? MetadataJson { get; private set; }
    public AuditSeverity Severity { get; private set; }
    public bool IsSystemGenerated { get; private set; }
    public DateTime CreationTime { get; private set; }

    protected BusinessAuditLog() { }

    internal BusinessAuditLog(Guid id, Guid? tenantId, BusinessAuditEnvelope envelope, DateTime creationTime)
        : base(id)
    {
        EventId = envelope.EventId;
        TenantId = tenantId;
        Module = envelope.Module;
        EventName = envelope.EventName;
        EventVersion = envelope.EventVersion;
        Action = envelope.Action;
        EntityType = envelope.EntityType;
        EntityId = envelope.EntityId;
        EntityDisplay = envelope.EntityDisplay;
        ActorType = envelope.ActorType;
        UserId = envelope.UserId;
        UserNameSnapshot = envelope.UserName;
        EventTime = envelope.EventTime;
        CorrelationId = envelope.CorrelationId;
        CausationId = envelope.CausationId;
        OldValueJson = envelope.OldValueJson;
        NewValueJson = envelope.NewValueJson;
        MetadataJson = envelope.MetadataJson;
        Severity = envelope.Severity;
        IsSystemGenerated = envelope.IsSystemGenerated;
        CreationTime = creationTime;
    }
}
