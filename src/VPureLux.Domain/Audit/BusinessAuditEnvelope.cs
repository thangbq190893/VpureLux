using System;

namespace VPureLux.Audit;

public sealed record BusinessAuditEnvelope(
    Guid EventId,
    string Module,
    string EventName,
    string Action,
    string EntityType,
    Guid EntityId,
    string CorrelationId,
    string CausationId,
    DateTime EventTime,
    AuditSeverity Severity = AuditSeverity.Informational,
    string? EntityDisplay = null,
    string? OldValueJson = null,
    string? NewValueJson = null,
    string? MetadataJson = null,
    int EventVersion = 1,
    Guid? UserId = null,
    string? UserName = null,
    AuditActorType ActorType = AuditActorType.System,
    bool IsSystemGenerated = false);
