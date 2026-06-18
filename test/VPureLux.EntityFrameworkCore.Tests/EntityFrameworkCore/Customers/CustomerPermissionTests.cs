using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Shouldly;
using VPureLux.Customers;
using VPureLux.Permissions;
using Volo.Abp.Authorization.Permissions;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Customers;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class CustomerPermissionTests : VPureLuxEntityFrameworkCoreTestBase
{
    private readonly IPermissionDefinitionManager _permissions;
    public CustomerPermissionTests() { _permissions = GetRequiredService<IPermissionDefinitionManager>(); }

    [Fact]
    public async Task Should_Define_All_Customer_Permissions()
    {
        foreach (var permission in new[]
        {
            VPureLuxPermissions.Customers.View, VPureLuxPermissions.Customers.Create, VPureLuxPermissions.Customers.Edit,
            VPureLuxPermissions.Customers.ManageStatus, VPureLuxPermissions.CustomerGroups.View,
            VPureLuxPermissions.CustomerGroups.Create, VPureLuxPermissions.CustomerGroups.Edit,
            VPureLuxPermissions.CustomerGroups.ManageStatus
        })
        {
            (await _permissions.GetAsync(permission)).ShouldNotBeNull();
        }
    }

    [Fact]
    public void Should_Protect_Application_Workflows()
    {
        GetPermission(typeof(CustomerAppService)).ShouldBe(VPureLuxPermissions.Customers.Default);
        GetPermission(typeof(CustomerGroupAppService)).ShouldBe(VPureLuxPermissions.CustomerGroups.Default);
        GetPermission(typeof(CustomerAppService), nameof(CustomerAppService.CreateAsync)).ShouldBe(VPureLuxPermissions.Customers.Create);
        GetPermission(typeof(CustomerAppService), nameof(CustomerAppService.UpdateAsync)).ShouldBe(VPureLuxPermissions.Customers.Edit);
        GetPermission(typeof(CustomerAppService), nameof(CustomerAppService.ActivateAsync)).ShouldBe(VPureLuxPermissions.Customers.ManageStatus);
        GetPermission(typeof(CustomerGroupAppService), nameof(CustomerGroupAppService.DeactivateAsync)).ShouldBe(VPureLuxPermissions.CustomerGroups.ManageStatus);
    }

    private static string? GetPermission(MemberInfo member) => member.GetCustomAttribute<AuthorizeAttribute>()?.Policy;
    private static string? GetPermission(System.Type type, string method) =>
        type.GetMethods().Single(x => x.Name == method).GetCustomAttribute<AuthorizeAttribute>()?.Policy;
}
