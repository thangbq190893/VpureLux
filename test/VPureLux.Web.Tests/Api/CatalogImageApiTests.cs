using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Shouldly;
using SixLabors.ImageSharp;
using VPureLux.Catalog;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using Xunit;

namespace VPureLux.Api;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class CatalogImageApiTests : VPureLuxWebTestBase
{
    [Fact]
    public async Task Product_Image_Routes_Should_Upload_Get_Thumbnail_And_Delete()
    {
        var product = await CreateProductAsync();
        var upload = CatalogImageTestData.Png();

        var put = await Client.PutAsJsonAsync($"/api/catalog/products/{product.Id}/image", upload);
        put.StatusCode.ShouldBe(HttpStatusCode.OK, await put.Content.ReadAsStringAsync());

        var image = await Client.GetAsync($"/api/catalog/products/{product.Id}/image");
        image.StatusCode.ShouldBe(HttpStatusCode.OK);
        image.Content.Headers.ContentType!.MediaType.ShouldBe("image/png");
        image.Headers.ETag.ShouldNotBeNull();
        image.Headers.CacheControl!.Public.ShouldBeTrue();

        var thumbnail = await Client.GetAsync($"/api/catalog/products/{product.Id}/thumbnail");
        thumbnail.StatusCode.ShouldBe(HttpStatusCode.OK);
        thumbnail.Content.Headers.ContentType!.MediaType.ShouldBe("image/webp");
        thumbnail.Headers.ETag.ShouldNotBeNull();
        var info = Image.Identify(await thumbnail.Content.ReadAsByteArrayAsync());
        info.ShouldNotBeNull();
        info.Width.ShouldBeLessThanOrEqualTo(96);
        info.Height.ShouldBeLessThanOrEqualTo(96);

        (await Client.DeleteAsync($"/api/catalog/products/{product.Id}/image"))
            .StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Component_Image_Routes_Should_Upload_Get_Thumbnail_And_Delete()
    {
        var component = await CreateComponentAsync();

        (await Client.PutAsJsonAsync($"/api/catalog/components/{component.Id}/image", CatalogImageTestData.Jpeg()))
            .StatusCode.ShouldBe(HttpStatusCode.OK);
        (await Client.GetAsync($"/api/catalog/components/{component.Id}/image"))
            .Content.Headers.ContentType!.MediaType.ShouldBe("image/jpeg");
        (await Client.GetAsync($"/api/catalog/components/{component.Id}/thumbnail"))
            .Content.Headers.ContentType!.MediaType.ShouldBe("image/webp");
        (await Client.DeleteAsync($"/api/catalog/components/{component.Id}/image"))
            .StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Invalid_Signature_Should_Return_Catalog_Error()
    {
        var product = await CreateProductAsync();
        var response = await Client.PutAsJsonAsync(
            $"/api/catalog/products/{product.Id}/image",
            new CatalogImageUploadDto
            {
                ImageBase64 = Convert.ToBase64String([1, 2, 3, 4]),
                MimeType = "image/jpeg",
                FileName = "invalid.jpg"
            });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await response.Content.ReadAsStringAsync()).ShouldContain(VPureLuxDomainErrorCodes.CatalogImageInvalidSignature);
    }

    private Task<ProductDto> CreateProductAsync() =>
        GetRequiredService<IProductAppService>().CreateAsync(new CreateProductDto
        {
            Code = Unique("API-IMG-P"),
            Name = "API Image Product"
        });

    private Task<ComponentDto> CreateComponentAsync() =>
        GetRequiredService<IComponentAppService>().CreateAsync(new CreateComponentDto
        {
            Code = Unique("API-IMG-C"),
            Name = "API Image Component",
            Unit = "Piece"
        });

    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];
}
