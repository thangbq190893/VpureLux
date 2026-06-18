using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using VPureLux.Catalog;
using VPureLux.Pricing;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Pricing;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class PricingRepositoryTests : VPureLuxEntityFrameworkCoreTestBase
{
    private readonly IComponentSuggestedSellingPriceVersionRepository _componentPriceRepository;
    private readonly IProductSuggestedPriceVersionRepository _productPriceRepository;
    private readonly IComponentRepository _componentRepository;
    private readonly IProductRepository _productRepository;
    private readonly CatalogManager _catalogManager;
    private readonly IDbContextProvider<VPureLuxDbContext> _dbContextProvider;

    public PricingRepositoryTests()
    {
        _componentPriceRepository = GetRequiredService<IComponentSuggestedSellingPriceVersionRepository>();
        _productPriceRepository = GetRequiredService<IProductSuggestedPriceVersionRepository>();
        _componentRepository = GetRequiredService<IComponentRepository>();
        _productRepository = GetRequiredService<IProductRepository>();
        _catalogManager = GetRequiredService<CatalogManager>();
        _dbContextProvider = GetRequiredService<IDbContextProvider<VPureLuxDbContext>>();
    }

    [Fact]
    public async Task Should_Persist_Value_Objects_And_History()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var component = await CreateComponentAsync();
            var version = ComponentVersion(component.Id);
            await _componentPriceRepository.InsertAsync(version, autoSave: true);

            var found = await _componentPriceRepository.GetAsync(version.Id);
            found.VersionNo.Value.ShouldBe(1);
            found.Price.Amount.ShouldBe(30000m);
            found.Price.Currency.ShouldBe(PricingConsts.Currency);
            found.Reason.ShouldBe("Periodic component selling price adjustment");
            (await _componentPriceRepository.GetHistoryAsync(component.Id)).ShouldHaveSingleItem();
        });
    }

    [Fact]
    public async Task Should_Enforce_Catalog_Foreign_Keys()
    {
        await Should.ThrowAsync<DbUpdateException>(() => WithUnitOfWorkAsync(async () =>
        {
            var dbContext = await _dbContextProvider.GetDbContextAsync();
            dbContext.ComponentSuggestedSellingPriceVersions.Add(ComponentVersion(Guid.NewGuid()));
            await dbContext.SaveChangesAsync();
        }));

        await Should.ThrowAsync<DbUpdateException>(() => WithUnitOfWorkAsync(async () =>
        {
            var dbContext = await _dbContextProvider.GetDbContextAsync();
            dbContext.ProductSuggestedPriceVersions.Add(ProductVersion(Guid.NewGuid()));
            await dbContext.SaveChangesAsync();
        }));
    }

    [Fact]
    public async Task Should_Enforce_Unique_Component_Version_Index()
    {
        await Should.ThrowAsync<DbUpdateException>(() => WithUnitOfWorkAsync(async () =>
        {
            var component = await CreateComponentAsync();
            var dbContext = await _dbContextProvider.GetDbContextAsync();
            dbContext.ComponentSuggestedSellingPriceVersions.AddRange(
                ComponentVersion(component.Id),
                ComponentVersion(component.Id));
            await dbContext.SaveChangesAsync();
        }));
    }

    [Fact]
    public async Task Should_Enforce_Active_Version_And_Translate_To_PRICE_001()
    {
        var exception = await Should.ThrowAsync<BusinessException>(() => WithUnitOfWorkAsync(async () =>
        {
            var component = await CreateComponentAsync();
            await _componentPriceRepository.InsertAsync(ComponentVersion(component.Id), autoSave: true);
            await _componentPriceRepository.InsertAsync(ComponentVersion(component.Id, 2), autoSave: true);
        }));

        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.ActivePriceVersionAlreadyExists);
    }

    [Fact]
    public async Task Should_Simulate_Concurrent_Active_Creation_With_Database_Protection()
    {
        var product = await WithUnitOfWorkAsync(CreateProductAsync);
        await WithUnitOfWorkAsync(() =>
            _productPriceRepository.InsertAsync(ProductVersion(product.Id), autoSave: true));

        var exception = await Should.ThrowAsync<BusinessException>(() => WithUnitOfWorkAsync(() =>
            _productPriceRepository.InsertAsync(ProductVersion(product.Id, 2), autoSave: true)));

        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.ActivePriceVersionAlreadyExists);
    }

    [Fact]
    public async Task Should_Soft_Delete_Without_Removing_History_Row()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var product = await CreateProductAsync();
            var version = ProductVersion(product.Id);
            await _productPriceRepository.InsertAsync(version, autoSave: true);
            await _productPriceRepository.DeleteAsync(version, autoSave: true);

            (await _productPriceRepository.FindAsync(version.Id)).ShouldBeNull();
            var dbContext = await _dbContextProvider.GetDbContextAsync();
            (await dbContext.ProductSuggestedPriceVersions
                .IgnoreQueryFilters()
                .SingleAsync(x => x.Id == version.Id)).IsDeleted.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task Should_Have_Consistent_Pricing_Model_Configuration()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var dbContext = await _dbContextProvider.GetDbContextAsync();
            AssertPricingModel<ComponentSuggestedSellingPriceVersion>(
                dbContext,
                nameof(ComponentSuggestedSellingPriceVersion.ComponentId),
                ComponentSuggestedSellingPriceVersionConfiguration.ActiveComponentUniqueIndexName);
            AssertPricingModel<ProductSuggestedPriceVersion>(
                dbContext,
                nameof(ProductSuggestedPriceVersion.ProductId),
                ProductSuggestedPriceVersionConfiguration.ActiveProductUniqueIndexName);
        });
    }

    private static void AssertPricingModel<TEntity>(
        VPureLuxDbContext dbContext,
        string ownerProperty,
        string activeIndexName)
        where TEntity : class
    {
        var entity = dbContext.Model.FindEntityType(typeof(TEntity));
        entity.ShouldNotBeNull();
        entity.FindProperty("Reason")!.GetColumnType().ShouldBe("nvarchar(500)");
        entity.FindProperty("Status")!.GetColumnType().ShouldBe("INTEGER");
        entity.GetIndexes().Single(x => x.GetDatabaseName() == activeIndexName).IsUnique.ShouldBeTrue();
        entity.GetIndexes().Single(x => x.GetDatabaseName() == activeIndexName)
            .Properties.Single().Name.ShouldBe(ownerProperty);

        var price = dbContext.Model.GetEntityTypes().Single(x =>
            x.ClrType == typeof(Money) && x.FindOwnership()!.PrincipalEntityType == entity);
        price.FindProperty(nameof(Money.Amount))!.GetPrecision().ShouldBe(PricingConsts.PricePrecision);
        price.FindProperty(nameof(Money.Amount))!.GetScale().ShouldBe(PricingConsts.PriceScale);
    }

    private async Task<Component> CreateComponentAsync()
    {
        var component = await _catalogManager.CreateComponentAsync(
            Unique("PRICE-EFC"), "Pricing EF Component", null, "Piece");
        return await _componentRepository.InsertAsync(component, autoSave: true);
    }

    private async Task<Product> CreateProductAsync()
    {
        var product = await _catalogManager.CreateProductAsync(
            Unique("PRICE-EFP"), "Pricing EF Product", null);
        return await _productRepository.InsertAsync(product, autoSave: true);
    }

    private static ComponentSuggestedSellingPriceVersion ComponentVersion(Guid componentId, int versionNo = 1) =>
        (ComponentSuggestedSellingPriceVersion)Activator.CreateInstance(
            typeof(ComponentSuggestedSellingPriceVersion),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args:
            [
                Guid.NewGuid(),
                componentId,
                new PriceVersionNo(versionNo),
                new Money(30000m),
                "Periodic component selling price adjustment",
                DateTime.Now.Date
            ],
            culture: null)!;

    private static ProductSuggestedPriceVersion ProductVersion(Guid productId, int versionNo = 1) =>
        (ProductSuggestedPriceVersion)Activator.CreateInstance(
            typeof(ProductSuggestedPriceVersion),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args:
            [
                Guid.NewGuid(),
                productId,
                new PriceVersionNo(versionNo),
                new Money(100000m),
                "Periodic adjustment",
                DateTime.Now.Date
            ],
            culture: null)!;

    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];
}
