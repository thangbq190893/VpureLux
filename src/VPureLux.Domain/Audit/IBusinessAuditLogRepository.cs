using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VPureLux.Audit;

public interface IBusinessAuditLogRepository
{
    Task InsertAsync(BusinessAuditLog log, bool autoSave = false, CancellationToken cancellationToken = default);
    Task<BusinessAuditLog?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task<long> GetCountAsync(AuditSearchFilter filter, CancellationToken cancellationToken = default);
    Task<List<BusinessAuditLog>> GetListAsync(AuditSearchFilter filter, CancellationToken cancellationToken = default);
}

public sealed record AuditSearchFilter(
    DateTime? From = null,
    DateTime? To = null,
    Guid? UserId = null,
    string? Module = null,
    string? EntityType = null,
    Guid? EntityId = null,
    AuditSeverity? Severity = null,
    string? CorrelationId = null,
    int SkipCount = 0,
    int MaxResultCount = 50);
