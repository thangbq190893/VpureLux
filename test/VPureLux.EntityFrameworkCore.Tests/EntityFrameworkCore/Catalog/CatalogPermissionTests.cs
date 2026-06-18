using System.Threading.Tasks;
using Shouldly;
using VPureLux.Permissions;
using Volo.Abp.Authorization.Permissions;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Catalog;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class CatalogPermissionTests : VPureLuxEntityFrameworkCoreTestBase
{
    private readonly IPermissionDefinitionManager _permissionDefinitionManager;

    public CatalogPermissionTests()
    {
        _permissionDefinitionManager = GetRequiredService<IPermissionDefinitionManager>();
    }

    [Fact]
    public async Task Should_Define_Catalog_Permissions()
    {
        (await _permissionDefinitionManager.GetAsync(VPureLuxPermissions.Catalog.Components.View)).ShouldNotBeNull();
        (await _permissionDefinitionManager.GetAsync(VPureLuxPermissions.Catalog.Components.Create)).ShouldNotBeNull();
        (await _permissionDefinitionManager.GetAsync(VPureLuxPermissions.Catalog.Components.Edit)).ShouldNotBeNull();
        (await _permissionDefinitionManager.GetAsync(VPureLuxPermissions.Catalog.Products.View)).ShouldNotBeNull();
        (await _permissionDefinitionManager.GetAsync(VPureLuxPermissions.Catalog.Products.Create)).ShouldNotBeNull();
        (await _permissionDefinitionManager.GetAsync(VPureLuxPermissions.Catalog.Products.Edit)).ShouldNotBeNull();
    }
}
