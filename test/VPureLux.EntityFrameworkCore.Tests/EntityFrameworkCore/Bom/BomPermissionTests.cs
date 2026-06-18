using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Shouldly;
using VPureLux.Bom;
using VPureLux.Permissions;
using Volo.Abp.Authorization.Permissions;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Bom;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class BomPermissionTests : VPureLuxEntityFrameworkCoreTestBase
{
    private readonly IPermissionDefinitionManager _permissionDefinitionManager;

    public BomPermissionTests()
    {
        _permissionDefinitionManager = GetRequiredService<IPermissionDefinitionManager>();
    }

    [Fact]
    public async Task Should_Define_All_Bom_Permissions()
    {
        (await _permissionDefinitionManager.GetAsync(VPureLuxPermissions.Bom.View)).ShouldNotBeNull();
        (await _permissionDefinitionManager.GetAsync(VPureLuxPermissions.Bom.Create)).ShouldNotBeNull();
        (await _permissionDefinitionManager.GetAsync(VPureLuxPermissions.Bom.Publish)).ShouldNotBeNull();
        (await _permissionDefinitionManager.GetAsync(VPureLuxPermissions.Bom.Archive)).ShouldNotBeNull();
    }

    [Fact]
    public void Should_Protect_Bom_AppService_Operations_With_Permissions()
    {
        GetPermission(typeof(BomAppService)).ShouldBe(VPureLuxPermissions.Bom.View);
        GetPermission(nameof(BomAppService.CreateAsync)).ShouldBe(VPureLuxPermissions.Bom.Create);
        GetPermission(nameof(BomAppService.UpdateAsync)).ShouldBe(VPureLuxPermissions.Bom.Create);
        GetPermission(nameof(BomAppService.CloneAsync)).ShouldBe(VPureLuxPermissions.Bom.Create);
        GetPermission(nameof(BomAppService.PublishAsync)).ShouldBe(VPureLuxPermissions.Bom.Publish);
        GetPermission(nameof(BomAppService.ArchiveAsync)).ShouldBe(VPureLuxPermissions.Bom.Archive);
    }

    private static string? GetPermission(string methodName)
    {
        var method = typeof(BomAppService).GetMethods().Single(x => x.Name == methodName);
        return method.GetCustomAttribute<AuthorizeAttribute>()?.Policy;
    }

    private static string? GetPermission(MemberInfo member)
    {
        return member.GetCustomAttribute<AuthorizeAttribute>()?.Policy;
    }
}
