using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using VPureLux.Catalog;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using Volo.Abp.EntityFrameworkCore;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Catalog;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class CatalogImagePersistenceTests : VPureLuxEntityFrameworkCoreTestBase
{
    private readonly IProductRepository _products;
    private readonly IComponentRepository _components;
    private readonly CatalogManager _manager;
    private readonly ICatalogImageProcessor _processor;

    public CatalogImagePersistenceTests()
    {
        _products = GetRequiredService<IProductRepository>();
        _components = GetRequiredService<IComponentRepository>();
        _manager = GetRequiredService<CatalogManager>();
        _processor = GetRequiredService<ICatalogImageProcessor>();
    }

    [Fact]
    public async Task Should_Persist_Null_And_Product_Image()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var product = await _manager.CreateProductAsync(Unique("EF-P"), "EF Product", null);
            await _products.InsertAsync(product, autoSave: true);
            (await _products.GetAsync(product.Id)).Image.ShouldBeNull();

            product.SetImage(Process(CatalogImageTestData.Png()));
            await _products.UpdateAsync(product, autoSave: true);

            var persisted = await _products.GetAsync(product.Id);
            persisted.Image.ShouldNotBeNull();
            persisted.Image.ImageHash.Length.ShouldBe(64);
        });
    }

    [Fact]
    public async Task Should_Persist_Replace_And_Remove_Component_Image()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var component = await _manager.CreateComponentAsync(Unique("EF-C"), "EF Component", null, "Piece");
            component.SetImage(Process(CatalogImageTestData.Png()));
            await _components.InsertAsync(component, autoSave: true);
            var firstHash = component.Image!.ImageHash;

            component.SetImage(Process(CatalogImageTestData.Jpeg()));
            await _components.UpdateAsync(component, autoSave: true);
            (await _components.GetAsync(component.Id)).Image!.ImageHash.ShouldNotBe(firstHash);

            component.RemoveImage();
            await _components.UpdateAsync(component, autoSave: true);
            (await _components.GetAsync(component.Id)).Image.ShouldBeNull();
        });
    }

    [Fact]
    public async Task Model_Should_Map_Optional_Image_Columns_Without_Thumbnail()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var dbContext = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            var product = dbContext.Model.FindEntityType(typeof(Product))!;
            var component = dbContext.Model.FindEntityType(typeof(Component))!;
            product.FindNavigation(nameof(Product.Image)).ShouldNotBeNull();
            component.FindNavigation(nameof(Component.Image)).ShouldNotBeNull();
            dbContext.Model.GetEntityTypes().SelectMany(x => x.GetProperties())
                .ShouldNotContain(x => x.Name.Contains("Thumbnail", StringComparison.OrdinalIgnoreCase));
        });
    }

    [Fact]
    public async Task List_Projections_Should_Not_Select_ImageBase64()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var productSql = (await _products.GetQueryableAsync())
                .Select(x => new ProductDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    Description = x.Description,
                    Status = x.Status,
                    HasImage = x.Image!.ImageHash != null,
                    ImageHash = x.Image.ImageHash
                })
                .ToQueryString();
            var componentSql = (await _components.GetQueryableAsync())
                .Select(x => new ComponentDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    Description = x.Description,
                    Unit = x.Unit,
                    Status = x.Status,
                    HasImage = x.Image!.ImageHash != null,
                    ImageHash = x.Image.ImageHash
                })
                .ToQueryString();

            productSql.ShouldNotContain("ImageBase64");
            componentSql.ShouldNotContain("ImageBase64");
        });
    }

    private ImageData Process(CatalogImageUploadDto input) =>
        _processor.Process(input.ImageBase64, input.MimeType, input.FileName);

    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];
}
