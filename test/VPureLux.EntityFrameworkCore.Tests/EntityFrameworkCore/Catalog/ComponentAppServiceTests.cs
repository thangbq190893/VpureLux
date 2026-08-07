using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Shouldly;
using VPureLux.Catalog;
using VPureLux.Catalog.Components;
using Volo.Abp;
using Volo.Abp.Timing;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Catalog;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class ComponentAppServiceTests : VPureLuxEntityFrameworkCoreTestBase
{
    private readonly IComponentAppService _componentAppService;
    private readonly IDistributedCache _cache;
    private readonly IClock _clock;

    public ComponentAppServiceTests()
    {
        _componentAppService = GetRequiredService<IComponentAppService>();
        _cache = GetRequiredService<IDistributedCache>();
        _clock = GetRequiredService<IClock>();
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
    public async Task Should_Generate_Component_Code_When_Blank()
    {
        await ResetMaterialSequenceAsync();

        var component = await _componentAppService.CreateAsync(new CreateComponentDto
        {
            Code = " ",
            Name = "Auto Component Code",
            Unit = "Piece"
        });

        component.Code.ShouldBe($"MAT-{DatePart()}0001");
    }

    [Fact]
    public async Task Should_Seed_Component_Code_From_Existing_Max_Suffix()
    {
        await ResetMaterialSequenceAsync();
        var datePart = DatePart();
        await _componentAppService.CreateAsync(new CreateComponentDto
        {
            Code = $"MAT-{datePart}0003",
            Name = "Seed Component 3",
            Unit = "Piece"
        });
        await _componentAppService.CreateAsync(new CreateComponentDto
        {
            Code = $"MAT-{datePart}0009",
            Name = "Seed Component 9",
            Unit = "Piece"
        });

        var component = await _componentAppService.CreateAsync(new CreateComponentDto
        {
            Name = "Seeded Auto Component",
            Unit = "Piece"
        });

        component.Code.ShouldBe($"MAT-{datePart}0010");
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

        await _componentAppService.ActivateAsync(component.Id);
        (await _componentAppService.GetAsync(component.Id)).Status.ShouldBe(CatalogItemStatus.Active);
    }

    private string DatePart() => _clock.Now.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

    private Task ResetMaterialSequenceAsync() =>
        _cache.RemoveAsync($"Sequence:Material:{DatePart()}");
}
