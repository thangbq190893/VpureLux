using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shouldly;
using VPureLux.Catalog.Components;
using VPureLux.Customers;
using VPureLux.Customers.CustomerGroups;
using VPureLux.EntityFrameworkCore.Warranty;
using VPureLux.Inventory;
using VPureLux.Migrations;
using VPureLux.Permissions;
using VPureLux.Service;
using VPureLux.Warranty;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EntityFrameworkCore;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Service;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public partial class ServiceOrderWorkflowTests : VPureLuxEntityFrameworkCoreTestBase
{
    private IServiceOrderAppService Orders => GetRequiredService<IServiceOrderAppService>();
    private IServiceWorkAppService Works => GetRequiredService<IServiceWorkAppService>();

    [Fact]
    public async Task Feature_and_each_permission_should_be_enforced_server_side()
    {
        GetRequiredService<IOptions<ServiceOptions>>().Value.IsEnabled = false;
        foreach (var action in new Func<Task>[]
                 {
                     async () => await Orders.GetListAsync(new()),
                     async () => await Orders.GetAsync(Guid.NewGuid()),
                     async () => await Orders.CreateAsync(new CreateServiceOrderDto
                     {
                         CustomerAssetId = Guid.NewGuid(), Lines = [Labor(Guid.NewGuid())]
                     }),
                     async () => await Orders.UpdateAsync(Guid.NewGuid(), new UpdateServiceOrderDto
                     {
                         OrderDate = DateTime.UtcNow, ConcurrencyStamp = "missing", Lines = [Labor(Guid.NewGuid())]
                     }),
                     async () => await Orders.ConfirmAsync(Guid.NewGuid(), new ServiceOrderTransitionDto
                     {
                         ConcurrencyStamp = "missing"
                     }),
                     async () => await Orders.StartAsync(Guid.NewGuid(), new ServiceOrderTransitionDto
                     {
                         ConcurrencyStamp = "missing"
                     }),
                     async () => await Orders.CancelAsync(Guid.NewGuid(), new CancelServiceOrderDto
                     {
                         ConcurrencyStamp = "missing", Reason = "test"
                     }),
                     async () => await Orders.GetAssetOptionsAsync(new()),
                     async () => await Orders.GetMaterialOptionsAsync(new()),
                     async () => await Orders.GetWorkOptionsAsync(new()),
                     async () => await Orders.GetTechnicianOptionsAsync(new()),
                     async () => await Orders.GetWarehouseOptionsAsync()
                 })
        {
            (await Should.ThrowAsync<BusinessException>(action)).Code.ShouldBe(ServiceErrorCodes.Disabled);
        }

        Enable();
        var cases = new (string Permission, Func<Task> Action, string? AllowedError)[]
        {
            (VPureLuxPermissions.Service.Default, async () => await Orders.GetListAsync(new()), null),
            (VPureLuxPermissions.Service.View, async () => await Orders.GetListAsync(new()), null),
            (VPureLuxPermissions.Service.Create, async () => await Orders.CreateAsync(new CreateServiceOrderDto
            {
                CustomerAssetId = Guid.NewGuid(), Lines = [Labor(Guid.NewGuid())]
            }), ServiceErrorCodes.AssetUnavailable),
            (VPureLuxPermissions.Service.Edit, async () => await Orders.UpdateAsync(Guid.NewGuid(), new UpdateServiceOrderDto
            {
                OrderDate = DateTime.UtcNow, ConcurrencyStamp = "missing", Lines = [Labor(Guid.NewGuid())]
            }), ServiceErrorCodes.OrderNotFound),
            (VPureLuxPermissions.Service.Confirm, async () => await Orders.ConfirmAsync(Guid.NewGuid(), new ServiceOrderTransitionDto
            {
                ConcurrencyStamp = "missing"
            }), ServiceErrorCodes.OrderNotFound),
            (VPureLuxPermissions.Service.Cancel, async () => await Orders.CancelAsync(Guid.NewGuid(), new CancelServiceOrderDto
            {
                ConcurrencyStamp = "missing", Reason = "test"
            }), ServiceErrorCodes.OrderNotFound)
        };

        foreach (var item in cases)
        {
            using (WarrantyMatrixAuthorizationService.Deny(item.Permission))
            {
                await Should.ThrowAsync<AbpAuthorizationException>(item.Action);
            }

            if (item.AllowedError == null)
            {
                await item.Action();
            }
            else
            {
                (await Should.ThrowAsync<BusinessException>(item.Action)).Code.ShouldBe(item.AllowedError);
            }
        }

        Permission(nameof(ServiceOrderAppService.StartAsync)).ShouldBe(VPureLuxPermissions.Service.Confirm);
    }

    [Fact]
    public async Task Draft_edit_should_preserve_line_identity_and_unchanged_snapshots()
    {
        var fixture = await CreateFixtureAsync("SNAP");
        var secondWork = await CreateWorkAsync("SNAP-W2", "Second", "Hour", 200, 0);
        var order = await Orders.CreateAsync(new CreateServiceOrderDto
        {
            CustomerAssetId = fixture.AssetId,
            WarehouseId = fixture.WarehouseId,
            Lines = [Labor(fixture.Work.Id, 1, 100), Labor(secondWork.Id, 1, 200)]
        });
        var firstId = order.Lines[0].Id;
        var secondId = order.Lines[1].Id;

        await WithUnitOfWorkAsync(async () =>
        {
            var repository = GetRequiredService<IServiceWorkRepository>();
            var current = await repository.GetAsync(fixture.Work.Id);
            current.Update("Changed template", "Day", 999, 70, ServiceWorkStatus.Active, null);
            await repository.UpdateAsync(current, autoSave: true);
        });

        var updated = await Orders.UpdateAsync(order.Id, ToUpdate(order, "Header only", lines:
        [
            Labor(fixture.Work.Id, 2, 125, firstId),
            Labor(secondWork.Id, 1, 200, secondId)
        ]));

        updated.Lines[0].Id.ShouldBe(firstId);
        updated.Lines[0].ItemName.ShouldBe("SNAP work");
        updated.Lines[0].Unit.ShouldBe("Visit");
        updated.Lines[0].StandardCostSnapshot.ShouldBe(50);
        updated.Lines[0].PlannedQuantity.ShouldBe(2);
        updated.Lines[1].Id.ShouldBe(secondId);

        var replacement = await CreateWorkAsync("SNAP-W3", "Replacement", "Job", 300, null);
        var replaced = await Orders.UpdateAsync(order.Id, ToUpdate(updated, "Explicit replacement", lines:
        [
            Labor(replacement.Id, 1, 300, firstId),
            Labor(secondWork.Id, 1, 200, secondId)
        ]));
        replaced.Lines[0].Id.ShouldBe(firstId);
        replaced.Lines[0].ItemName.ShouldBe("Replacement");
        replaced.Lines[0].Unit.ShouldBe("Job");
        replaced.Lines[0].StandardCostSnapshot.ShouldBeNull();

        var removedAndAdded = await Orders.UpdateAsync(order.Id, ToUpdate(replaced, "Remove one", lines:
        [
            Labor(secondWork.Id, 1, 200, secondId),
            Labor(replacement.Id, 1, 300)
        ]));
        removedAndAdded.Lines[0].Id.ShouldBe(secondId);
        removedAndAdded.Lines.ShouldNotContain(line => line.Id == firstId);
        removedAndAdded.Lines[1].Id.ShouldNotBe(secondId);
    }

    [Fact]
    public async Task Component_snapshot_and_inactive_historical_items_should_survive_header_edit()
    {
        var fixture = await CreateFixtureAsync("MAT");
        var order = await Orders.CreateAsync(new CreateServiceOrderDto
        {
            CustomerAssetId = fixture.AssetId,
            WarehouseId = fixture.WarehouseId,
            Lines = [Material(fixture.Component.Id, 2, 75)]
        });
        var lineId = order.Lines.Single().Id;
        var components = GetRequiredService<IComponentAppService>();
        await components.UpdateAsync(fixture.Component.Id, new UpdateComponentDto
        {
            Name = "Changed current component", Unit = "Box"
        });
        await components.DeactivateAsync(fixture.Component.Id);

        var updated = await Orders.UpdateAsync(order.Id, ToUpdate(order, "Keep history",
            [Material(fixture.Component.Id, 2, 75, lineId)]));

        updated.Lines.Single().Id.ShouldBe(lineId);
        updated.Lines.Single().ItemName.ShouldBe("MAT component");
        updated.Lines.Single().Unit.ShouldBe("Piece");
        updated.Lines.Single().StandardCostSnapshot.ShouldBeNull();
    }

    [Fact]
    public async Task Stale_edit_and_each_stale_transition_should_be_rejected()
    {
        var fixture = await CreateFixtureAsync("CONC");
        var order = await CreateOrderAsync(fixture);
        var staleDraft = order.ConcurrencyStamp;
        var updated = await Orders.UpdateAsync(order.Id, ToUpdate(order, "new note"));
        (await Should.ThrowAsync<BusinessException>(() => Orders.UpdateAsync(order.Id, ToUpdate(order, "stale"))))
            .Code.ShouldBe(ServiceErrorCodes.ConcurrentModification);
        (await Should.ThrowAsync<BusinessException>(() => Orders.ConfirmAsync(order.Id, new ServiceOrderTransitionDto
        {
            ConcurrencyStamp = staleDraft
        }))).Code.ShouldBe(ServiceErrorCodes.ConcurrentModification);

        var confirmed = await Orders.ConfirmAsync(order.Id, new ServiceOrderTransitionDto
        {
            ConcurrencyStamp = updated.ConcurrencyStamp
        });
        (await Should.ThrowAsync<BusinessException>(() => Orders.StartAsync(order.Id, new ServiceOrderTransitionDto
        {
            ConcurrencyStamp = updated.ConcurrencyStamp
        }))).Code.ShouldBe(ServiceErrorCodes.ConcurrentModification);
        var started = await Orders.StartAsync(order.Id, new ServiceOrderTransitionDto
        {
            ConcurrencyStamp = confirmed.ConcurrencyStamp
        });
        started.Status.ShouldBe(ServiceOrderStatus.InProgress);

        var cancellable = await CreateOrderAsync(fixture);
        var changed = await Orders.UpdateAsync(cancellable.Id, ToUpdate(cancellable, "changed"));
        (await Should.ThrowAsync<BusinessException>(() => Orders.CancelAsync(cancellable.Id, new CancelServiceOrderDto
        {
            ConcurrencyStamp = cancellable.ConcurrencyStamp,
            Reason = "stale"
        }))).Code.ShouldBe(ServiceErrorCodes.ConcurrentModification);
        var cancelled = await Orders.CancelAsync(cancellable.Id, new CancelServiceOrderDto
        {
            ConcurrencyStamp = changed.ConcurrencyStamp,
            Reason = "Customer cancelled"
        });
        cancelled.Status.ShouldBe(ServiceOrderStatus.Cancelled);
        cancelled.CancellationReason.ShouldBe("Customer cancelled");
    }

    [Fact]
    public async Task Confirm_and_start_should_not_touch_inventory_care_or_revenue()
    {
        var fixture = await CreateFixtureAsync("SIDE");
        var order = await Orders.CreateAsync(new CreateServiceOrderDto
        {
            CustomerAssetId = fixture.AssetId,
            WarehouseId = fixture.WarehouseId,
            Lines = [Material(fixture.Component.Id), Labor(fixture.Work.Id)]
        });
        var confirmed = await Orders.ConfirmAsync(order.Id, new ServiceOrderTransitionDto { ConcurrencyStamp = order.ConcurrencyStamp });
        await Orders.StartAsync(order.Id, new ServiceOrderTransitionDto { ConcurrencyStamp = confirmed.ConcurrencyStamp });

        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            (await db.InventoryTransactions.CountAsync()).ShouldBe(0);
            (await db.AssetMaintenanceEvents.CountAsync()).ShouldBe(0);
            (await db.AssetReplacementReminders.CountAsync()).ShouldBe(0);
            var persisted = await db.ServiceOrders.AsNoTracking().Include(item => item.Lines).SingleAsync(item => item.Id == order.Id);
            persisted.TotalRevenueAmount.ShouldBe(0);
            persisted.TotalCostAmount.ShouldBe(0);
            persisted.TotalProfitAmount.ShouldBe(0);
            persisted.InventoryTransactionId.ShouldBeNull();
            persisted.Lines.ShouldAllBe(line => line.ActualQuantity == 0 && line.RevenueAmount == 0 && line.CostAmountSnapshot == 0);
        });
    }

    [Fact]
    public async Task List_and_lookups_should_filter_sort_and_reach_page_two()
    {
        var fixture = await CreateFixtureAsync("PAGE");
        for (var index = 0; index < 22; index++)
        {
            await Orders.CreateAsync(new CreateServiceOrderDto
            {
                CustomerAssetId = fixture.AssetId,
                WarehouseId = fixture.WarehouseId,
                OrderDate = new DateTime(2026, 9, 1).AddDays(index),
                Note = $"Page order {index:D2}",
                Lines = [Labor(fixture.Work.Id)]
            });
        }

        var page2 = await Orders.GetListAsync(new GetServiceOrderListInput
        {
            SearchText = fixture.AssetNo,
            Sorting = "orderDate asc",
            SkipCount = 10,
            MaxResultCount = 10
        });
        page2.TotalCount.ShouldBe(22);
        page2.Items.Count.ShouldBe(10);
        page2.Items.First().OrderDate.Date.ShouldBe(new DateTime(2026, 9, 11));
        var unsupported = await Orders.GetListAsync(new GetServiceOrderListInput
        {
            SearchText = fixture.AssetNo,
            Sorting = "drop table asc",
            MaxResultCount = 5
        });
        unsupported.Items.First().OrderDate.Date.ShouldBe(new DateTime(2026, 9, 22));

        for (var index = 0; index < 24; index++)
        {
            await CreateWorkAsync($"LOOK-{index:D2}", $"Lookup {index:D2}", "Visit", index, null);
        }
        var workPage2 = await Orders.GetWorkOptionsAsync(new ServiceLookupInput
        {
            SearchText = "LOOK-", SkipCount = 20, MaxResultCount = 10
        });
        workPage2.TotalCount.ShouldBe(24);
        workPage2.Items.Count.ShouldBe(4);
        workPage2.Items.First().Code.ShouldBe("LOOK-20");
        workPage2.Items.Last().Code.ShouldBe("LOOK-23");

        var assetRepository = GetRequiredService<IRepository<CustomerAsset, Guid>>();
        for (var index = 0; index < 24; index++)
        {
            await assetRepository.InsertAsync(CustomerAsset.CreateExternal(
                Guid.NewGuid(), fixture.CustomerId, $"PAGE-LOOK-{index:D2}", fixture.CustomerCode,
                fixture.CustomerName, $"Lookup machine {index:D2}"), autoSave: true);
        }
        var assetPage2 = await Orders.GetAssetOptionsAsync(new ServiceLookupInput
        {
            CustomerId = fixture.CustomerId,
            SearchText = "PAGE-LOOK-",
            SkipCount = 20,
            MaxResultCount = 10
        });
        assetPage2.TotalCount.ShouldBe(24);
        assetPage2.Items.Count.ShouldBe(4);
        assetPage2.Items.First().AssetNo.ShouldBe("PAGE-LOOK-20");
        assetPage2.Items.Last().AssetNo.ShouldBe("PAGE-LOOK-23");

        var materials = await Orders.GetMaterialOptionsAsync(new ServiceLookupInput
        {
            SearchText = fixture.Component.Code,
            MaxResultCount = 10
        });
        materials.TotalCount.ShouldBe(1);
        materials.Items.Single().Id.ShouldBe(fixture.Component.Id);
    }

    [Fact]
    public void Migration_should_be_additive_and_contain_no_business_dml()
    {
        var migration = new AddServiceOrderWorkflow();
        migration.UpOperations.Count.ShouldBe(2);
        migration.UpOperations.ShouldAllBe(operation => operation is Microsoft.EntityFrameworkCore.Migrations.Operations.AddColumnOperation);
        migration.UpOperations.OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.SqlOperation>().ShouldBeEmpty();
        migration.UpOperations.OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.CreateTableOperation>().ShouldBeEmpty();
    }

    private async Task<Fixture> CreateFixtureAsync(string prefix)
    {
        Enable();
        var group = await GetRequiredService<ICustomerGroupAppService>().CreateAsync(new CreateCustomerGroupDto
        {
            Code = Unique(prefix + "-G"), Name = prefix + " group"
        });
        var customer = await GetRequiredService<ICustomerAppService>().CreateAsync(new CreateCustomerDto
        {
            Code = Unique(prefix + "-C"), Name = prefix + " customer", CustomerGroupId = group.Id
        });
        var warehouse = await GetRequiredService<IWarehouseAppService>().CreateAsync(new CreateWarehouseDto
        {
            Code = Unique(prefix + "-W"), Name = prefix + " warehouse"
        });
        var component = await GetRequiredService<IComponentAppService>().CreateAsync(new CreateComponentDto
        {
            Code = Unique(prefix + "-M"), Name = prefix + " component", Unit = "Piece"
        });
        var work = await CreateWorkAsync(Unique(prefix + "-WORK"), prefix + " work", "Visit", 100, 50);
        var assetId = Guid.NewGuid();
        var assetNo = Unique(prefix + "-ASSET");
        await GetRequiredService<IRepository<CustomerAsset, Guid>>().InsertAsync(
            CustomerAsset.CreateExternal(assetId, customer.Id, assetNo, customer.Code, customer.Name, prefix + " machine"),
            autoSave: true);
        return new Fixture(assetId, assetNo, customer.Id, customer.Code, customer.Name, warehouse.Id, component, work);
    }

    private async Task<ServiceOrderDto> CreateOrderAsync(Fixture fixture) => await Orders.CreateAsync(new CreateServiceOrderDto
    {
        CustomerAssetId = fixture.AssetId,
        WarehouseId = fixture.WarehouseId,
        Lines = [Labor(fixture.Work.Id)]
    });

    private async Task<ServiceWorkDto> CreateWorkAsync(
        string code,
        string name,
        string unit,
        decimal price,
        decimal? cost) => await Works.CreateAsync(new CreateUpdateServiceWorkDto
    {
        Code = code,
        Name = name,
        Unit = unit,
        DefaultPrice = price,
        StandardCost = cost,
        Status = ServiceWorkStatus.Active
    });

    private static ServiceOrderLineInput Labor(
        Guid workId,
        int quantity = 1,
        decimal price = 100,
        Guid? lineId = null) => new()
    {
        Id = lineId,
        LineType = ServiceOrderLineType.Labor,
        CatalogItemId = workId,
        Quantity = quantity,
        UnitPrice = price
    };

    private static ServiceOrderLineInput Material(
        Guid componentId,
        int quantity = 1,
        decimal price = 100,
        Guid? lineId = null) => new()
    {
        Id = lineId,
        LineType = ServiceOrderLineType.Material,
        CatalogItemId = componentId,
        Quantity = quantity,
        UnitPrice = price
    };

    private static UpdateServiceOrderDto ToUpdate(
        ServiceOrderDto order,
        string? note,
        IReadOnlyCollection<ServiceOrderLineInput>? lines = null) => new()
    {
        OrderDate = order.OrderDate,
        ScheduledAt = order.ScheduledAt,
        TechnicianUserId = order.TechnicianUserId,
        ServiceAddress = order.ServiceAddress,
        Note = note,
        ConcurrencyStamp = order.ConcurrencyStamp,
        Lines = (lines ?? order.Lines.Select(line => new ServiceOrderLineInput
        {
            Id = line.Id,
            LineType = line.LineType,
            CatalogItemId = line.ComponentId ?? line.ServiceWorkId ?? Guid.Empty,
            CustomerAssetComponentId = line.CustomerAssetComponentId,
            Quantity = line.PlannedQuantity,
            UnitPrice = line.UnitPrice,
            Note = line.Note
        }).ToList()).ToList()
    };

    private void Enable() => GetRequiredService<IOptions<ServiceOptions>>().Value.IsEnabled = true;
    private static string Unique(string prefix) => prefix + "-" + Guid.NewGuid().ToString("N")[..8];
    private static string? Permission(string methodName) => typeof(ServiceOrderAppService).GetMethods()
        .Single(method => method.Name == methodName).GetCustomAttribute<AuthorizeAttribute>()?.Policy;

    private sealed record Fixture(
        Guid AssetId,
        string AssetNo,
        Guid CustomerId,
        string CustomerCode,
        string CustomerName,
        Guid WarehouseId,
        ComponentDto Component,
        ServiceWorkDto Work);
}
