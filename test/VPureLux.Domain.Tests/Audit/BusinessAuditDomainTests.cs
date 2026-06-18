using System;
using System.Linq;
using NSubstitute;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Guids;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Timing;
using Xunit;

namespace VPureLux.Audit;

public class BusinessAuditDomainTests
{
    private readonly BusinessAuditManager _manager;

    public BusinessAuditDomainTests()
    {
        var guids = Substitute.For<IGuidGenerator>();
        guids.Create().Returns(_ => Guid.NewGuid());
        var tenant = Substitute.For<ICurrentTenant>();
        tenant.Id.Returns((Guid?)null);
        var clock = Substitute.For<IClock>();
        clock.Now.Returns(_ => DateTime.UtcNow);
        _manager = new BusinessAuditManager(guids, tenant, clock);
    }

    [Fact]
    public void Business_Audit_Log_Should_Be_Immutable()
    {
        typeof(BusinessAuditLog).GetProperties()
            .ShouldAllBe(x => x.SetMethod == null || !x.SetMethod.IsPublic);
        typeof(IBusinessAuditLogRepository).GetMethods()
            .ShouldNotContain(x => x.Name.Contains("Update") || x.Name.Contains("Delete"));
    }

    [Theory]
    [InlineData(AuditSeverity.Informational, 0)]
    [InlineData(AuditSeverity.Important, 1)]
    [InlineData(AuditSeverity.Critical, 2)]
    public void Severity_Values_Should_Match_Approved_Contract(AuditSeverity severity, byte value)
    {
        ((byte)severity).ShouldBe(value);
        Enum.IsDefined(severity).ShouldBeTrue();
    }

    [Fact]
    public void User_Actor_Should_Require_User_Id()
    {
        Should.Throw<BusinessException>(() => _manager.Create(Envelope(actorType: AuditActorType.User)))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.ValidationFailed);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Correlation_And_Causation_Should_Be_Required(string value)
    {
        Should.Throw<ArgumentException>(() => _manager.Create(Envelope(correlationId: value)));
        Should.Throw<ArgumentException>(() => _manager.Create(Envelope(causationId: value)));
    }

    [Theory]
    [InlineData("Old")]
    [InlineData("New")]
    [InlineData("Metadata")]
    public void Payloads_Over_32KB_Should_Return_AUDIT_001(string payload)
    {
        var oversized = new string('A', AuditConsts.MaxPayloadBytes + 1);
        var envelope = payload switch
        {
            "Old" => Envelope(oldValueJson: oversized),
            "New" => Envelope(newValueJson: oversized),
            _ => Envelope(metadataJson: oversized)
        };

        Should.Throw<BusinessException>(() => _manager.Create(envelope))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.AuditPayloadTooLarge);
    }

    [Fact]
    public void Valid_Actors_And_Maximum_Payload_Should_Be_Accepted()
    {
        _manager.Create(Envelope(actorType: AuditActorType.System, metadataJson: new string('A', AuditConsts.MaxPayloadBytes)))
            .ActorType.ShouldBe(AuditActorType.System);
        _manager.Create(Envelope(actorType: AuditActorType.Integration))
            .ActorType.ShouldBe(AuditActorType.Integration);
    }

    private static BusinessAuditEnvelope Envelope(
        string correlationId = "correlation",
        string causationId = "causation",
        AuditActorType actorType = AuditActorType.System,
        string? oldValueJson = null,
        string? newValueJson = null,
        string? metadataJson = null) =>
        new(Guid.NewGuid(), "Audit", "TEST", "TEST", "TestEntity", Guid.NewGuid(),
            correlationId, causationId, DateTime.UtcNow, ActorType: actorType,
            OldValueJson: oldValueJson, NewValueJson: newValueJson, MetadataJson: metadataJson);
}
