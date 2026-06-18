using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Shouldly;
using VPureLux.Bom;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using Xunit;

namespace VPureLux.Api;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class BomApiTests : VPureLuxWebTestBase
{
    [Fact]
    public async Task Should_Create_Get_And_List_Bom_Versions()
    {
        var references = await CreateReferencesAsync();

        var createResponse = await Client.PostAsJsonAsync(
            $"/api/bom/products/{references.ProductId}/versions",
            CreateInput(references.ComponentId));
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await createResponse.Content.ReadAsStringAsync());
        var created = await createResponse.Content.ReadFromJsonAsync<BomVersionDto>();
        created.ShouldNotBeNull();
        created.Status.ShouldBe(BomStatus.Draft);

        var detail = await GetResponseAsObjectAsync<BomVersionDto>($"/api/bom/versions/{created.Id}");
        detail.Id.ShouldBe(created.Id);

        var versions = await GetResponseAsObjectAsync<List<BomVersionDto>>(
            $"/api/bom/products/{references.ProductId}/versions");
        versions.ShouldContain(x => x.Id == created.Id);
    }

    [Fact]
    public async Task Should_Publish_And_Archive_Bom_Version()
    {
        var references = await CreateReferencesAsync();
        var created = await CreateBomAsync(references.ProductId, references.ComponentId);

        (await Client.PostAsync($"/api/bom/versions/{created.Id}/publish", null))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await GetResponseAsObjectAsync<BomVersionDto>($"/api/bom/versions/{created.Id}"))
            .Status.ShouldBe(BomStatus.Published);

        (await Client.PostAsync($"/api/bom/versions/{created.Id}/archive", null))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await GetResponseAsObjectAsync<BomVersionDto>($"/api/bom/versions/{created.Id}"))
            .Status.ShouldBe(BomStatus.Archived);
    }

    [Fact]
    public async Task Should_Clone_Bom_Version()
    {
        var references = await CreateReferencesAsync();
        var source = await CreateBomAsync(references.ProductId, references.ComponentId);

        var response = await Client.PostAsJsonAsync(
            $"/api/bom/versions/{source.Id}/clone",
            new CloneBomVersionDto { EffectiveFrom = DateTime.UtcNow.AddDays(1) });
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var result = await response.Content.ReadFromJsonAsync<CloneBomVersionResultDto>();
        result.ShouldNotBeNull();

        var clone = await GetResponseAsObjectAsync<BomVersionDto>($"/api/bom/versions/{result.NewBomVersionId}");
        clone.Status.ShouldBe(BomStatus.Draft);
        clone.VersionNo.ShouldBe(source.VersionNo + 1);
        clone.Items.Count.ShouldBe(source.Items.Count);
    }

    private async Task<(Guid ProductId, Guid ComponentId)> CreateReferencesAsync()
    {
        var productService = GetRequiredService<IProductAppService>();
        var componentService = GetRequiredService<IComponentAppService>();

        var product = await productService.CreateAsync(new CreateProductDto
        {
            Code = UniqueCode("APIP"),
            Name = "API BOM Product"
        });
        var component = await componentService.CreateAsync(new CreateComponentDto
        {
            Code = UniqueCode("APIC"),
            Name = "API BOM Component",
            Unit = "Piece"
        });

        return (product.Id, component.Id);
    }

    private async Task<BomVersionDto> CreateBomAsync(Guid productId, Guid componentId)
    {
        var response = await Client.PostAsJsonAsync(
            $"/api/bom/products/{productId}/versions",
            CreateInput(componentId));
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<BomVersionDto>())!;
    }

    private static CreateBomVersionDto CreateInput(Guid componentId)
    {
        return new CreateBomVersionDto
        {
            EffectiveFrom = DateTime.UtcNow,
            Items =
            {
                new CreateBomItemDto { ComponentId = componentId, Quantity = 1 }
            }
        };
    }

    private static string UniqueCode(string prefix)
    {
        return prefix + Guid.NewGuid().ToString("N")[..8];
    }
}
