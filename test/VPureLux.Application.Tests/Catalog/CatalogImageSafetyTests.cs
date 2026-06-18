using System.Linq;
using System.Reflection;
using Shouldly;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Events;
using VPureLux.Catalog.Products;
using Volo.Abp.Auditing;
using Xunit;

namespace VPureLux.Catalog;

public class CatalogImageSafetyTests
{
    [Fact]
    public void General_Dtos_And_Events_Should_Not_Expose_Image_Content()
    {
        new[] { typeof(ProductDto), typeof(ComponentDto) }
            .SelectMany(x => x.GetProperties())
            .ShouldNotContain(x => x.Name.Contains("Base64") || x.PropertyType == typeof(byte[]));

        new[]
            {
                typeof(ProductImageChangedEvent),
                typeof(ProductImageRemovedEvent),
                typeof(ComponentImageChangedEvent),
                typeof(ComponentImageRemovedEvent)
            }
            .SelectMany(x => x.GetProperties())
            .ShouldNotContain(x => x.Name.Contains("Base64") || x.PropertyType == typeof(byte[]));
    }

    [Fact]
    public void Image_Content_Contracts_Should_Disable_Auditing()
    {
        typeof(CatalogImageUploadDto).GetProperty(nameof(CatalogImageUploadDto.ImageBase64))!
            .GetCustomAttribute<DisableAuditingAttribute>().ShouldNotBeNull();

        foreach (var service in new[] { typeof(IProductAppService), typeof(IComponentAppService) })
        {
            service.GetMethod("SetImageAsync")!.GetCustomAttribute<DisableAuditingAttribute>().ShouldNotBeNull();
            service.GetMethod("GetImageAsync")!.GetCustomAttribute<DisableAuditingAttribute>().ShouldNotBeNull();
            service.GetMethod("GetThumbnailAsync")!.GetCustomAttribute<DisableAuditingAttribute>().ShouldNotBeNull();
        }
    }

    [Fact]
    public void Image_Processor_Should_Not_Depend_On_Logger()
    {
        typeof(CatalogImageProcessor).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .ShouldNotContain(x => x.FieldType.Name.Contains("Logger"));
    }
}
