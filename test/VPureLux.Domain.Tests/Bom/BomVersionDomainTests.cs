using System;
using System.Linq;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace VPureLux.Bom;

public class BomVersionDomainTests
{
    [Fact]
    public void Should_Create_Draft_Bom()
    {
        var bom = CreateBom();

        bom.Status.ShouldBe(BomStatus.Draft);
        bom.VersionNo.Value.ShouldBe(1);
        bom.Items.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Add_Remove_And_Update_Item()
    {
        var bom = CreateBom();
        var item = bom.AddItem(Guid.NewGuid(), Guid.NewGuid(), 1);
        var replacementComponentId = Guid.NewGuid();

        bom.UpdateItem(item.Id, replacementComponentId, 2);
        bom.Items.Single().ComponentId.ShouldBe(replacementComponentId);
        bom.Items.Single().Quantity.ShouldBe(2);

        bom.RemoveItem(item.Id);
        bom.Items.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Reject_Non_Positive_Quantity()
    {
        var bom = CreateBom();

        var exception = Should.Throw<BusinessException>(
            () => bom.AddItem(Guid.NewGuid(), Guid.NewGuid(), 0));

        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.ValidationFailed);
    }

    [Fact]
    public void Should_Reject_Fractional_Quantity()
    {
        var bom = CreateBom();

        var exception = Should.Throw<BusinessException>(
            () => bom.AddItem(Guid.NewGuid(), Guid.NewGuid(), 1.5m));

        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.ValidationFailed);
    }

    [Fact]
    public void Should_Publish_Bom_With_Item()
    {
        var bom = CreateBomWithItem();

        bom.Publish();

        bom.Status.ShouldBe(BomStatus.Published);
    }

    [Fact]
    public void Should_Reject_Publishing_Empty_Bom()
    {
        var exception = Should.Throw<BusinessException>(() => CreateBom().Publish());

        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.ValidationFailed);
    }

    [Fact]
    public void Should_Archive_Published_Bom()
    {
        var bom = CreateBomWithItem();
        bom.Publish();
        var effectiveTo = DateTime.UtcNow.AddDays(1);

        bom.Archive(effectiveTo);

        bom.Status.ShouldBe(BomStatus.Archived);
        bom.EffectiveTo.ShouldBe(effectiveTo);
    }

    [Fact]
    public void Should_Clone_All_Items_Into_New_Draft()
    {
        var bom = CreateBom();
        bom.AddItem(Guid.NewGuid(), Guid.NewGuid(), 1);
        bom.AddItem(Guid.NewGuid(), Guid.NewGuid(), 2);

        var clone = bom.CloneVersion(Guid.NewGuid(), new BomVersionNo(2), DateTime.UtcNow, Guid.NewGuid);

        clone.Status.ShouldBe(BomStatus.Draft);
        clone.VersionNo.Value.ShouldBe(2);
        clone.Items.Count.ShouldBe(2);
        clone.Items.Select(x => x.ComponentId).ShouldBe(bom.Items.Select(x => x.ComponentId), ignoreOrder: true);
        clone.Items.Select(x => x.Id).Intersect(bom.Items.Select(x => x.Id)).ShouldBeEmpty();
    }

    [Fact]
    public void Should_Reject_Published_Modification()
    {
        var bom = CreateBomWithItem();
        bom.Publish();

        var exception = Should.Throw<BusinessException>(
            () => bom.AddItem(Guid.NewGuid(), Guid.NewGuid(), 1));

        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.PublishedBomCannotBeModified);
    }

    [Fact]
    public void Should_Reject_Archived_Modification()
    {
        var bom = CreateBomWithItem();
        bom.Publish();
        bom.Archive(DateTime.UtcNow.AddDays(1));

        var exception = Should.Throw<BusinessException>(
            () => bom.UpdateQuantity(bom.Items.Single().Id, 2));

        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.ArchivedBomCannotBeModified);
    }

    internal static BomVersion CreateBom()
    {
        return new BomVersion(Guid.NewGuid(), Guid.NewGuid(), new BomVersionNo(1), DateTime.UtcNow);
    }

    internal static BomVersion CreateBomWithItem()
    {
        var bom = CreateBom();
        bom.AddItem(Guid.NewGuid(), Guid.NewGuid(), 1);
        return bom;
    }
}
