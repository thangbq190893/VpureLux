using System.Threading.Tasks;
using Shouldly;
using VPureLux.Catalog;
using VPureLux.Catalog.Components;
using Volo.Abp;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Catalog;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class ComponentAppServiceTests : VPureLuxEntityFrameworkCoreTestBase
{
    private readonly IComponentAppService _componentAppService;

    public ComponentAppServiceTests()
    {
        _componentAppService = GetRequiredService<IComponentAppService>();
    }

    [Fact]
    public async Task Should_Create_Component()
    {
        var component = await _componentAppService.CreateAsync(new CreateComponentDto
        {
            Code = "PP001",
            Name = "PP Filter",
            Unit = "Piece"
        });

        component.Id.ShouldNotBe(default);
        component.Status.ShouldBe(CatalogItemStatus.Active);
    }

    [Fact]
    public async Task Should_Not_Create_Duplicate_Component_Code()
    {
        await _componentAppService.CreateAsync(new CreateComponentDto
        {
            Code = "PP002",
            Name = "PP Filter",
            Unit = "Piece"
        });

        var exception = await Should.ThrowAsync<BusinessException>(() =>
            _componentAppService.CreateAsync(new CreateComponentDto
            {
                Code = "PP002",
                Name = "Duplicate PP Filter",
                Unit = "Piece"
            }));

        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.ComponentCodeAlreadyExists);
    }

    [Fact]
    public async Task Should_Update_And_Deactivate_Component()
    {
        var component = await _componentAppService.CreateAsync(new CreateComponentDto
        {
            Code = "PP003",
            Name = "PP Filter",
            Unit = "Piece"
        });

        var updated = await _componentAppService.UpdateAsync(component.Id, new UpdateComponentDto
        {
            Name = "PP Filter 5 Micron",
            Description = "Updated",
            Unit = "Unit"
        });

        updated.Name.ShouldBe("PP Filter 5 Micron");

        await _componentAppService.DeactivateAsync(component.Id);
        var deactivated = await _componentAppService.GetAsync(component.Id);

        deactivated.Status.ShouldBe(CatalogItemStatus.Inactive);
    }
}
