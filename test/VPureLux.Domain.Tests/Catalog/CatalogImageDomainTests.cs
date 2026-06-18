using System;
using System.Linq;
using System.Reflection;
using Shouldly;
using VPureLux.Catalog.Events;
using Xunit;

namespace VPureLux.Catalog;

public class CatalogImageDomainTests
{
    [Theory]
    [InlineData("image/jpeg", "photo.jpg")]
    [InlineData("image/png", "photo.png")]
    [InlineData("image/webp", "photo.webp")]
    public void Should_Set_Supported_Image(string mimeType, string fileName)
    {
        var product = NewProduct();
        var image = NewImage("A", mimeType, fileName);

        product.SetImage(image);

        product.Image.ShouldBe(image);
        product.GetLocalEvents().Select(x => x.EventData)
            .OfType<ProductImageChangedEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Should_Replace_And_Remove_Image()
    {
        var component = NewComponent();
        component.SetImage(NewImage("A"));

        component.SetImage(NewImage("B"));
        component.Image!.ImageHash.ShouldBe(Hash("B"));

        component.RemoveImage();
        component.Image.ShouldBeNull();
        component.GetLocalEvents().Select(x => x.EventData)
            .OfType<ComponentImageRemovedEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Same_Hash_Should_Be_Idempotent()
    {
        var product = NewProduct();
        product.SetImage(NewImage("A"));
        var count = product.GetLocalEvents().Count();

        product.SetImage(NewImage("A"));

        product.GetLocalEvents().Count().ShouldBe(count);
        product.GetLocalEvents().Select(x => x.EventData)
            .OfType<ProductImageChangedEvent>().Count().ShouldBe(1);
    }

    [Fact]
    public void ImageData_Should_Be_Immutable_And_Events_Should_Not_Contain_Content()
    {
        typeof(ImageData).GetProperties()
            .ShouldAllBe(x => x.SetMethod == null || !x.SetMethod.IsPublic);

        var eventTypes = new[]
        {
            typeof(ProductImageChangedEvent),
            typeof(ProductImageRemovedEvent),
            typeof(ComponentImageChangedEvent),
            typeof(ComponentImageRemovedEvent)
        };

        eventTypes.SelectMany(x => x.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .ShouldNotContain(x => x.Name.Contains("Base64") || x.PropertyType == typeof(byte[]));
    }

    private static Product NewProduct() => new(Guid.NewGuid(), "IMG-P", "Image Product", null);

    private static Component NewComponent() => new(Guid.NewGuid(), "IMG-C", "Image Component", null, "Piece");

    private static ImageData NewImage(string marker, string mimeType = "image/png", string fileName = "image.png") =>
        new(Convert.ToBase64String([1, 2, 3]), mimeType, fileName, Hash(marker));

    private static string Hash(string marker) => marker.PadRight(CatalogConsts.ImageHashLength, '0');
}
