using System.Threading.Tasks;
using VPureLux.Permissions;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Uow;

namespace VPureLux.Permissions;

public class VPureLuxPermissionDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private const string AdminRoleName = "admin";

    private readonly IPermissionDataSeeder _permissionDataSeeder;

    public VPureLuxPermissionDataSeedContributor(IPermissionDataSeeder permissionDataSeeder)
    {
        _permissionDataSeeder = permissionDataSeeder;
    }

    [UnitOfWork]
    public virtual async Task SeedAsync(DataSeedContext context)
    {
        await _permissionDataSeeder.SeedAsync(
            RolePermissionValueProvider.ProviderName,
            AdminRoleName,
            [
                VPureLuxPermissions.Suppliers.Default,
                VPureLuxPermissions.Service.Default,
                VPureLuxPermissions.Service.View,
                VPureLuxPermissions.Service.Create,
                VPureLuxPermissions.Service.Edit,
                VPureLuxPermissions.Service.Confirm,
                VPureLuxPermissions.Service.Cancel,
                VPureLuxPermissions.Service.ManageWorks,
                VPureLuxPermissions.Service.ManagePayments,
                VPureLuxPermissions.Service.ViewCost,
                VPureLuxPermissions.Service.ViewProfit,
                VPureLuxPermissions.Reports.Service.View,
                VPureLuxPermissions.Reports.Consolidated.View,
                VPureLuxPermissions.Suppliers.View,
                VPureLuxPermissions.Suppliers.Create,
                VPureLuxPermissions.Suppliers.Edit,
                VPureLuxPermissions.Suppliers.Delete,
                VPureLuxPermissions.OperatingCosts.Default,
                VPureLuxPermissions.OperatingCosts.View,
                VPureLuxPermissions.OperatingCosts.ManageEntries,
                VPureLuxPermissions.OperatingCosts.ManageCategories,
                VPureLuxPermissions.OperatingCosts.Delete,
                VPureLuxPermissions.Warranty.Default,
                VPureLuxPermissions.Warranty.View,
                VPureLuxPermissions.Warranty.ManagePolicies,
                VPureLuxPermissions.Warranty.ManageMachines,
                VPureLuxPermissions.Warranty.ManageSyncFailures,
                VPureLuxPermissions.Warranty.ManageInstallations,
                VPureLuxPermissions.Warranty.ManageAssets,
                VPureLuxPermissions.Warranty.ManageReminders
            ],
            context.TenantId);
    }
}
