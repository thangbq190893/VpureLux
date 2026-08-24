using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Shouldly;
using VPureLux.Permissions;
using VPureLux.Warranty;
using Volo.Abp.Authorization.Permissions;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Warranty;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class WarrantyPermissionTests : VPureLuxEntityFrameworkCoreTestBase
{
    [Fact]
    public async Task Should_define_warranty_configuration_permissions()
    {
        var definitions = GetRequiredService<IPermissionDefinitionManager>();

        (await definitions.GetAsync(VPureLuxPermissions.Warranty.View)).ShouldNotBeNull();
        (await definitions.GetAsync(VPureLuxPermissions.Warranty.ManagePolicies)).ShouldNotBeNull();
        (await definitions.GetAsync(VPureLuxPermissions.Warranty.ManageMachines)).ShouldNotBeNull();
        (await definitions.GetAsync(VPureLuxPermissions.Warranty.ManageSyncFailures)).ShouldNotBeNull();
        (await definitions.GetAsync(VPureLuxPermissions.Warranty.ManageInstallations)).ShouldNotBeNull();
        (await definitions.GetAsync(VPureLuxPermissions.Warranty.ManageAssets)).ShouldNotBeNull();
        (await definitions.GetAsync(VPureLuxPermissions.Warranty.ManageReminders)).ShouldNotBeNull();
    }

    [Fact]
    public void Should_protect_warranty_configuration_commands()
    {
        Permission(nameof(WarrantyAppService.SetPolicyAsync))
            .ShouldBe(VPureLuxPermissions.Warranty.ManagePolicies);
        Permission(nameof(WarrantyAppService.SetMachineSettingAsync))
            .ShouldBe(VPureLuxPermissions.Warranty.ManageMachines);
        Permission(nameof(WarrantyAppService.GetMachineSettingsByProductIdsAsync))
            .ShouldBe(VPureLuxPermissions.Warranty.ManageMachines);
        Permission(nameof(WarrantyAppService.GetPoliciesByComponentIdsAsync))
            .ShouldBe(VPureLuxPermissions.Warranty.ManagePolicies);
        Permission(nameof(WarrantyAppService.RetrySyncFailureAsync))
            .ShouldBe(VPureLuxPermissions.Warranty.ManageSyncFailures);
        Permission(nameof(WarrantyAppService.ConfirmInstallationAsync))
            .ShouldBe(VPureLuxPermissions.Warranty.ManageInstallations);
        Permission(nameof(WarrantyAppService.CreateExternalAssetAsync))
            .ShouldBe(VPureLuxPermissions.Warranty.ManageAssets);
        Permission(nameof(WarrantyAppService.SuspendAssetAsync))
            .ShouldBe(VPureLuxPermissions.Warranty.ManageReminders);
    }

    private static string? Permission(string methodName) =>
        typeof(WarrantyAppService).GetMethods()
            .Single(method => method.Name == methodName)
            .GetCustomAttribute<AuthorizeAttribute>()?.Policy;
}
