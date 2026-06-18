using System;
using System.Linq;
using Shouldly;
using VPureLux.Catalog.Events;
using Xunit;

namespace VPureLux.Catalog;

public class ComponentDomainTests
{
    [Fact]
    public void Should_Create_Component_As_Active()
    {
        var component = new Component(
            Guid.NewGuid(),
            "PP001",
            "PP Filter",
            "5 micron filter",
            "Piece");

        component.Status.ShouldBe(CatalogItemStatus.Active);
        component.Code.ShouldBe("PP001");
        component.Name.ShouldBe("PP Filter");
        component.Unit.ShouldBe("Piece");
    }

    [Fact]
    public void Should_Update_Component_Info()
    {
        var component = new Component(Guid.NewGuid(), "PP001", "PP Filter", null, "Piece");

        component.UpdateInfo("PP Filter 5 Micron", "Updated", "Unit");

        component.Name.ShouldBe("PP Filter 5 Micron");
        component.Description.ShouldBe("Updated");
        component.Unit.ShouldBe("Unit");
    }

    [Fact]
    public void Should_Deactivate_Component()
    {
        var component = new Component(Guid.NewGuid(), "PP001", "PP Filter", null, "Piece");

        component.Deactivate();

        component.Status.ShouldBe(CatalogItemStatus.Inactive);
    }

    [Fact]
    public void Should_Activate_Component_After_Deactivation()
    {
        var component = new Component(Guid.NewGuid(), "PP001", "PP Filter", null, "Piece");
        component.Deactivate();

        component.Activate();

        component.Status.ShouldBe(CatalogItemStatus.Active);
    }

    [Fact]
    public void Should_Raise_Component_Domain_Events()
    {
        var component = new Component(Guid.NewGuid(), "PP001", "PP Filter", null, "Piece");
        component.UpdateInfo("PP Filter Updated", null, "Piece");
        component.Deactivate();

        var events = component.GetLocalEvents().Select(x => x.EventData).ToList();
        events.OfType<ComponentCreatedEvent>().ShouldHaveSingleItem();
        events.OfType<ComponentUpdatedEvent>().ShouldHaveSingleItem();
        events.OfType<ComponentDeactivatedEvent>().ShouldHaveSingleItem();
    }
}
