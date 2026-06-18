using System;
using System.Threading.Tasks;
using Shouldly;
using VPureLux.Bom;
using VPureLux.Catalog;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using Volo.Abp;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Bom;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class BomAppServiceTests : VPureLuxEntityFrameworkCoreTestBase
{
    private readonly IBomAppService _bomAppService;
    private readonly IProductAppService _productAppService;
    private readonly IComponentAppService _componentAppService;

    public BomAppServiceTests()
    {
        _bomAppService = GetRequiredService<IBomAppService>();
        _productAppService = GetRequiredService<IProductAppService>();
        _componentAppService = GetRequiredService<IComponentAppService>();
    }

    [Fact]
    public async Task Should_Reject_Missing_Product()
    {
        var exception = await Should.ThrowAsync<BusinessException>(
            () => _bomAppService.CreateAsync(Guid.NewGuid(), CreateInput(Guid.NewGuid())));

        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.ProductNotFound);
    }

    [Fact]
    public async Task Should_Reject_Inactive_Product()
    {
        var product = await CreateProductAsync();
        var component = await CreateComponentAsync();
        await _productAppService.DeactivateAsync(product.Id);

        var exception = await Should.ThrowAsync<BusinessException>(
            () => _bomAppService.CreateAsync(product.Id, CreateInput(component.Id)));

        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task Should_Reject_Missing_Component()
    {
        var product = await CreateProductAsync();

        var exception = await Should.ThrowAsync<BusinessException>(
            () => _bomAppService.CreateAsync(product.Id, CreateInput(Guid.NewGuid())));

        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.ComponentNotFound);
    }

    [Fact]
    public async Task Should_Reject_Inactive_Component()
    {
        var product = await CreateProductAsync();
        var component = await CreateComponentAsync();
        await _componentAppService.DeactivateAsync(component.Id);

        var exception = await Should.ThrowAsync<BusinessException>(
            () => _bomAppService.CreateAsync(product.Id, CreateInput(component.Id)));

        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.ComponentNotActive);
    }

    [Fact]
    public async Task Should_Publish_Using_Application_Workflow()
    {
        var bom = await CreateBomAsync();

        await _bomAppService.PublishAsync(bom.Id);

        (await _bomAppService.GetAsync(bom.Id)).Status.ShouldBe(BomStatus.Published);
    }

    [Fact]
    public async Task Should_Clone_Using_Application_Workflow()
    {
        var bom = await CreateBomAsync();

        var result = await _bomAppService.CloneAsync(bom.Id, new CloneBomVersionDto
        {
            EffectiveFrom = DateTime.UtcNow.AddDays(1)
        });
        var clone = await _bomAppService.GetAsync(result.NewBomVersionId);

        clone.Status.ShouldBe(BomStatus.Draft);
        clone.VersionNo.ShouldBe(bom.VersionNo + 1);
        clone.Items.Count.ShouldBe(bom.Items.Count);
    }

    [Fact]
    public async Task Should_Reject_Published_Edit_With_Documented_Error()
    {
        var bom = await CreateBomAsync();
        await _bomAppService.PublishAsync(bom.Id);

        var exception = await Should.ThrowAsync<BusinessException>(
            () => _bomAppService.UpdateAsync(bom.Id, new UpdateBomVersionDto
            {
                Items = bom.Items.ConvertAll(x => new CreateBomItemDto
                {
                    ComponentId = x.ComponentId,
                    Quantity = x.Quantity + 1
                })
            }));

        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.PublishedBomCannotBeModified);
    }

    [Fact]
    public async Task Should_Update_Draft_Bom_When_Component_Changes()
    {
        var product = await CreateProductAsync();
        var firstComponent = await CreateComponentAsync();
        var secondComponent = await CreateComponentAsync();
        var bom = await _bomAppService.CreateAsync(product.Id, CreateInput(firstComponent.Id));

        await _bomAppService.UpdateAsync(bom.Id, new UpdateBomVersionDto
        {
            Items =
            {
                new CreateBomItemDto
                {
                    ComponentId = secondComponent.Id,
                    Quantity = 2
                }
            }
        });

        var updated = await _bomAppService.GetAsync(bom.Id);
        updated.Items.Count.ShouldBe(1);
        updated.Items[0].ComponentId.ShouldBe(secondComponent.Id);
        updated.Items[0].Quantity.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Reject_Fractional_Bom_Quantity()
    {
        var product = await CreateProductAsync();
        var component = await CreateComponentAsync();

        var exception = await Should.ThrowAsync<BusinessException>(
            () => _bomAppService.CreateAsync(product.Id, CreateInput(component.Id, 1.5m)));

        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task Should_Reject_Second_Published_Bom()
    {
        var product = await CreateProductAsync();
        var component = await CreateComponentAsync();
        var first = await _bomAppService.CreateAsync(product.Id, CreateInput(component.Id));
        var second = await _bomAppService.CreateAsync(product.Id, CreateInput(component.Id));
        await _bomAppService.PublishAsync(first.Id);

        var exception = await Should.ThrowAsync<BusinessException>(() => _bomAppService.PublishAsync(second.Id));

        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.OnlyOneActiveBomAllowed);
    }

    private async Task<BomVersionDto> CreateBomAsync()
    {
        var product = await CreateProductAsync();
        var component = await CreateComponentAsync();
        return await _bomAppService.CreateAsync(product.Id, CreateInput(component.Id));
    }

    private Task<ProductDto> CreateProductAsync()
    {
        return _productAppService.CreateAsync(new CreateProductDto
        {
            Code = UniqueCode("BOMP"),
            Name = "BOM Test Product"
        });
    }

    private Task<ComponentDto> CreateComponentAsync()
    {
        return _componentAppService.CreateAsync(new CreateComponentDto
        {
            Code = UniqueCode("BOMC"),
            Name = "BOM Test Component",
            Unit = "Piece"
        });
    }

    private static CreateBomVersionDto CreateInput(Guid componentId, decimal quantity = 1)
    {
        return new CreateBomVersionDto
        {
            EffectiveFrom = DateTime.UtcNow,
            Items =
            {
                new CreateBomItemDto { ComponentId = componentId, Quantity = quantity }
            }
        };
    }

    private static string UniqueCode(string prefix)
    {
        return prefix + Guid.NewGuid().ToString("N")[..8];
    }
}
