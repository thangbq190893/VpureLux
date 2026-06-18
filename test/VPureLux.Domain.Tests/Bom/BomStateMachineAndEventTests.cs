using System;
using System.Linq;
using Shouldly;
using VPureLux.Bom.Events;
using Volo.Abp;
using Xunit;

namespace VPureLux.Bom;

public class BomStateMachineAndEventTests
{
    [Fact]
    public void Should_Allow_Draft_To_Published_To_Archived()
    {
        var bom = BomVersionDomainTests.CreateBomWithItem();

        bom.Publish();
        bom.Status.ShouldBe(BomStatus.Published);

        bom.Archive(DateTime.UtcNow.AddDays(1));
        bom.Status.ShouldBe(BomStatus.Archived);
    }

    [Fact]
    public void Should_Reject_Draft_To_Archived()
    {
        var exception = Should.Throw<BusinessException>(
            () => BomVersionDomainTests.CreateBomWithItem().Archive(DateTime.UtcNow.AddDays(1)));

        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.ValidationFailed);
    }

    [Fact]
    public void Should_Reject_Published_To_Published()
    {
        var bom = BomVersionDomainTests.CreateBomWithItem();
        bom.Publish();

        var exception = Should.Throw<BusinessException>(() => bom.Publish());

        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.PublishedBomCannotBeModified);
    }

    [Fact]
    public void Should_Reject_Archived_Transitions()
    {
        var bom = BomVersionDomainTests.CreateBomWithItem();
        bom.Publish();
        bom.Archive(DateTime.UtcNow.AddDays(1));

        Should.Throw<BusinessException>(() => bom.Publish())
            .Code.ShouldBe(VPureLuxDomainErrorCodes.ArchivedBomCannotBeModified);
        Should.Throw<BusinessException>(() => bom.Archive(DateTime.UtcNow.AddDays(2)))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.ArchivedBomCannotBeModified);
    }

    [Fact]
    public void Should_Raise_BomVersionCreatedEvent()
    {
        Events(BomVersionDomainTests.CreateBom()).OfType<BomVersionCreatedEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Should_Raise_BomPublishedEvent()
    {
        var bom = BomVersionDomainTests.CreateBomWithItem();
        bom.Publish();

        Events(bom).OfType<BomPublishedEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Should_Raise_BomArchivedEvent()
    {
        var bom = BomVersionDomainTests.CreateBomWithItem();
        bom.Publish();
        bom.Archive(DateTime.UtcNow.AddDays(1));

        Events(bom).OfType<BomArchivedEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Should_Raise_BomVersionClonedEvent_On_Clone()
    {
        var clone = BomVersionDomainTests.CreateBomWithItem()
            .CloneVersion(Guid.NewGuid(), new BomVersionNo(2), DateTime.UtcNow, Guid.NewGuid);

        Events(clone).OfType<BomVersionClonedEvent>().ShouldHaveSingleItem();
    }

    private static object[] Events(BomVersion bom)
    {
        return bom.GetLocalEvents().Select(x => x.EventData).ToArray();
    }
}
