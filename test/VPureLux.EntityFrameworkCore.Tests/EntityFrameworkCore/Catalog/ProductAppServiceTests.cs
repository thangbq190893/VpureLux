using System.Threading.Tasks;
using Shouldly;
using VPureLux.Catalog;
using VPureLux.Catalog.Products;
using Volo.Abp;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Catalog;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class ProductAppServiceTests : VPureLuxEntityFrameworkCoreTestBase
{
    private readonly IProductAppService _productAppService;

    public ProductAppServiceTests()
    {
        _productAppService = GetRequiredService<IProductAppService>();
    }

    [Fact]
    public async Task Should_Create_Product()
    {
        var product = await _productAppService.CreateAsync(new CreateProductDto
        {
            Code = "RO8",
            Name = "RO 8 Stage"
        });

        product.Id.ShouldNotBe(default);
        product.Status.ShouldBe(CatalogItemStatus.Active);
    }

    [Fact]
    public async Task Should_Not_Create_Duplicate_Product_Code()
    {
        await _productAppService.CreateAsync(new CreateProductDto
        {
            Code = "RO9",
            Name = "RO 9 Stage"
        });

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            _productAppService.CreateAsync(new CreateProductDto
            {
                Code = "RO9",
                Name = "Duplicate RO 9 Stage"
            }));

        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.ProductCodeAlreadyExists);
    }

    [Fact]
    public async Task Should_Update_And_Deactivate_Product()
    {
        var product = await _productAppService.CreateAsync(new CreateProductDto
        {
            Code = "RO10",
            Name = "RO 10 Stage"
        });

        var updated = await _productAppService.UpdateAsync(product.Id, new UpdateProductDto
        {
            Name = "RO 10 Stage Premium",
            Description = "Updated"
        });

        updated.Name.ShouldBe("RO 10 Stage Premium");

        await _productAppService.DeactivateAsync(product.Id);
        var deactivated = await _productAppService.GetAsync(product.Id);

        deactivated.Status.ShouldBe(CatalogItemStatus.Inactive);
    }
}
