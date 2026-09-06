using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Options;
using Shouldly;
using VPureLux.Migrations;
using VPureLux.Permissions;
using VPureLux.Service;
using VPureLux.EntityFrameworkCore.Warranty;
using VPureLux.Customers;
using VPureLux.Customers.CustomerGroups;
using VPureLux.Inventory;
using VPureLux.Warranty;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Uow;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Service;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class ServiceFoundationTests : VPureLuxEntityFrameworkCoreTestBase
{
    private IServiceWorkAppService App => GetRequiredService<IServiceWorkAppService>();
    private void Enable() => GetRequiredService<IOptions<ServiceOptions>>().Value.IsEnabled = true;
    private static CreateUpdateServiceWorkDto Input(string code = "WORK-01") => new()
        { Code = code, Name = "Labor", Unit = "Visit", DefaultPrice = 100, StandardCost = null };

    [Fact]
    public async Task Disabled_should_block_all_work_apis()
    {
        GetRequiredService<IOptions<ServiceOptions>>().Value.IsEnabled.ShouldBeFalse();
        foreach (var action in new Func<Task>[] {
            async () => await App.GetListAsync(new()), async () => await App.GetAsync(Guid.NewGuid()),
            async () => await App.CreateAsync(Input()), async () => await App.UpdateAsync(Guid.NewGuid(), Input()) })
        {
            (await Should.ThrowAsync<BusinessException>(action)).Code.ShouldBe(ServiceErrorCodes.Disabled);
        }
    }

    [Fact]
    public async Task Each_permission_should_deny_and_allow_execution()
    {
        Enable();
        foreach (var permission in new[] { VPureLuxPermissions.Service.Default, VPureLuxPermissions.Service.View, VPureLuxPermissions.Service.ManageWorks })
        {
            using (WarrantyMatrixAuthorizationService.Deny(permission))
            {
                await Should.ThrowAsync<AbpAuthorizationException>(() => App.CreateAsync(Input(permission)));
                await Should.ThrowAsync<AbpAuthorizationException>(() => App.UpdateAsync(Guid.NewGuid(), Input()));
                if (permission != VPureLuxPermissions.Service.ManageWorks)
                    await Should.ThrowAsync<AbpAuthorizationException>(() => App.GetListAsync(new()));
            }
            (await App.CreateAsync(Input(permission))).Id.ShouldNotBe(Guid.Empty);
        }
        (await App.GetListAsync(new())).TotalCount.ShouldBe(3);
    }

    [Fact]
    public async Task Should_page_filter_sort_and_keep_null_distinct_from_zero()
    {
        Enable();
        for (var i = 0; i < 23; i++)
        {
            var input = Input($"PAGE-{i:D2}");
            input.Name = $"Work {i:D2}";
            input.Status = i < 20 ? ServiceWorkStatus.Active : ServiceWorkStatus.Inactive;
            input.StandardCost = i % 2 == 0 ? null : 0;
            await App.CreateAsync(input);
        }
        var page = await App.GetListAsync(new() { SearchText = "PAGE", Status = ServiceWorkStatus.Active, Sorting = "name desc", SkipCount = 10, MaxResultCount = 10 });
        page.TotalCount.ShouldBe(20);
        page.Items.Count.ShouldBe(10);
        page.Items.First().Code.ShouldBe("PAGE-09");
        page.Items.Last().Code.ShouldBe("PAGE-00");
        page.Items.First().StandardCost.ShouldBe(0);
        page.Items.Last().StandardCost.ShouldBeNull();
        var stable = await App.GetListAsync(new() { Sorting = "unit asc", MaxResultCount = 10 });
        var next = await App.GetListAsync(new() { Sorting = "unit asc", MaxResultCount = 10, SkipCount = 10 });
        stable.Items.Select(x => x.Id).Intersect(next.Items.Select(x => x.Id)).ShouldBeEmpty();
        (await App.GetListAsync(new() { Sorting = "status desc", MaxResultCount = 10 })).Items.First().Status.ShouldBe(ServiceWorkStatus.Inactive);
        (await App.GetListAsync(new() { Sorting = "unsupported asc", MaxResultCount = 10 })).Items.First().Code.ShouldBe("PAGE-00");
    }

    [Fact]
    public async Task Work_edits_should_only_change_catalog_and_reject_stale_or_duplicate_input()
    {
        Enable();
        var work = await App.CreateAsync(Input());
        var edit = Input();
        edit.ConcurrencyStamp = work.ConcurrencyStamp;
        edit.Unit = "Hour";
        edit.DefaultPrice = 200;
        edit.StandardCost = 0;
        var updated = await App.UpdateAsync(work.Id, edit);
        updated.Unit.ShouldBe("Hour");
        updated.StandardCost.ShouldBe(0);
        await Should.ThrowAsync<AbpDbConcurrencyException>(() => App.UpdateAsync(work.Id, edit));
        (await Should.ThrowAsync<BusinessException>(() => App.CreateAsync(Input(" work-01 ")))).Code.ShouldBe(ServiceErrorCodes.WorkCodeAlreadyExists);
        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            (await db.ServiceOrders.CountAsync()).ShouldBe(0);
            (await db.ServicePayments.CountAsync()).ShouldBe(0);
            (await db.SalesOrders.CountAsync()).ShouldBe(0);
            (await db.InventoryTransactions.CountAsync()).ShouldBe(0);
            (await db.AssetReplacementReminders.CountAsync()).ShouldBe(0);
        });
    }

    [Fact]
    public void Original_migration_and_additive_upgrade_should_be_discoverable_and_nonduplicating()
    {
        using var db = new VPureLuxDbContext(new DbContextOptionsBuilder<VPureLuxDbContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=S001_OFFLINE_ONLY;User Id=unused;Password=unused").Options);
        db.GetService<IMigrationsAssembly>().Migrations.Keys.ShouldContain("20260824113235_AddServiceModule");
        var original = new AddServiceModule();
        original.UpOperations.OfType<CreateTableOperation>().Select(x => x.Name).OrderBy(x => x)
            .ShouldBe(new[] { "AppServiceOrderLines", "AppServiceOrders", "AppServicePayments", "AppServiceWorks" });
        var forward = new AddServiceWorkCatalogFields();
        forward.UpOperations.Count.ShouldBe(2);
        foreach (var operation in forward.UpOperations)
        {
            var column = operation.ShouldBeOfType<AddColumnOperation>();
            column.Table.ShouldBe("AppServiceWorks");
            column.IsNullable.ShouldBeTrue();
            column.DefaultValue.ShouldBeNull();
            column.DefaultValueSql.ShouldBeNull();
        }
        forward.UpOperations.OfType<CreateTableOperation>().ShouldBeEmpty();
        forward.UpOperations.OfType<SqlOperation>().ShouldBeEmpty();
        var baseline = new AddSalesPreInstallationV1Foundation().TargetModel;
        var current = forward.TargetModel;
        foreach (var entity in baseline.GetEntityTypes())
        {
            var restored = current.FindEntityType(entity.Name);
            restored.ShouldNotBeNull(entity.Name);
            restored!.ToDebugString(MetadataDebugStringOptions.LongDefault).ShouldBe(entity.ToDebugString(MetadataDebugStringOptions.LongDefault), entity.Name);
        }
        current.GetEntityTypes().Count().ShouldBe(baseline.GetEntityTypes().Count() + 4);
    }

    [Fact]
    public async Task Catalog_update_should_not_rewrite_persisted_historical_line_snapshot()
    {
        Enable();
        var group = await GetRequiredService<ICustomerGroupAppService>().CreateAsync(new() { Code = "S001-CG", Name = "Test group" });
        var customer = await GetRequiredService<ICustomerAppService>().CreateAsync(new() { Code = "S001-C", Name = "Customer", CustomerGroupId = group.Id });
        var warehouse = await GetRequiredService<IWarehouseAppService>().CreateAsync(new() { Code = "S001-W", Name = "Warehouse" });
        var work = await App.CreateAsync(Input());
        var assetId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            db.CustomerAssets.Add(CustomerAsset.CreateExternal(assetId, customer.Id, "S001-ASSET", "S001-C", "Customer", "Machine"));
            // Import a historical persistence fixture, without exposing an order-creation command in S-001.
            var order = (ServiceOrder)Activator.CreateInstance(typeof(ServiceOrder), nonPublic: true)!;
            var values = db.Entry(order).CurrentValues;
            values[nameof(ServiceOrder.Id)] = orderId;
            values[nameof(ServiceOrder.OrderNo)] = "S001-HISTORY";
            values[nameof(ServiceOrder.CustomerId)] = customer.Id;
            values[nameof(ServiceOrder.CustomerAssetId)] = assetId;
            values[nameof(ServiceOrder.WarehouseId)] = warehouse.Id;
            values[nameof(ServiceOrder.OrderDate)] = new DateTime(2026, 8, 24);
            values[nameof(ServiceOrder.Status)] = ServiceOrderStatus.Completed;
            var line = (ServiceOrderLine)Activator.CreateInstance(typeof(ServiceOrderLine), nonPublic: true)!;
            foreach (var (name, value) in new (string, object)[] {
                ("Id", Guid.NewGuid()), ("LineNo", 1), ("LineType", ServiceOrderLineType.Labor),
                ("ServiceWorkId", work.Id), ("ItemCodeSnapshot", "HISTORICAL-WORK"),
                ("ItemNameSnapshot", "Historical labor"), ("UnitSnapshot", "Visit"),
                ("PlannedQuantity", 1), ("ActualQuantity", 1), ("UnitPrice", 100m), ("RevenueAmount", 100m), ("CostAmountSnapshot", 15m) })
                typeof(ServiceOrderLine).GetProperty(name)!.SetValue(line, value);
            var lines = (System.Collections.Generic.List<ServiceOrderLine>)typeof(ServiceOrder)
                .GetField("_lines", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(order)!;
            lines.Add(line);
            db.ServiceOrders.Add(order);
            await db.SaveChangesAsync();
        });
        var edit = Input();
        edit.Name = "Changed template";
        edit.Unit = "Hour";
        edit.DefaultPrice = 999;
        edit.StandardCost = 50;
        edit.ConcurrencyStamp = work.ConcurrencyStamp;
        await App.UpdateAsync(work.Id, edit);
        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            var line = (await db.ServiceOrders.AsNoTracking().Include(x => x.Lines).SingleAsync(x => x.Id == orderId)).Lines.Single();
            line.ItemNameSnapshot.ShouldBe("Historical labor");
            line.ItemCodeSnapshot.ShouldBe("HISTORICAL-WORK");
            line.UnitSnapshot.ShouldBe("Visit");
            line.UnitPrice.ShouldBe(100);
            line.CostAmountSnapshot.ShouldBe(15);
            line.RevenueAmount.ShouldBe(100);
        });
    }

    [Fact]
    public async Task Concurrent_test_context_initialization_should_not_share_an_active_native_connection()
    {
        var manager = GetRequiredService<IUnitOfWorkManager>();
        var provider = GetRequiredService<IDbContextProvider<VPureLuxDbContext>>();
        using var firstUow = manager.Begin(requiresNew: true);
        var first = await provider.GetDbContextAsync();
        first.Database.ProviderName.ShouldBe("Microsoft.EntityFrameworkCore.Sqlite");
        await first.Database.OpenConnectionAsync();
        await using var command = first.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT 1 UNION ALL SELECT 2";
        await using var reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).ShouldBeTrue();
        using var secondUow = manager.Begin(requiresNew: true);
        var second = await provider.GetDbContextAsync();
        second.Database.GetDbConnection().ShouldNotBeSameAs(first.Database.GetDbConnection());
        second.Database.GetConnectionString().ShouldBe(first.Database.GetConnectionString());
        (await second.ServiceWorks.CountAsync()).ShouldBe(0);
    }
}
