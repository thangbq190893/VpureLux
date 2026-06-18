using System;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Audit;

public class BusinessAuditLogDto : EntityDto<Guid>
{
    public Guid EventId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public int EventVersion { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? EntityDisplay { get; set; }
    public AuditActorType ActorType { get; set; }
    public Guid? UserId { get; set; }
    public string? UserNameSnapshot { get; set; }
    public DateTime EventTime { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string CausationId { get; set; } = string.Empty;
    public string? OldValueJson { get; set; }
    public string? NewValueJson { get; set; }
    public string? MetadataJson { get; set; }
    public AuditSeverity Severity { get; set; }
    public bool IsSystemGenerated { get; set; }
}

public class AuditSearchInput : PagedAndSortedResultRequestDto
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public Guid? UserId { get; set; }
    public string? Module { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public AuditSeverity? Severity { get; set; }
    public string? CorrelationId { get; set; }
}

public class AuditExportDto
{
    public byte[] Content { get; set; } = [];
    public string ContentType { get; set; } = "text/csv";
    public string FileName { get; set; } = "business-audit.csv";
}
