using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using VPureLux.Bom;
using VPureLux.Catalog;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Bom;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class BomRepositoryAndPersistenceTests : VPureLuxEntityFrameworkCoreTestBase
{
    private readonly IBomVersionRepository _bomRepository;
    private readonly BomManager _bomManager;
    private readonly IProductRepository _productRepository;
    private readonly IComponentRepository _componentRepository;
    private readonly CatalogManager _catalogManager;
    private readonly IDbContextProvider<VPureLuxDbContext> _dbContextProvider;

    public BomRepositoryAndPersistenceTests()
    {
        _bomRepository = GetRequiredService<IBomVersionRepository>();
        _bomManager = GetRequiredService<BomManager>();
        _productRepository = GetRequiredService<IProductRepository>();
        _componentRepository = GetRequiredService<IComponentRepository>();
        _catalogManager = GetRequiredService<CatalogManager>();
        _dbContextProvider = GetRequiredService<IDbContextProvider<VPureLuxDbContext>>();
    }

    [Fact]
    public async Task Should_Persist_Aggregate_And_Owned_Items()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var references = await CreateReferencesAsync();
            var bom = await _bomManager.CreateAsync(references.Product.Id, DateTime.UtcNow);
            bom.AddItem(Guid.NewGuid(), references.Component.Id, 2);
            await _bomRepository.InsertAsync(bom, autoSave: true);

            var found = await _bomRepository.GetAsync(bom.Id, includeDetails: true);

            found.ProductId.ShouldBe(references.Product.Id);
            found.Items.ShouldHaveSingleItem().Quantity.ShouldBe(2);
        });
    }

    [Fact]
    public async Task Should_Return_Versions_In_Descending_Order_And_Calculate_Next_Version()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var references = await CreateReferencesAsync();
            var first = await _bomManager.CreateAsync(references.Product.Id, DateTime.UtcNow);
            first.AddItem(Guid.NewGuid(), references.Component.Id, 1);
            await _bomRepository.InsertAsync(first, autoSave: true);
            var second = await _bomManager.CreateAsync(references.Product.Id, DateTime.UtcNow);
            second.AddItem(Guid.NewGuid(), references.Component.Id, 1);
            await _bomRepository.InsertAsync(second, autoSave: true);

            (await _bomRepository.GetNextVersionNoAsync(references.Product.Id)).ShouldBe(3);
            (await _bomRepository.GetListByProductIdAsync(references.Product.Id))
                .Select(x => x.VersionNo.Value)
                .ShouldBe(new[] { 2, 1 });
        });
    }

    [Fact]
    public async Task Should_Enforce_Unique_Product_And_Version()
    {
        await Should.ThrowAsync<DbUpdateException>(() => WithUnitOfWorkAsync(async () =>
        {
            var references = await CreateReferencesAsync();
            var first = await _bomManager.CreateAsync(references.Product.Id, DateTime.UtcNow);
            first.AddItem(Guid.NewGuid(), references.Component.Id, 1);
            await _bomRepository.InsertAsync(first, autoSave: true);

            var duplicate = first.CloneVersion(Guid.NewGuid(), new BomVersionNo(1), DateTime.UtcNow, Guid.NewGuid);
            await _bomRepository.InsertAsync(duplicate, autoSave: true);
        }));
    }

    [Fact]
    public async Task Should_Enforce_Only_One_Published_Bom_At_Database_Level()
    {
        await Should.ThrowAsync<DbUpdateException>(() => WithUnitOfWorkAsync(async () =>
        {
            var references = await CreateReferencesAsync();
            var first = await CreatePersistedBomAsync(references);
            var second = await CreatePersistedBomAsync(references);

            first.Publish();
            await _bomRepository.UpdateAsync(first, autoSave: true);

            second.Publish();
            var dbContext = await _dbContextProvider.GetDbContextAsync();
            dbContext.BomVersions.Update(second);
            await dbContext.SaveChangesAsync();
        }));
    }

    [Fact]
    public async Task Should_Translate_Published_Product_Unique_Violation_To_Bom_003()
    {
        var exception = await Should.ThrowAsync<BusinessException>(() => WithUnitOfWorkAsync(async () =>
        {
            var references = await CreateReferencesAsync();
            var first = await CreatePersistedBomAsync(references);
            var second = await CreatePersistedBomAsync(references);

            first.Publish();
            await _bomRepository.UpdateAsync(first, autoSave: true);

            second.Publish();
            await _bomRepository.UpdateAsync(second, autoSave: true);
        }));

        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.OnlyOneActiveBomAllowed);
    }

    [Fact]
    public async Task Should_Enforce_Product_Foreign_Key()
    {
        await Should.ThrowAsync<DbUpdateException>(() => WithUnitOfWorkAsync(async () =>
        {
            var component = await CreateComponentAsync();
            var bom = await _bomManager.CreateAsync(Guid.NewGuid(), DateTime.UtcNow);
            bom.AddItem(Guid.NewGuid(), component.Id, 1);
            await _bomRepository.InsertAsync(bom, autoSave: true);
        }));
    }

    [Fact]
    public async Task Should_Enforce_Component_Foreign_Key()
    {
        await Should.ThrowAsync<DbUpdateException>(() => WithUnitOfWorkAsync(async () =>
        {
            var product = await CreateProductAsync();
            var bom = await _bomManager.CreateAsync(product.Id, DateTime.UtcNow);
            bom.AddItem(Guid.NewGuid(), Guid.NewGuid(), 1);
            await _bomRepository.InsertAsync(bom, autoSave: true);
        }));
    }

    [Fact]
    public async Task Should_Soft_Delete_Bom_Aggregate()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var references = await CreateReferencesAsync();
            var bom = await _bomManager.CreateAsync(references.Product.Id, DateTime.UtcNow);
            bom.AddItem(Guid.NewGuid(), references.Component.Id, 1);
            await _bomRepository.InsertAsync(bom, autoSave: true);

            await _bomRepository.DeleteAsync(bom, autoSave: true);

            (await _bomRepository.FindAsync(bom.Id)).ShouldBeNull();
        });
    }

    private async Task<(Product Product, Component Component)> CreateReferencesAsync()
    {
        return (await CreateProductAsync(), await CreateComponentAsync());
    }

    private async Task<BomVersion> CreatePersistedBomAsync((Product Product, Component Component) references)
    {
        var bom = await _bomManager.CreateAsync(references.Product.Id, DateTime.UtcNow);
        bom.AddItem(Guid.NewGuid(), references.Component.Id, 1);
        return await _bomRepository.InsertAsync(bom, autoSave: true);
    }

    private async Task<Product> CreateProductAsync()
    {
        var product = await _catalogManager.CreateProductAsync(UniqueCode("EFP"), "EF BOM Product", null);
        return await _productRepository.InsertAsync(product, autoSave: true);
    }

    private async Task<Component> CreateComponentAsync()
    {
        var component = await _catalogManager.CreateComponentAsync(UniqueCode("EFC"), "EF BOM Component", null, "Piece");
        return await _componentRepository.InsertAsync(component, autoSave: true);
    }

    private static string UniqueCode(string prefix)
    {
        return prefix + Guid.NewGuid().ToString("N")[..8];
    }
}
