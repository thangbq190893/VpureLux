using System;
using System.Text;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Timing;

namespace VPureLux.Audit;

public class BusinessAuditManager : ITransientDependency
{
    private readonly IGuidGenerator _guidGenerator;
    private readonly ICurrentTenant _currentTenant;
    private readonly IClock _clock;

    public BusinessAuditManager(IGuidGenerator guidGenerator, ICurrentTenant currentTenant, IClock clock)
    {
        _guidGenerator = guidGenerator;
        _currentTenant = currentTenant;
        _clock = clock;
    }

    public BusinessAuditLog Create(BusinessAuditEnvelope envelope)
    {
        Check.NotDefaultOrNull<Guid>(envelope.EventId, nameof(envelope.EventId));
        Check.NotNullOrWhiteSpace(envelope.Module, nameof(envelope.Module), AuditConsts.MaxModuleLength);
        Check.NotNullOrWhiteSpace(envelope.EventName, nameof(envelope.EventName), AuditConsts.MaxEventNameLength);
        Check.NotNullOrWhiteSpace(envelope.Action, nameof(envelope.Action), AuditConsts.MaxActionLength);
        Check.NotNullOrWhiteSpace(envelope.EntityType, nameof(envelope.EntityType), AuditConsts.MaxEntityTypeLength);
        Check.NotDefaultOrNull<Guid>(envelope.EntityId, nameof(envelope.EntityId));
        Check.NotNullOrWhiteSpace(envelope.CorrelationId, nameof(envelope.CorrelationId), AuditConsts.MaxCorrelationIdLength);
        Check.NotNullOrWhiteSpace(envelope.CausationId, nameof(envelope.CausationId), AuditConsts.MaxCorrelationIdLength);
        Check.Positive(envelope.EventVersion, nameof(envelope.EventVersion));
        ValidatePayload(envelope.OldValueJson, nameof(envelope.OldValueJson));
        ValidatePayload(envelope.NewValueJson, nameof(envelope.NewValueJson));
        ValidatePayload(envelope.MetadataJson, nameof(envelope.MetadataJson));
        if (envelope.ActorType == AuditActorType.User && envelope.UserId == null)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }
        return new BusinessAuditLog(_guidGenerator.Create(), _currentTenant.Id, envelope, _clock.Now);
    }

    private static void ValidatePayload(string? payload, string name)
    {
        if (payload != null && Encoding.UTF8.GetByteCount(payload) > AuditConsts.MaxPayloadBytes)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.AuditPayloadTooLarge)
                .WithData("Payload", name)
                .WithData("MaxBytes", AuditConsts.MaxPayloadBytes);
        }
    }
}
