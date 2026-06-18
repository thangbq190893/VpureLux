using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using VPureLux.Permissions;
using VPureLux.Sales;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.EntityFrameworkCore;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Sales;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class SalesRepositoryAndPermissionTests : VPureLuxEntityFrameworkCoreTestBase
{
    [Fact]
    public async Task Should_Define_And_Enforce_All_Sales_Permissions()
    {
        var manager = GetRequiredService<IPermissionDefinitionManager>();
        foreach (var permission in new[]
                 {
                     VPureLuxPermissions.Sales.View, VPureLuxPermissions.Sales.Create, VPureLuxPermissions.Sales.Edit,
                     VPureLuxPermissions.Sales.OverridePrice, VPureLuxPermissions.Sales.Confirm, VPureLuxPermissions.Sales.Cancel,
                     VPureLuxPermissions.Sales.ViewCost, VPureLuxPermissions.Sales.ViewProfit, VPureLuxPermissions.Sales.ViewCustomerHistory
                 })
        {
            (await manager.GetAsync(permission)).ShouldNotBeNull();
        }

        Permission(typeof(SalesOrderAppService)).ShouldBe(VPureLuxPermissions.Sales.View);
        Permission(nameof(SalesOrderAppService.CreateAsync)).ShouldBe(VPureLuxPermissions.Sales.Create);
        Permission(nameof(SalesOrderAppService.AddLineAsync)).ShouldBe(VPureLuxPermissions.Sales.Edit);
        Permission(nameof(SalesOrderAppService.ConfirmAsync)).ShouldBe(VPureLuxPermissions.Sales.Confirm);
        Permission(nameof(SalesOrderAppService.CancelAsync)).ShouldBe(VPureLuxPermissions.Sales.Cancel);
    }

    [Fact]
    public async Task Should_Have_Required_Tables_Indexes_Precision_Fks_And_Concurrency()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            var order = db.Model.FindEntityType(typeof(SalesOrder))!;
            order.GetTableName().ShouldBe("AppSalesOrders");
            order.FindIndex(order.FindProperty(nameof(SalesOrder.OrderNo))!)!.IsUnique.ShouldBeTrue();
            order.GetIndexes().Single(x => x.GetDatabaseName() == SalesOrderConfiguration.ConfirmationKeyUniqueIndexName)
                .GetFilter()!.ShouldContain("IsDeleted");
            order.FindProperty(nameof(SalesOrder.RowVersion))!.IsConcurrencyToken.ShouldBeTrue();
            order.FindProperty(nameof(SalesOrder.TotalRevenueAmount))!.GetScale().ShouldBe(2);

            var line = order.FindNavigation(nameof(SalesOrder.Lines))!.TargetEntityType;
            line.GetTableName().ShouldBe("AppSalesOrderLines");
            line.FindProperty(nameof(SalesOrderLine.Quantity))!.GetScale().ShouldBe(4);
            line.FindProperty(nameof(SalesOrderLine.MarginPercent))!.GetPrecision().ShouldBe(9);
            line.FindProperty(nameof(SalesOrderLine.MarginPercent))!.GetScale().ShouldBe(4);
            line.FindProperty(nameof(SalesOrderLine.OverrideReason))!.GetColumnType().ShouldBe("nvarchar(500)");
            line.GetForeignKeys().Where(x => !x.IsOwnership).ShouldAllBe(x => x.DeleteBehavior == DeleteBehavior.Restrict);
            db.Model.FindEntityType(typeof(NumberSequence))!.GetTableName().ShouldBe("AppNumberSequences");
        });
    }

    [Fact]
    public async Task Number_Sequence_Should_Be_Unique_And_Use_Required_Format()
    {
        var manager = GetRequiredService<SalesManager>();
        var customer = Guid.NewGuid();
        var warehouse = Guid.NewGuid();
        var date = new DateTime(2026, 6, 15);
        var first = await manager.CreateAsync(customer, warehouse, date);
        var second = await manager.CreateAsync(customer, warehouse, date);
        first.OrderNo.ShouldMatch(@"^SO-202606-\d{6}$");
        second.OrderNo.ShouldNotBe(first.OrderNo);
    }

    private static string? Permission(MemberInfo member) => member.GetCustomAttribute<AuthorizeAttribute>()?.Policy;
    private static string? Permission(string method) => Permission(typeof(SalesOrderAppService).GetMethod(method)!);
}
