using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Warranty;

public class CustomerCareSyncFailure : FullAuditedAggregateRoot<Guid>
{
    public Guid? SalesOrderId { get; private set; }
    public Guid? SalesOrderLineId { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string? ErrorCode { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;
    public string? ErrorContext { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime FirstOccurredAt { get; private set; }
    public DateTime LastOccurredAt { get; private set; }
    public DateTime? NextRetryAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public CustomerCareSyncFailureStatus Status { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    protected CustomerCareSyncFailure()
    {
    }

    public CustomerCareSyncFailure(
        Guid id,
        string idempotencyKey,
        string errorMessage,
        DateTime occurredAt,
        Guid? salesOrderId = null,
        Guid? salesOrderLineId = null,
        string? errorCode = null,
        string? errorContext = null,
        DateTime? nextRetryAt = null)
        : base(id)
    {
        IdempotencyKey = Check.NotNullOrWhiteSpace(
            idempotencyKey,
            nameof(idempotencyKey),
            WarrantyConsts.MaxIdempotencyKeyLength);
        SalesOrderId = salesOrderId;
        SalesOrderLineId = salesOrderLineId;
        FirstOccurredAt = occurredAt;
        Status = CustomerCareSyncFailureStatus.Pending;
        RecordAttempt(errorCode, errorMessage, errorContext, occurredAt, nextRetryAt);
    }

    public void RecordAttempt(
        string? errorCode,
        string errorMessage,
        string? errorContext,
        DateTime occurredAt,
        DateTime? nextRetryAt)
    {
        if (Status != CustomerCareSyncFailureStatus.Pending)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        ErrorCode = Check.Length(errorCode, nameof(errorCode), WarrantyConsts.MaxErrorCodeLength);
        ErrorMessage = Check.NotNullOrWhiteSpace(
            errorMessage,
            nameof(errorMessage),
            WarrantyConsts.MaxErrorMessageLength);
        ErrorContext = Check.Length(errorContext, nameof(errorContext), WarrantyConsts.MaxErrorContextLength);
        AttemptCount++;
        LastOccurredAt = occurredAt;
        NextRetryAt = nextRetryAt;
    }

    public void Resolve(DateTime resolvedAt)
    {
        Status = CustomerCareSyncFailureStatus.Resolved;
        ResolvedAt = resolvedAt;
        NextRetryAt = null;
    }

    public void Ignore(DateTime resolvedAt)
    {
        Status = CustomerCareSyncFailureStatus.Ignored;
        ResolvedAt = resolvedAt;
        NextRetryAt = null;
    }

    public void ScheduleRetry(DateTime retryAt)
    {
        Status = CustomerCareSyncFailureStatus.Pending;
        ResolvedAt = null;
        NextRetryAt = retryAt;
    }
}
