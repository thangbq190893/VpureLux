using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using VPureLux.Catalog.Products;
using VPureLux.Catalog.Components;
using VPureLux.Customers;
using VPureLux.Customers.CustomerGroups;
using VPureLux.Warranty;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EntityFrameworkCore;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Warranty;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class CustomerCareSchemaTests : VPureLuxEntityFrameworkCoreTestBase
{
    [Fact]
    public async Task Machine_settings_should_default_false_and_page_filter_in_database()
    {
        var products = GetRequiredService<IProductAppService>();
        var warranty = GetRequiredService<IWarrantyAppService>();
        var prefix = Unique("MCH");
        var first = await products.CreateAsync(new CreateProductDto { Code = prefix + "A", Name = "Machine A" });
        await products.CreateAsync(new CreateProductDto { Code = prefix + "B", Name = "Machine B" });
        await products.CreateAsync(new CreateProductDto { Code = prefix + "C", Name = "Machine C" });

        var page = await warranty.GetMachineSettingListAsync(new GetProductMachineSettingListInput
        {
            SearchText = prefix,
            Sorting = "productCode asc",
            SkipCount = 1,
            MaxResultCount = 1
        });

        page.TotalCount.ShouldBe(3);
        page.Items.Count.ShouldBe(1);
        page.Items.Single().IsMachine.ShouldBeFalse();

        await warranty.SetMachineSettingAsync(first.Id, new SetProductMachineSettingDto
        {
            IsMachine = true,
            Note = "Water purifier"
        });
        var machines = await warranty.GetMachineSettingListAsync(new GetProductMachineSettingListInput
        {
            SearchText = prefix,
            IsMachine = true,
            MaxResultCount = 10
        });

        machines.TotalCount.ShouldBe(1);
        machines.Items.Single().ProductId.ShouldBe(first.Id);
        machines.Items.Single().Note.ShouldBe("Water purifier");
    }

    [Fact]
    public async Task Should_persist_external_asset_positions_history_and_sync_failure_without_sales_source()
    {
        var groups = GetRequiredService<ICustomerGroupAppService>();
        var customers = GetRequiredService<ICustomerAppService>();
        var products = GetRequiredService<IProductAppService>();
        var group = await groups.CreateAsync(new CreateCustomerGroupDto
        {
            Code = Unique("CCG"),
            Name = "CustomerCare group"
        });
        var customer = await customers.CreateAsync(new CreateCustomerDto
        {
            Code = Unique("CCC"),
            Name = "CustomerCare customer",
            CustomerGroupId = group.Id
        });
        var product = await products.CreateAsync(new CreateProductDto
        {
            Code = Unique("CCP"),
            Name = "Mapped external machine"
        });

        var setting = new ProductMachineSetting(Guid.NewGuid(), product.Id, true, "Tracked machine");
        var asset = CustomerAsset.CreateExternal(
            Guid.NewGuid(),
            customer.Id,
            Unique("EXT"),
            customer.Code,
            customer.Name,
            "External model",
            brand: "External brand",
            productId: product.Id,
            productCode: product.Code,
            productName: product.Name);
        var position = new CustomerAssetComponent(
            Guid.NewGuid(),
            asset.Id,
            "CORE-01",
            "Loi 1",
            null,
            null,
            null,
            null,
            1,
            null,
            pendingInstallation: false);
        var maintenanceEvent = new AssetMaintenanceEvent(
            Guid.NewGuid(),
            asset.Id,
            position.Id,
            AssetMaintenanceEventType.Inspection,
            AssetMaintenanceSourceType.ExternalOnboarding,
            DateTime.UtcNow,
            Unique("EVT"),
            note: "Initial external-machine review");
        var failure = new CustomerCareSyncFailure(
            Guid.NewGuid(),
            Unique("FAIL"),
            "Test isolated failure",
            DateTime.UtcNow);

        await WithUnitOfWorkAsync(async () =>
        {
            await GetRequiredService<IRepository<ProductMachineSetting, Guid>>().InsertAsync(setting);
            await GetRequiredService<IRepository<CustomerAsset, Guid>>().InsertAsync(asset);
            await GetRequiredService<IRepository<CustomerAssetComponent, Guid>>().InsertAsync(position);
            await GetRequiredService<IRepository<AssetMaintenanceEvent, Guid>>().InsertAsync(maintenanceEvent);
            await GetRequiredService<IRepository<CustomerCareSyncFailure, Guid>>().InsertAsync(failure, autoSave: true);
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            var persistedAsset = await db.CustomerAssets.AsNoTracking().SingleAsync(x => x.Id == asset.Id);
            var persistedPosition = await db.CustomerAssetComponents.AsNoTracking().SingleAsync(x => x.Id == position.Id);

            persistedAsset.Source.ShouldBe(CustomerAssetSource.External);
            persistedAsset.SalesOrderId.ShouldBeNull();
            persistedAsset.ProductId.ShouldBe(product.Id);
            persistedPosition.Status.ShouldBe(CustomerAssetComponentStatus.MissingMapping);
            (await db.AssetMaintenanceEvents.CountAsync(x => x.CustomerAssetId == asset.Id)).ShouldBe(1);
            (await db.CustomerCareSyncFailures.CountAsync(x => x.Id == failure.Id)).ShouldBe(1);
            (await db.ProductMachineSettings.SingleAsync(x => x.ProductId == product.Id)).IsMachine.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task Model_should_define_required_unique_and_concurrency_guards()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();

            Index(db, typeof(ProductMachineSetting), "UX_ProductMachineSettings_ProductId").IsUnique.ShouldBeTrue();
            Index(db, typeof(CustomerAsset), "UX_CustomerAssets_SourceLine_UnitIndex").IsUnique.ShouldBeTrue();
            Index(db, typeof(CustomerAssetComponent), "UX_CustomerAssetComponents_ActivePosition").IsUnique.ShouldBeTrue();
            Index(db, typeof(AssetReplacementReminder), "UX_AssetReplacementReminders_OpenPosition").IsUnique.ShouldBeTrue();
            Index(db, typeof(AssetMaintenanceEvent), "UX_AssetMaintenanceEvents_IdempotencyKey").IsUnique.ShouldBeTrue();
            Index(db, typeof(CustomerCareSyncFailure), "UX_CustomerCareSyncFailures_IdempotencyKey").IsUnique.ShouldBeTrue();

            db.Model.FindEntityType(typeof(CustomerAsset))!
                .FindProperty(nameof(CustomerAsset.RowVersion))!
                .IsConcurrencyToken.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task External_asset_should_preserve_nine_positions_and_schedule_only_confirmed_baselines()
    {
        var groups = GetRequiredService<ICustomerGroupAppService>();
        var customers = GetRequiredService<ICustomerAppService>();
        var components = GetRequiredService<IComponentAppService>();
        var warranty = GetRequiredService<IWarrantyAppService>();
        var group = await groups.CreateAsync(new CreateCustomerGroupDto
        {
            Code = Unique("EXG"),
            Name = "External asset group"
        });
        var customer = await customers.CreateAsync(new CreateCustomerDto
        {
            Code = Unique("EXC"),
            Name = "External asset customer",
            CustomerGroupId = group.Id
        });
        var component = await components.CreateAsync(new CreateComponentDto
        {
            Code = Unique("CORE"),
            Name = "Mapped replacement core",
            Unit = "Piece"
        });
        await warranty.SetPolicyAsync(component.Id, new SetComponentReplacementPolicyDto
        {
            IsEnabled = true,
            CycleMonths = 3,
            WarningDaysBeforeDue = 10
        });

        var baseline = new DateTime(2026, 8, 1);
        var key = Guid.NewGuid().ToString("N");
        var input = new CreateExternalCustomerAssetDto
        {
            CustomerId = customer.Id,
            Model = "External nine-core machine",
            Brand = "Other vendor",
            SerialNo = "EXT-SERIAL-001",
            InstallationAddress = "Customer test address",
            IdempotencyKey = key,
            Positions = Enumerable.Range(1, 9).Select(index => new ExternalAssetPositionInput
            {
                PositionCode = $"CORE-{index:D2}",
                PositionName = $"Core {index}",
                ComponentId = index <= 3 ? component.Id : null,
                Quantity = 1,
                ReplacementBaselineDate = index <= 3 ? baseline : null
            }).ToList()
        };

        var created = await warranty.CreateExternalAssetAsync(input);
        var replay = await warranty.CreateExternalAssetAsync(input);
        created.PositionCount.ShouldBe(9);
        created.CreatedReminderCount.ShouldBe(3);
        replay.AssetId.ShouldBe(created.AssetId);
        replay.PositionCount.ShouldBe(9);
        replay.CreatedReminderCount.ShouldBe(3);

        var detail = await warranty.GetAssetDetailsAsync(created.AssetId);
        detail.Source.ShouldBe(CustomerAssetSource.External);
        detail.Status.ShouldBe(CustomerAssetStatus.Active);
        detail.Positions.Count.ShouldBe(9);
        detail.Positions.Count(position => position.ComponentId.HasValue).ShouldBe(3);
        detail.Positions.Count(position => position.ReplacementBaselineDate.HasValue).ShouldBe(3);
        var untouchedPositions = detail.Positions.Where(position => position.PositionCode is "CORE-08" or "CORE-09")
            .Select(position => (position.Id, position.PositionCode, position.PositionName, position.ComponentId, position.ReplacementBaselineDate))
            .ToList();
        foreach (var position in detail.Positions.Where(position => position.PositionCode is "CORE-01" or "CORE-02" or "CORE-03"))
        {
            position.Note = "Reviewed first three cores";
        }
        var updateInput = new UpdateExternalCustomerAssetDto
        {
            Model = detail.Model!,
            Brand = detail.Brand,
            SerialNo = detail.SerialNo,
            InstallationAddress = detail.InstallationAddress,
            ExternalReference = detail.ExternalReference,
            Note = detail.Note,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Positions = detail.Positions
        };
        var updated = await warranty.UpdateExternalAssetAsync(detail.Id, updateInput);
        var updateReplay = await warranty.UpdateExternalAssetAsync(detail.Id, updateInput);
        updated.CreatedReminderCount.ShouldBe(0);
        updateReplay.CreatedReminderCount.ShouldBe(0);
        var updatedDetail = await warranty.GetAssetDetailsAsync(detail.Id);
        updatedDetail.Positions.Where(position => position.PositionCode is "CORE-08" or "CORE-09")
            .Select(position => (position.Id, position.PositionCode, position.PositionName, position.ComponentId, position.ReplacementBaselineDate))
            .ShouldBe(untouchedPositions);

        var listed = await warranty.GetAssetListAsync(new GetCustomerAssetListInput
        {
            SearchText = "EXT-SERIAL-001",
            Source = CustomerAssetSource.External,
            MaxResultCount = 10
        });
        listed.TotalCount.ShouldBe(1);
        listed.Items.Single().PositionCount.ShouldBe(9);
        listed.Items.Single().NextDueDate.ShouldBe(baseline.AddMonths(3));

        var duplicate = await warranty.CreateExternalAssetAsync(new CreateExternalCustomerAssetDto
        {
            CustomerId = customer.Id,
            Model = "Second physical machine",
            SerialNo = "EXT-SERIAL-001",
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Positions =
            [
                new ExternalAssetPositionInput
                {
                    PositionCode = "CORE-01",
                    PositionName = "Unknown core",
                    Quantity = 1
                }
            ]
        });
        duplicate.DuplicateSerialAssetNos.ShouldContain(created.AssetNo);

        var initialReminders = await warranty.GetReminderListAsync(new GetWarrantyReminderListInput
        {
            SearchText = created.AssetNo,
            Status = AssetReplacementReminderStatus.Pending,
            TimingStatus = WarrantyReminderTimingStatus.NotDue,
            MaxResultCount = 10
        });
        initialReminders.TotalCount.ShouldBe(3);
        await warranty.SetPolicyAsync(component.Id, new SetComponentReplacementPolicyDto
        {
            IsEnabled = true,
            CycleMonths = 12,
            WarningDaysBeforeDue = 45
        });
        var completeTarget = initialReminders.Items[0];
        var rescheduleTarget = initialReminders.Items[1];
        var skipTarget = initialReminders.Items[2];
        var completedAt = new DateTime(2026, 9, 1);
        var completeInput = new CompleteReplacementReminderDto
        {
            CompletedAt = completedAt,
            Note = "Replaced during Warranty transition",
            IdempotencyKey = Guid.NewGuid().ToString("N")
        };
        await warranty.CompleteReminderAsync(completeTarget.Id, completeInput);
        await warranty.CompleteReminderAsync(completeTarget.Id, completeInput);

        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var rescheduleInput = new RescheduleReplacementReminderDto
        {
            DueDate = yesterday,
            Note = "Customer requested an earlier visit",
            IdempotencyKey = Guid.NewGuid().ToString("N")
        };
        await warranty.RescheduleReminderAsync(rescheduleTarget.Id, rescheduleInput);
        await warranty.RescheduleReminderAsync(rescheduleTarget.Id, rescheduleInput);
        var skipInput = new SkipReplacementReminderDto
        {
            Note = "Customer already replaced this core elsewhere",
            IdempotencyKey = Guid.NewGuid().ToString("N")
        };
        await warranty.SkipReminderAsync(skipTarget.Id, skipInput);
        await warranty.SkipReminderAsync(skipTarget.Id, skipInput);

        var overdue = await warranty.GetReminderListAsync(new GetWarrantyReminderListInput
        {
            SearchText = created.AssetNo,
            TimingStatus = WarrantyReminderTimingStatus.Overdue,
            MaxResultCount = 10
        });
        overdue.Items.ShouldHaveSingleItem().Id.ShouldBe(rescheduleTarget.Id);

        var history = await warranty.GetAssetHistoryAsync(new GetAssetMaintenanceHistoryInput
        {
            CustomerAssetId = created.AssetId,
            MaxResultCount = 20
        });
        history.Items.ShouldAllBe(item => item.CustomerAssetId == created.AssetId);
        history.Items.Count(item => item.EventType == AssetMaintenanceEventType.Replacement).ShouldBe(1);
        history.Items.Count(item => item.Note == rescheduleInput.Note).ShouldBe(1);
        history.Items.Count(item => item.Note == skipInput.Note).ShouldBe(1);
        var duplicateHistory = await warranty.GetAssetHistoryAsync(new GetAssetMaintenanceHistoryInput
        {
            CustomerAssetId = duplicate.AssetId,
            MaxResultCount = 20
        });
        duplicateHistory.Items.ShouldAllBe(item => item.CustomerAssetId == duplicate.AssetId);
        duplicateHistory.Items.ShouldNotContain(item => item.Note == completeInput.Note);

        var suspendInput = new SuspendCustomerAssetDto
        {
            Reason = "Customer temporarily stopped maintenance",
            IdempotencyKey = Guid.NewGuid().ToString("N")
        };
        await warranty.SuspendAssetAsync(created.AssetId, suspendInput);
        await warranty.SuspendAssetAsync(created.AssetId, suspendInput);

        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            var positions = await db.CustomerAssetComponents.AsNoTracking()
                .Where(position => position.CustomerAssetId == created.AssetId)
                .ToListAsync();
            positions.Count(position => position.Status == CustomerAssetComponentStatus.MissingMapping).ShouldBe(6);
            positions.Where(position => position.Status == CustomerAssetComponentStatus.MissingMapping)
                .ShouldAllBe(position => position.ReplacementBaselineDate == null);
            (await db.AssetReplacementReminders.CountAsync(reminder => reminder.CustomerAssetId == created.AssetId))
                .ShouldBe(4);
            var completed = await db.AssetReplacementReminders.AsNoTracking().SingleAsync(reminder => reminder.Id == completeTarget.Id);
            var next = await db.AssetReplacementReminders.AsNoTracking().SingleAsync(reminder => reminder.Id == completed.NextReminderId);
            next.CycleMonthsSnapshot.ShouldBe(3);
            next.WarningDaysBeforeDueSnapshot.ShouldBe(10);
            next.DueDate.ShouldBe(completedAt.AddMonths(3));
            (await db.CustomerAssets.AsNoTracking().SingleAsync(asset => asset.Id == created.AssetId)).Status
                .ShouldBe(CustomerAssetStatus.Inactive);
            (await db.AssetReplacementReminders.CountAsync(reminder =>
                reminder.CustomerAssetId == created.AssetId && reminder.Status == AssetReplacementReminderStatus.Pending))
                .ShouldBe(0);
            (await db.AssetMaintenanceEvents.CountAsync(maintenanceEvent =>
                maintenanceEvent.CustomerAssetId == created.AssetId &&
                maintenanceEvent.EventType == AssetMaintenanceEventType.Deactivated)).ShouldBe(1);
        });
    }

    private static Microsoft.EntityFrameworkCore.Metadata.IReadOnlyIndex Index(
        VPureLuxDbContext db,
        Type entityType,
        string databaseName) =>
        db.Model.FindEntityType(entityType)!.GetIndexes()
            .Single(index => index.GetDatabaseName() == databaseName);

    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];
}
