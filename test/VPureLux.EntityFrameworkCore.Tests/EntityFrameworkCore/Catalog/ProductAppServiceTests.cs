using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Shouldly;
using VPureLux.Catalog;
using VPureLux.Catalog.Products;
using Volo.Abp;
using Volo.Abp.Timing;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Catalog;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class ProductAppServiceTests : VPureLuxEntityFrameworkCoreTestBase
{
    private readonly IProductAppService _productAppService;
    private readonly IDistributedCache _cache;
    private readonly IClock _clock;

    public ProductAppServiceTests()
    {
        _productAppService = GetRequiredService<IProductAppService>();
        _cache = GetRequiredService<IDistributedCache>();
        _clock = GetRequiredService<IClock>();
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
    public async Task Should_Generate_Product_Code_When_Blank()
    {
        await ResetProductSequenceAsync();

        var product = await _productAppService.CreateAsync(new CreateProductDto
        {
            Code = " ",
            Name = "Auto Product Code"
        });

        product.Code.ShouldBe($"PROD-{DatePart()}0001");
    }

    [Fact]
    public async Task Should_Seed_Product_Code_From_Existing_Max_Suffix()
    {
        await ResetProductSequenceAsync();
        var datePart = DatePart();
        await _productAppService.CreateAsync(new CreateProductDto
        {
            Code = $"PROD-{datePart}0003",
            Name = "Seed Product 3"
        });
        await _productAppService.CreateAsync(new CreateProductDto
        {
            Code = $"PROD-{datePart}0009",
            Name = "Seed Product 9"
        });

        var product = await _productAppService.CreateAsync(new CreateProductDto
        {
            Name = "Seeded Auto Product"
        });

        product.Code.ShouldBe($"PROD-{datePart}0010");
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

    private string DatePart() => _clock.Now.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

    private Task ResetProductSequenceAsync() =>
        _cache.RemoveAsync($"Sequence:Product:{DatePart()}");
}
