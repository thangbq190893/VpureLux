using System.Threading.Tasks;
using Shouldly;
using VPureLux.Catalog;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Catalog;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class CatalogRepositoryTests : VPureLuxEntityFrameworkCoreTestBase
{
    private readonly IComponentRepository _componentRepository;
    private readonly IProductRepository _productRepository;
    private readonly CatalogManager _catalogManager;

    public CatalogRepositoryTests()
    {
        _componentRepository = GetRequiredService<IComponentRepository>();
        _productRepository = GetRequiredService<IProductRepository>();
        _catalogManager = GetRequiredService<CatalogManager>();
    }

    [Fact]
    public async Task Should_Insert_And_Find_Component_By_Code()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var component = await _catalogManager.CreateComponentAsync("EFPP001", "EF PP Filter", null, "Piece");
            await _componentRepository.InsertAsync(component, autoSave: true);

            var found = await _componentRepository.FindByCodeAsync("EFPP001");

            found.ShouldNotBeNull();
            found.Name.ShouldBe("EF PP Filter");
        });
    }

    [Fact]
    public async Task Should_Insert_And_Find_Product_By_Code()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var product = await _catalogManager.CreateProductAsync("EFRO8", "EF RO 8 Stage", null);
            await _productRepository.InsertAsync(product, autoSave: true);

            var found = await _productRepository.FindByCodeAsync("EFRO8");

            found.ShouldNotBeNull();
            found.Name.ShouldBe("EF RO 8 Stage");
        });
    }

    [Fact]
    public async Task Should_Soft_Delete_Component()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var component = await _catalogManager.CreateComponentAsync("EFDEL001", "Deleted Filter", null, "Piece");
            await _componentRepository.InsertAsync(component, autoSave: true);
            await _componentRepository.DeleteAsync(component, autoSave: true);

            (await _componentRepository.FindAsync(component.Id)).ShouldBeNull();
        });
    }
}
