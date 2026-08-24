using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using VPureLux.Permissions;
using VPureLux.Service;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.EntityFrameworkCore;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Service;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class ServiceModelAndPermissionTests : VPureLuxEntityFrameworkCoreTestBase
{
    [Fact]
    public async Task Should_Define_Service_Permissions()
    {
        var manager = GetRequiredService<IPermissionDefinitionManager>();
        foreach (var permission in new[]
                 {
                     VPureLuxPermissions.Service.View, VPureLuxPermissions.Service.Create, VPureLuxPermissions.Service.Edit,
                     VPureLuxPermissions.Service.Confirm, VPureLuxPermissions.Service.Complete, VPureLuxPermissions.Service.Cancel,
                     VPureLuxPermissions.Service.ManagePayments, VPureLuxPermissions.Service.ManageWorks,
                     VPureLuxPermissions.Service.ViewCost, VPureLuxPermissions.Service.ViewProfit
                 })
        {
            (await manager.GetAsync(permission)).ShouldNotBeNull();
        }
        Permission(typeof(ServiceAppService)).ShouldBe(VPureLuxPermissions.Service.Default);
        Permission(nameof(ServiceAppService.CreateAsync)).ShouldBe(VPureLuxPermissions.Service.Create);
        Permission(nameof(ServiceAppService.CompleteAsync)).ShouldBe(VPureLuxPermissions.Service.Complete);
    }

    [Fact]
    public async Task Should_Map_Service_Tables_Indexes_And_Integer_Quantities()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            var order = db.Model.FindEntityType(typeof(ServiceOrder))!;
            order.GetTableName().ShouldBe("AppServiceOrders");
            order.FindProperty(nameof(ServiceOrder.RowVersion))!.IsConcurrencyToken.ShouldBeTrue();
            order.GetIndexes().Single(x => x.GetDatabaseName() == ServiceOrderConfiguration.OrderNoUniqueIndexName).IsUnique.ShouldBeTrue();
            order.GetIndexes().Single(x => x.GetDatabaseName() == ServiceOrderConfiguration.CompletionKeyUniqueIndexName).IsUnique.ShouldBeTrue();
            var line = order.FindNavigation(nameof(ServiceOrder.Lines))!.TargetEntityType;
            line.GetTableName().ShouldBe("AppServiceOrderLines");
            line.FindProperty(nameof(ServiceOrderLine.PlannedQuantity))!.ClrType.ShouldBe(typeof(int));
            line.FindProperty(nameof(ServiceOrderLine.ActualQuantity))!.ClrType.ShouldBe(typeof(int));
            line.GetForeignKeys().Where(x => !x.IsOwnership).ShouldAllBe(x => x.DeleteBehavior == DeleteBehavior.Restrict);
            db.Model.FindEntityType(typeof(ServiceWork))!.GetTableName().ShouldBe("AppServiceWorks");
            db.Model.FindEntityType(typeof(ServicePayment))!.GetTableName().ShouldBe("AppServicePayments");
        });
    }

    private static string? Permission(MemberInfo member) => member.GetCustomAttribute<AuthorizeAttribute>()?.Policy;
    private static string? Permission(string method) => Permission(typeof(ServiceAppService).GetMethod(method)!);
}
