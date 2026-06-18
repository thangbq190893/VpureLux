using System;
using System.Linq;
using Shouldly;
using VPureLux.Catalog.Events;
using Xunit;

namespace VPureLux.Catalog;

public class ProductDomainTests
{
    [Fact]
    public void Should_Create_Product_As_Active()
    {
        var product = new Product(
            Guid.NewGuid(),
            "RO8",
            "RO 8 Stage",
            "Water purifier");

        product.Status.ShouldBe(CatalogItemStatus.Active);
        product.Code.ShouldBe("RO8");
        product.Name.ShouldBe("RO 8 Stage");
    }

    [Fact]
    public void Should_Update_Product_Info()
    {
        var product = new Product(Guid.NewGuid(), "RO8", "RO 8 Stage", null);

        product.UpdateInfo("RO 8 Stage Premium", "Updated");

        product.Name.ShouldBe("RO 8 Stage Premium");
        product.Description.ShouldBe("Updated");
    }

    [Fact]
    public void Should_Deactivate_Product()
    {
        var product = new Product(Guid.NewGuid(), "RO8", "RO 8 Stage", null);

        product.Deactivate();

        product.Status.ShouldBe(CatalogItemStatus.Inactive);
    }

    [Fact]
    public void Should_Activate_Product_After_Deactivation()
    {
        var product = new Product(Guid.NewGuid(), "RO8", "RO 8 Stage", null);
        product.Deactivate();

        product.Activate();

        product.Status.ShouldBe(CatalogItemStatus.Active);
    }

    [Fact]
    public void Should_Raise_Product_Domain_Events()
    {
        var product = new Product(Guid.NewGuid(), "RO8", "RO 8 Stage", null);
        product.UpdateInfo("RO 8 Stage Updated", null);
        product.Deactivate();

        var events = product.GetLocalEvents().Select(x => x.EventData).ToList();
        events.OfType<ProductCreatedEvent>().ShouldHaveSingleItem();
        events.OfType<ProductUpdatedEvent>().ShouldHaveSingleItem();
        events.OfType<ProductDeactivatedEvent>().ShouldHaveSingleItem();
    }
}
