using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Shouldly;
using VPureLux.Catalog;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using VPureLux.Permissions;
using Volo.Abp;
using Volo.Abp.Auditing;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Catalog;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class CatalogImageAppServiceTests : VPureLuxEntityFrameworkCoreTestBase
{
    private readonly IProductAppService _products;
    private readonly IComponentAppService _components;

    public CatalogImageAppServiceTests()
    {
        _products = GetRequiredService<IProductAppService>();
        _components = GetRequiredService<IComponentAppService>();
    }

    [Fact]
    public async Task Should_Upload_Get_Replace_And_Remove_Product_Image()
    {
        var product = await CreateProductAsync();
        var first = await _products.SetImageAsync(product.Id, CatalogImageTestData.Png());
        var content = await _products.GetImageAsync(product.Id);

        content.MimeType.ShouldBe("image/png");
        content.Content.ShouldNotBeEmpty();
        content.ImageHash.ShouldBe(first.ImageHash);

        var replacement = await _products.SetImageAsync(product.Id, CatalogImageTestData.Jpeg());
        replacement.ImageHash.ShouldNotBe(first.ImageHash);
        (await _products.GetAsync(product.Id)).HasImage.ShouldBeTrue();

        await _products.RemoveImageAsync(product.Id);
        (await _products.GetAsync(product.Id)).HasImage.ShouldBeFalse();
        (await Should.ThrowAsync<BusinessException>(() => _products.GetImageAsync(product.Id)))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.CatalogImageNotFound);
    }

    [Fact]
    public async Task Should_Upload_Component_Image_And_Generate_Thumbnail()
    {
        var component = await CreateComponentAsync();

        var metadata = await _components.SetImageAsync(component.Id, CatalogImageTestData.Webp());
        var thumbnail = await _components.GetThumbnailAsync(component.Id);

        metadata.MimeType.ShouldBe("image/webp");
        thumbnail.MimeType.ShouldBe("image/webp");
        thumbnail.FileName.ShouldBe("thumbnail.webp");
        thumbnail.Content.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Same_Image_Reupload_Should_Succeed_Without_Persistence_Change()
    {
        var product = await CreateProductAsync();
        var input = CatalogImageTestData.Png();
        var first = await _products.SetImageAsync(product.Id, input);
        var repository = GetRequiredService<IProductRepository>();
        var before = await repository.GetAsync(product.Id);
        var lastModificationTime = before.LastModificationTime;

        var second = await _products.SetImageAsync(product.Id, input);
        var after = await repository.GetAsync(product.Id);

        second.ImageHash.ShouldBe(first.ImageHash);
        after.LastModificationTime.ShouldBe(lastModificationTime);
    }

    [Fact]
    public async Task Should_Handle_Missing_Entities()
    {
        (await Should.ThrowAsync<BusinessException>(() => _products.SetImageAsync(Guid.NewGuid(), CatalogImageTestData.Png())))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.ProductNotFound);
        (await Should.ThrowAsync<BusinessException>(() => _components.SetImageAsync(Guid.NewGuid(), CatalogImageTestData.Png())))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.ComponentNotFound);
    }

    [Fact]
    public void Image_Methods_Should_Use_Existing_Permissions_And_Disable_Content_Auditing()
    {
        var productMethods = typeof(ProductAppService).GetMethods(BindingFlags.Public | BindingFlags.Instance);
        var componentMethods = typeof(ComponentAppService).GetMethods(BindingFlags.Public | BindingFlags.Instance);

        AssertImageMethod(productMethods, nameof(ProductAppService.SetImageAsync), VPureLuxPermissions.Catalog.Products.Edit);
        AssertImageMethod(productMethods, nameof(ProductAppService.GetImageAsync), VPureLuxPermissions.Catalog.Products.View);
        AssertImageMethod(componentMethods, nameof(ComponentAppService.SetImageAsync), VPureLuxPermissions.Catalog.Components.Edit);
        AssertImageMethod(componentMethods, nameof(ComponentAppService.GetImageAsync), VPureLuxPermissions.Catalog.Components.View);
    }

    private static void AssertImageMethod(MethodInfo[] methods, string name, string permission)
    {
        var method = methods.Single(x => x.Name == name);
        method.GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()!.Policy.ShouldBe(permission);
        method.GetCustomAttribute<DisableAuditingAttribute>().ShouldNotBeNull();
    }

    private Task<ProductDto> CreateProductAsync() => _products.CreateAsync(new CreateProductDto
    {
        Code = Unique("IMG-P"),
        Name = "Image Product"
    });

    private Task<ComponentDto> CreateComponentAsync() => _components.CreateAsync(new CreateComponentDto
    {
        Code = Unique("IMG-C"),
        Name = "Image Component",
        Unit = "Piece"
    });

    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];
}
