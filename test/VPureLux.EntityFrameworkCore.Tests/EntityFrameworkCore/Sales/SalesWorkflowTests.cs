using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shouldly;
using VPureLux.Bom;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using VPureLux.Customers;
using VPureLux.Customers.CustomerGroups;
using VPureLux.CustomerCare;
using VPureLux.Inventory;
using VPureLux.Pricing;
using VPureLux.Sales;
using VPureLux.Warranty;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Sales;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class SalesWorkflowTests : VPureLuxEntityFrameworkCoreTestBase
{
    private readonly ISalesOrderAppService _sales;
    private readonly ISalesPostConfirmationAppService _postConfirmation;
    private readonly ICustomerAppService _customers;
    private readonly ICustomerGroupAppService _groups;
    private readonly IWarehouseAppService _warehouses;
    private readonly IComponentAppService _components;
    private readonly IProductAppService _products;
    private readonly IStockItemRepository _stockItems;
    private readonly IInventoryTransactionAppService _inventory;
    private readonly IInventoryQueryAppService _inventoryQuery;
    private readonly IBomAppService _boms;
    private readonly IComponentSuggestedSellingPriceAppService _componentPrices;
    private readonly IProductSuggestedPriceAppService _prices;
    private readonly ISalesOrderPaymentRepository _payments;
    private readonly IWarrantyAppService _warranty;

    public SalesWorkflowTests()
    {
        _sales = GetRequiredService<ISalesOrderAppService>();
        _postConfirmation = GetRequiredService<ISalesPostConfirmationAppService>();
        _customers = GetRequiredService<ICustomerAppService>();
        _groups = GetRequiredService<ICustomerGroupAppService>();
        _warehouses = GetRequiredService<IWarehouseAppService>();
        _components = GetRequiredService<IComponentAppService>();
        _products = GetRequiredService<IProductAppService>();
        _stockItems = GetRequiredService<IStockItemRepository>();
        _inventory = GetRequiredService<IInventoryTransactionAppService>();
        _inventoryQuery = GetRequiredService<IInventoryQueryAppService>();
        _boms = GetRequiredService<IBomAppService>();
        _componentPrices = GetRequiredService<IComponentSuggestedSellingPriceAppService>();
        _prices = GetRequiredService<IProductSuggestedPriceAppService>();
        _payments = GetRequiredService<ISalesOrderPaymentRepository>();
        _warranty = GetRequiredService<IWarrantyAppService>();
    }

    [Fact]
    public async Task V2_End_To_End_Product_Sale_Should_Expand_Bom_Consume_Fifo_And_Calculate_Snapshots()
    {
        var context = await CreateBaseAsync();
        var pp = await _components.CreateAsync(new CreateComponentDto { Code = Unique("PP"), Name = "PP 1 micron", Unit = "Piece" });
        var cto = await _components.CreateAsync(new CreateComponentDto { Code = Unique("CTO"), Name = "CTO", Unit = "Piece" });
        var ppStockItem = await GetComponentStockItemAsync(pp.Id);
        var ctoStockItem = await GetComponentStockItemAsync(cto.Id);
        await PostReceiptAsync(context.Warehouse.Id, ppStockItem.Id, 5, 10_000, Unique("PPA"));
        await PostReceiptAsync(context.Warehouse.Id, ppStockItem.Id, 5, 12_000, Unique("PPB"));
        await PostReceiptAsync(context.Warehouse.Id, ctoStockItem.Id, 10, 20_000, Unique("CTO"));
        var product = await _products.CreateAsync(new CreateProductDto { Code = Unique("RH8"), Name = "Máy lọc nước RH8" });
        var bom = await _boms.CreateAsync(product.Id, new CreateBomVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Items =
            [
                new CreateBomItemDto { ComponentId = pp.Id, Quantity = 2 },
                new CreateBomItemDto { ComponentId = cto.Id, Quantity = 1 }
            ]
        });
        await _boms.PublishAsync(bom.Id);
        await _componentPrices.CreateAsync(pp.Id, new CreateComponentSuggestedSellingPriceVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Price = 30_000,
            Reason = "UAT component suggested price"
        });
        await _componentPrices.CreateAsync(cto.Id, new CreateComponentSuggestedSellingPriceVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Price = 40_000,
            Reason = "UAT component suggested price"
        });
        var productPrice = await _prices.CreateAsync(product.Id, new CreateProductSuggestedPriceVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Price = 100_000,
            Reason = "UAT product suggested price"
        });

        var order = await _sales.CreateAsync(Input(context, product.Id, 3, null));
        order.Lines.Single().ProductId.ShouldBe(product.Id);
        order.Lines.Single().SuggestedPriceVersionId.ShouldBe(productPrice.Id);
        order.Lines.Single().SuggestedPriceSnapshot.ShouldBe(100_000);

        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });

        var confirmed = await _sales.GetAsync(order.Id);
        var line = confirmed.Lines.Single();
        line.ProductId.ShouldBe(product.Id);
        line.BomVersionId.ShouldBe(bom.Id);
        line.BomVersionNoSnapshot.ShouldBe(bom.VersionNo);
        line.BomSnapshotItems.Count.ShouldBe(2);
        line.BomSnapshotItems.Single(x => x.ComponentId == pp.Id).TotalRequiredQuantity.ShouldBe(6);
        line.BomSnapshotItems.Single(x => x.ComponentId == cto.Id).TotalRequiredQuantity.ShouldBe(3);
        line.CostAmountSnapshot.ShouldBe(122_000);
        line.CostPriceSnapshot.ShouldBe(decimal.Round(122_000m / 3m, SalesConsts.MoneyScale, MidpointRounding.AwayFromZero));
        line.RevenueAmount.ShouldBe(300_000);
        line.ProfitAmount.ShouldBe(178_000);
        line.MarginPercent.ShouldBe(decimal.Round(178_000m / 300_000m * 100m, SalesConsts.MarginScale, MidpointRounding.AwayFromZero));

        var ppBalance = (await _inventoryQuery.GetBalancesAsync(context.Warehouse.Id, ppStockItem.Id)).Single();
        var ctoBalance = (await _inventoryQuery.GetBalancesAsync(context.Warehouse.Id, ctoStockItem.Id)).Single();
        ppBalance.QuantityOnHand.ShouldBe(4);
        ppBalance.InventoryValue.ShouldBe(48_000);
        ctoBalance.QuantityOnHand.ShouldBe(7);
        ctoBalance.InventoryValue.ShouldBe(140_000);

        var ledger = await _inventoryQuery.GetLedgerAsync(context.Warehouse.Id);
        var salesIssue = ledger.Single(x => x.ReferenceType == "SalesOrderLine" && x.ReferenceId == line.Id);
        salesIssue.BomVersionId.ShouldBe(bom.Id);
        salesIssue.TotalIssueCost.ShouldBe(122_000);
        var ppIssueLine = salesIssue.Lines.Single(x => x.StockItemId == ppStockItem.Id);
        var ppFifoLots = await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            return await db.InventoryLots
                .AsNoTracking()
                .Where(x => ppIssueLine.Allocations.Select(a => a.InventoryLotId).Contains(x.Id))
                .OrderBy(x => x.ReceivedAt)
                .ThenBy(x => x.CreationTime)
                .ThenBy(x => x.Id)
                .ToListAsync();
        });
        ppFifoLots.Select(x => ppIssueLine.Allocations.Single(a => a.InventoryLotId == x.Id).Quantity).ToArray()
            .ShouldBe([5, 1]);
        salesIssue.Lines.Single(x => x.StockItemId == ppStockItem.Id).Allocations.Sum(x => x.TotalCost).ShouldBe(62_000);
        salesIssue.Lines.Single(x => x.StockItemId == ctoStockItem.Id).Allocations.Sum(x => x.TotalCost).ShouldBe(60_000);

        var persisted = await GetRequiredService<ISalesOrderRepository>().GetAsync(order.Id, includeDetails: true);
        var persistedLine = persisted.Lines.Single();
        persistedLine.LineType.ShouldBe(SalesOrderLineType.Product);
        persistedLine.ProductId.ShouldBe(product.Id);
        persistedLine.ProductId.ShouldNotBe(pp.Id);
        persistedLine.ProductId.ShouldNotBe(cto.Id);
    }

    [Fact]
    public async Task V2_Loose_Component_Sale_Should_Use_Product_With_One_Component_Bom()
    {
        var context = await CreateBaseAsync();
        var pp = await _components.CreateAsync(new CreateComponentDto { Code = Unique("PP"), Name = "PP bán rời", Unit = "Piece" });
        var stockItem = await GetComponentStockItemAsync(pp.Id);
        await PostReceiptAsync(context.Warehouse.Id, stockItem.Id, 10, 25_000, Unique("PPL"));
        await _componentPrices.CreateAsync(pp.Id, new CreateComponentSuggestedSellingPriceVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Price = 35_000,
            Reason = "Loose component suggested price"
        });
        var looseSku = await _products.CreateAsync(new CreateProductDto
        {
            Code = Unique("LPP"),
            Name = "Lõi PP bán rời"
        });
        var bom = await _boms.CreateAsync(looseSku.Id, new CreateBomVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Items = [new CreateBomItemDto { ComponentId = pp.Id, Quantity = 1 }]
        });
        await _boms.PublishAsync(bom.Id);
        await _prices.CreateAsync(looseSku.Id, new CreateProductSuggestedPriceVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Price = 50_000,
            Reason = "Loose SKU suggested price"
        });

        var order = await _sales.CreateAsync(Input(context, looseSku.Id, 2, null));
        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });

        var confirmed = await _sales.GetAsync(order.Id);
        var line = confirmed.Lines.Single();
        line.ProductId.ShouldBe(looseSku.Id);
        line.BomVersionId.ShouldBe(bom.Id);
        line.BomSnapshotItems.ShouldHaveSingleItem();
        line.BomSnapshotItems.Single().ComponentId.ShouldBe(pp.Id);
        line.BomSnapshotItems.Single().QuantityPerProduct.ShouldBe(1);
        line.BomSnapshotItems.Single().TotalRequiredQuantity.ShouldBe(2);
        line.CostAmountSnapshot.ShouldBe(50_000);
        line.RevenueAmount.ShouldBe(100_000);
        line.ProfitAmount.ShouldBe(50_000);

        var balance = (await _inventoryQuery.GetBalancesAsync(context.Warehouse.Id, stockItem.Id)).Single();
        balance.QuantityOnHand.ShouldBe(8);
        balance.InventoryValue.ShouldBe(200_000);

        var persisted = await GetRequiredService<ISalesOrderRepository>().GetAsync(order.Id, includeDetails: true);
        var persistedLine = persisted.Lines.Single();
        persistedLine.LineType.ShouldBe(SalesOrderLineType.Product);
        persistedLine.ProductId.ShouldBe(looseSku.Id);
        persistedLine.ProductId.ShouldNotBe(pp.Id);
    }

    [Fact]
    public async Task Confirm_Should_Not_Run_CustomerCare_Synchronously()
    {
        var options = GetRequiredService<IOptions<CustomerCareOptions>>().Value;
        options.IsEnabled.ShouldBeFalse();
        options.IsSalesIntakeEnabled.ShouldBeFalse();
        options.SalesIntakeGoLiveFrom.ShouldBeNull();

        var context = await CreateBaseAsync();
        var component = await CreateComponentWithStockAsync(context.Warehouse.Id, 10, 25_000);
        var (product, _) = await CreateProductForComponentAsync(component);
        await _warranty.SetPolicyAsync(component.Id, new SetComponentReplacementPolicyDto
        {
            IsEnabled = true,
            CycleMonths = 3,
            WarningDaysBeforeDue = 10
        });

        var order = await _sales.CreateAsync(Input(context, product.Id, 2, 100_000));
        var key = Guid.NewGuid().ToString("N");
        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = key });

        var confirmed = await _sales.GetAsync(order.Id);
        confirmed.Status.ShouldBe(SalesOrderStatus.Confirmed);
        confirmed.TotalRevenueAmount.ShouldBe(200_000);

        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = key });

        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            (await db.CustomerAssets.CountAsync(x => x.SalesOrderId == order.Id)).ShouldBe(0);
            (await db.AssetReplacementReminders.CountAsync(x => x.SalesOrderId == order.Id)).ShouldBe(0);
        });
    }

    [Fact]
    public void Sales_AppService_Should_Not_Depend_On_Warranty_Or_CustomerCare()
    {
        var constructor = typeof(SalesOrderAppService).GetConstructors().ShouldHaveSingleItem();

        constructor.GetParameters().Any(parameter =>
            (parameter.ParameterType.Namespace ?? string.Empty).StartsWith("VPureLux.Warranty", StringComparison.Ordinal) ||
            (parameter.ParameterType.Namespace ?? string.Empty).StartsWith("VPureLux.CustomerCare", StringComparison.Ordinal))
            .ShouldBeFalse();
    }

    [Fact]
    public async Task CustomerCare_intake_should_create_pending_machine_units_idempotently_after_sales_confirmation()
    {
        var context = await CreateBaseAsync();
        var component = await CreateComponentWithStockAsync(context.Warehouse.Id, 20, 10_000);
        var (machine, _) = await CreateProductForComponentAsync(component);
        var (nonMachine, _) = await CreateProductForComponentAsync(component);
        await _warranty.SetMachineSettingAsync(machine.Id, new SetProductMachineSettingDto
        {
            IsMachine = true,
            Note = "Machine intake test"
        });
        var order = await _sales.CreateAsync(Input(context, machine.Id, 2, 100_000));
        var nonMachineOrder = await _sales.CreateAsync(Input(context, nonMachine.Id, 1, 50_000));
        var goLive = DateTimeOffset.UtcNow.AddMinutes(-5);
        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto
        {
            IdempotencyKey = Guid.NewGuid().ToString("N")
        });
        await _sales.ConfirmAsync(nonMachineOrder.Id, new ConfirmSalesOrderDto
        {
            IdempotencyKey = Guid.NewGuid().ToString("N")
        });

        var options = GetRequiredService<IOptions<CustomerCareOptions>>().Value;
        options.IsEnabled = true;
        options.IsSalesIntakeEnabled = true;
        options.SalesIntakeGoLiveFrom = goLive;
        try
        {
            var intake = GetRequiredService<CustomerCareSalesIntakeService>();
            var first = await intake.RunBatchAsync(DateTimeOffset.UtcNow);
            var replay = await intake.RunBatchAsync(DateTimeOffset.UtcNow);

            first.CreatedAssetCount.ShouldBe(2);
            first.CandidateCount.ShouldBe(1);
            first.FailedLineCount.ShouldBe(0);
            replay.CreatedAssetCount.ShouldBe(0);

            await WithUnitOfWorkAsync(async () =>
            {
                var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
                var assets = await db.CustomerAssets.AsNoTracking()
                    .Where(x => x.SalesOrderId == order.Id)
                    .OrderBy(x => x.SourceUnitIndex)
                    .ToListAsync();
                assets.Count.ShouldBe(2);
                assets.Select(x => x.SourceUnitIndex).ShouldBe(new int?[] { 1, 2 });
                assets.ShouldAllBe(x => x.Status == CustomerAssetStatus.PendingInstallation);
                assets.ShouldAllBe(x => x.InstalledAt == null);
                (await db.CustomerAssetComponents.CountAsync(x => assets.Select(a => a.Id).Contains(x.CustomerAssetId)))
                    .ShouldBe(2);
                (await db.CustomerAssets.CountAsync(x => x.SalesOrderId == nonMachineOrder.Id)).ShouldBe(0);
            });
        }
        finally
        {
            DisableCustomerCareIntake(options);
        }
    }

    [Fact]
    public async Task Installation_should_create_first_schedules_once_from_actual_positions_and_policy_snapshots()
    {
        var context = await CreateBaseAsync();
        var trackedComponent = await CreateComponentWithStockAsync(context.Warehouse.Id, 10, 10_000);
        var disabledComponent = await CreateComponentWithStockAsync(context.Warehouse.Id, 10, 20_000);
        var machine = await _products.CreateAsync(new CreateProductDto
        {
            Code = Unique("MCH"),
            Name = "Installation schedule machine"
        });
        var soldBom = await _boms.CreateAsync(machine.Id, new CreateBomVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Items =
            [
                new CreateBomItemDto { ComponentId = trackedComponent.Id, Quantity = 2 },
                new CreateBomItemDto { ComponentId = disabledComponent.Id, Quantity = 1 }
            ]
        });
        await _boms.PublishAsync(soldBom.Id);
        await _warranty.SetMachineSettingAsync(machine.Id, new SetProductMachineSettingDto { IsMachine = true });
        await _warranty.SetPolicyAsync(trackedComponent.Id, new SetComponentReplacementPolicyDto
        {
            IsEnabled = true,
            CycleMonths = 3,
            WarningDaysBeforeDue = 14
        });
        await _warranty.SetPolicyAsync(disabledComponent.Id, new SetComponentReplacementPolicyDto
        {
            IsEnabled = false,
            CycleMonths = 6,
            WarningDaysBeforeDue = 30
        });

        var order = await _sales.CreateAsync(Input(context, machine.Id, 1, 200_000));
        var goLive = DateTimeOffset.UtcNow.AddMinutes(-5);
        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto
        {
            IdempotencyKey = Guid.NewGuid().ToString("N")
        });
        await _boms.ArchiveAsync(soldBom.Id);
        var currentBom = await _boms.CreateAsync(machine.Id, new CreateBomVersionDto
        {
            EffectiveFrom = DateTime.Now.Date.AddDays(1),
            Items = [new CreateBomItemDto { ComponentId = trackedComponent.Id, Quantity = 5 }]
        });
        await _boms.PublishAsync(currentBom.Id);

        var options = GetRequiredService<IOptions<CustomerCareOptions>>().Value;
        options.IsEnabled = true;
        options.IsSalesIntakeEnabled = true;
        options.SalesIntakeGoLiveFrom = goLive;
        try
        {
            (await GetRequiredService<CustomerCareSalesIntakeService>()
                .RunBatchAsync(DateTimeOffset.UtcNow)).CreatedAssetCount.ShouldBe(1);

            var pending = await _warranty.GetPendingInstallationListAsync(new GetPendingInstallationListInput
            {
                SearchText = order.OrderNo,
                MaxResultCount = 10
            });
            pending.TotalCount.ShouldBe(1);
            pending.Items.Single().PositionCount.ShouldBe(2);

            var editor = await _warranty.GetInstallationEditorAsync(pending.Items.Single().Id);
            editor.Positions.Count.ShouldBe(2);
            editor.Positions.Single(position => position.ComponentId == trackedComponent.Id).Quantity.ShouldBe(2);
            await WithUnitOfWorkAsync(async () =>
            {
                var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
                (await db.AssetReplacementReminders.CountAsync(x => x.CustomerAssetId == editor.Id)).ShouldBe(0);
            });

            var installedAt = new DateTime(2026, 8, 24, 9, 30, 0, DateTimeKind.Utc);
            var idempotencyKey = Guid.NewGuid().ToString("N");
            var input = new ConfirmAssetInstallationDto
            {
                InstalledAt = installedAt,
                SerialNo = "RH8-INSTALL-001",
                InstallationAddress = "Test installation address",
                IdempotencyKey = idempotencyKey,
                Positions = editor.Positions
            };
            input.Positions.Single(position => position.ComponentId == disabledComponent.Id).IsIncluded = false;
            input.Positions.Add(new AssetInstallationPositionDto
            {
                PositionCode = "EXTRA-01",
                PositionName = "Vị trí thực tế chưa map",
                Quantity = 1,
                IsIncluded = true
            });

            await _warranty.SetMachineSettingAsync(machine.Id, new SetProductMachineSettingDto { IsMachine = false });
            await Should.ThrowAsync<BusinessException>(() => _warranty.ConfirmInstallationAsync(editor.Id, input));
            await _warranty.SetMachineSettingAsync(machine.Id, new SetProductMachineSettingDto { IsMachine = true });

            var first = await _warranty.ConfirmInstallationAsync(editor.Id, input);
            var replay = await _warranty.ConfirmInstallationAsync(editor.Id, input);

            first.IsReplay.ShouldBeFalse();
            first.ActivePositionCount.ShouldBe(2);
            first.CreatedReminderCount.ShouldBe(1);
            replay.IsReplay.ShouldBeTrue();
            replay.CreatedReminderCount.ShouldBe(1);

            await WithUnitOfWorkAsync(async () =>
            {
                var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
                var asset = await db.CustomerAssets.AsNoTracking().SingleAsync(x => x.Id == editor.Id);
                asset.Status.ShouldBe(CustomerAssetStatus.Active);
                asset.InstalledAt.ShouldBe(installedAt);
                asset.SerialNo.ShouldBe("RH8-INSTALL-001");

                var reminders = await db.AssetReplacementReminders.AsNoTracking()
                    .Where(x => x.CustomerAssetId == editor.Id)
                    .ToListAsync();
                reminders.ShouldHaveSingleItem();
                reminders.Single().ComponentId.ShouldBe(trackedComponent.Id);
                reminders.Single().QuantityPerProductSnapshot.ShouldBe(2);
                reminders.Single().DueDate.ShouldBe(installedAt.Date.AddMonths(3));
                reminders.Single().WarningDate.ShouldBe(installedAt.Date.AddMonths(3).AddDays(-14));
                reminders.Single().TriggerSource.ShouldBe(ReplacementReminderTriggerSource.Installation);
                var positions = await db.CustomerAssetComponents.AsNoTracking()
                    .Where(x => x.CustomerAssetId == editor.Id)
                    .ToListAsync();
                positions.Count.ShouldBe(3);
                positions.Single(x => x.ComponentId == disabledComponent.Id).Status
                    .ShouldBe(CustomerAssetComponentStatus.Inactive);
                positions.Single(x => x.PositionCode == "EXTRA-01").Status
                    .ShouldBe(CustomerAssetComponentStatus.MissingMapping);
                (await db.AssetMaintenanceEvents.CountAsync(x =>
                    x.CustomerAssetId == editor.Id && x.EventType == AssetMaintenanceEventType.Installation)).ShouldBe(1);
            });
        }
        finally
        {
            DisableCustomerCareIntake(options);
        }
    }

    [Fact]
    public async Task CustomerCare_intake_should_isolate_invalid_machine_quantity_and_continue_batch()
    {
        var context = await CreateBaseAsync();
        var badComponent = await CreateComponentWithStockAsync(context.Warehouse.Id, 10, 10_000);
        var goodComponent = await CreateComponentWithStockAsync(context.Warehouse.Id, 10, 10_000);
        var (badMachine, _) = await CreateProductForComponentAsync(badComponent);
        var (goodMachine, _) = await CreateProductForComponentAsync(goodComponent);
        await _warranty.SetMachineSettingAsync(badMachine.Id, new SetProductMachineSettingDto { IsMachine = true });
        await _warranty.SetMachineSettingAsync(goodMachine.Id, new SetProductMachineSettingDto { IsMachine = true });

        var goLive = DateTimeOffset.UtcNow.AddMinutes(-5);
        var badOrder = await _sales.CreateAsync(Input(context, badMachine.Id, 1.5m, 100_000));
        var goodOrder = await _sales.CreateAsync(Input(context, goodMachine.Id, 1, 100_000));
        await _sales.ConfirmAsync(badOrder.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });
        await _sales.ConfirmAsync(goodOrder.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });

        var options = GetRequiredService<IOptions<CustomerCareOptions>>().Value;
        options.IsEnabled = true;
        options.IsSalesIntakeEnabled = true;
        options.SalesIntakeGoLiveFrom = goLive;
        try
        {
            var result = await GetRequiredService<CustomerCareSalesIntakeService>()
                .RunBatchAsync(DateTimeOffset.UtcNow);

            result.CreatedAssetCount.ShouldBe(1);
            result.FailedLineCount.ShouldBe(1);
            await WithUnitOfWorkAsync(async () =>
            {
                var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
                (await db.CustomerAssets.CountAsync(x => x.SalesOrderId == badOrder.Id)).ShouldBe(0);
                (await db.CustomerAssets.CountAsync(x => x.SalesOrderId == goodOrder.Id)).ShouldBe(1);
                var failure = await db.CustomerCareSyncFailures.AsNoTracking()
                    .SingleAsync(x => x.SalesOrderId == badOrder.Id);
                failure.Status.ShouldBe(CustomerCareSyncFailureStatus.Pending);
                failure.AttemptCount.ShouldBe(1);
                failure.ErrorContext!.ShouldContain(badMachine.Code);
            });

            var listed = await _warranty.GetSyncFailureListAsync(new GetCustomerCareSyncFailureListInput
            {
                SearchText = badMachine.Code,
                Status = CustomerCareSyncFailureStatus.Pending,
                MaxResultCount = 10
            });
            listed.TotalCount.ShouldBe(1);
            listed.Items.Single().OrderNo.ShouldBe(badOrder.OrderNo);
            await _warranty.RetrySyncFailureAsync(listed.Items.Single().Id);
        }
        finally
        {
            DisableCustomerCareIntake(options);
        }
    }

    [Fact]
    public async Task Draft_Line_Update_Should_Allow_Changing_Product_And_Refresh_Bom_And_Price_Snapshot()
    {
        var context = await CreateBaseAsync();
        var firstComponent = await CreateComponentWithStockAsync(context.Warehouse.Id, 10, 20_000);
        var secondComponent = await CreateComponentWithStockAsync(context.Warehouse.Id, 10, 30_000);
        var (firstProduct, firstBom) = await CreateProductForComponentAsync(firstComponent);
        var (secondProduct, secondBom) = await CreateProductForComponentAsync(secondComponent);
        await _prices.CreateAsync(firstProduct.Id, new CreateProductSuggestedPriceVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Price = 100_000,
            Reason = "First product list price"
        });
        var secondPrice = await _prices.CreateAsync(secondProduct.Id, new CreateProductSuggestedPriceVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Price = 150_000,
            Reason = "Second product list price"
        });
        var order = await _sales.CreateAsync(Input(context, firstProduct.Id, 1, null));
        var line = order.Lines.Single();

        await _sales.UpdateLineAsync(order.Id, line.Id, new UpdateSalesOrderLineDto
        {
            ProductId = secondProduct.Id,
            Quantity = 2,
            ActualSellingPrice = 150_000
        });

        var updated = await _sales.GetAsync(order.Id);
        var updatedLine = updated.Lines.Single();
        updatedLine.Id.ShouldBe(line.Id);
        updatedLine.ProductId.ShouldBe(secondProduct.Id);
        updatedLine.BomVersionId.ShouldBe(secondBom.Id);
        updatedLine.BomVersionId.ShouldNotBe(firstBom.Id);
        updatedLine.SuggestedPriceVersionId.ShouldBe(secondPrice.Id);
        updatedLine.SuggestedPriceSnapshot.ShouldBe(150_000);
        updatedLine.Quantity.ShouldBe(2);
        updatedLine.ActualSellingPrice.ShouldBe(150_000);
    }

    [Fact]
    public async Task Should_Confirm_Product_Line_Idempotently_And_Calculate_Profit_History()
    {
        var context = await CreateBaseAsync();
        var component = await CreateComponentWithStockAsync(context.Warehouse.Id, 10, 650_000);
        var (product, _) = await CreateProductForComponentAsync(component);
        var order = await _sales.CreateAsync(Input(context, product.Id, 1, 1_000_000));
        var key = Guid.NewGuid().ToString("N");

        var first = await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = key });
        var replay = await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = key });
        var confirmed = await _sales.GetAsync(order.Id);

        replay.SalesOrderId.ShouldBe(first.SalesOrderId);
        confirmed.Status.ShouldBe(SalesOrderStatus.Confirmed);
        confirmed.TotalRevenueAmount.ShouldBe(1_000_000);
        confirmed.TotalCostAmount.ShouldBe(650_000);
        confirmed.TotalProfitAmount.ShouldBe(350_000);
        confirmed.Lines.Single().MarginPercent.ShouldBe(35);
        confirmed.CustomerCodeSnapshot.ShouldBe(context.Customer.Code);
        confirmed.Lines.Single().ItemCodeSnapshot.ShouldBe(product.Code);
        confirmed.Lines.Single().BomSnapshotItems.Single().ComponentCode.ShouldBe(component.Code);

        var history = (await _sales.GetCustomerHistoryAsync(context.Customer.Id)).Single();
        history.LastPurchasePrice.ShouldBe(1_000_000);
        history.AveragePurchasePrice.ShouldBe(1_000_000);
        history.Revenue.ShouldBe(1_000_000);
        history.Profit.ShouldBe(350_000);

        (await Should.ThrowAsync<BusinessException>(() =>
            _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = "different" })))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.DuplicateConfirmationKey);
    }

    [Fact]
    public async Task Should_Confirm_Product_Line_Using_Published_Bom_And_Pricing_Default()
    {
        var context = await CreateBaseAsync();
        var component = await CreateComponentWithStockAsync(context.Warehouse.Id, 20, 30_000);
        var product = await _products.CreateAsync(new CreateProductDto { Code = Unique("SP"), Name = "Sales Product" });
        var bom = await _boms.CreateAsync(product.Id, new CreateBomVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Items = [new CreateBomItemDto { ComponentId = component.Id, Quantity = 2 }]
        });
        await _boms.PublishAsync(bom.Id);
        await _componentPrices.CreateAsync(component.Id, new CreateComponentSuggestedSellingPriceVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Price = 40_000,
            Reason = "Reference component price"
        });
        var price = await _prices.CreateAsync(product.Id, new CreateProductSuggestedPriceVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Price = 100_000,
            Reason = "Product list price"
        });

        var order = await _sales.CreateAsync(Input(context, product.Id, 3, null));
        order.Lines.Single().SuggestedPriceSnapshot.ShouldBe(100_000);
        order.Lines.Single().SuggestedPriceVersionId.ShouldBe(price.Id);

        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });
        var confirmed = await _sales.GetAsync(order.Id);
        var line = confirmed.Lines.Single();
        line.BomVersionId.ShouldBe(bom.Id);
        line.BomVersionNoSnapshot.ShouldBe(bom.VersionNo);
        line.BomSnapshotItems.Single().TotalRequiredQuantity.ShouldBe(6);
        line.CostAmountSnapshot.ShouldBe(180_000);
        line.RevenueAmount.ShouldBe(300_000);
        line.ProfitAmount.ShouldBe(120_000);
    }

    [Fact]
    public async Task Payment_Read_Model_Should_Derive_Receivable_Status_And_Preserve_Sales_Posting()
    {
        var context = await CreateBaseAsync();
        var component = await CreateComponentWithStockAsync(context.Warehouse.Id, 10, 500);
        var (product, _) = await CreateProductForComponentAsync(component);
        var order = await _sales.CreateAsync(Input(context, product.Id, 2, 1_000));
        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });
        var confirmed = await _sales.GetAsync(order.Id);
        var line = confirmed.Lines.Single();
        var transactionCountBeforePayment = await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            return await db.InventoryTransactions.CountAsync();
        });

        var unpaid = await _sales.GetPaymentSummaryAsync(order.Id);
        unpaid.TotalAmount.ShouldBe(2_000);
        unpaid.PaidAmount.ShouldBe(0);
        unpaid.RemainingAmount.ShouldBe(2_000);
        unpaid.PaymentStatus.ShouldBe(SalesOrderReceivableStatus.Unpaid);

        await InsertPaymentAsync(order.Id, context.Customer.Id, 500, "PAY-PARTIAL");
        var partial = await _sales.GetAsync(order.Id);
        partial.PaymentSummary.PaidAmount.ShouldBe(500);
        partial.PaymentSummary.RemainingAmount.ShouldBe(1_500);
        partial.PaymentSummary.PaymentStatus.ShouldBe(SalesOrderReceivableStatus.PartiallyPaid);

        await InsertPaymentAsync(order.Id, context.Customer.Id, 1_500, "PAY-PAID");
        var paid = await _sales.GetAsync(order.Id);
        paid.PaymentSummary.PaidAmount.ShouldBe(2_000);
        paid.PaymentSummary.RemainingAmount.ShouldBe(0);
        paid.PaymentSummary.PaymentStatus.ShouldBe(SalesOrderReceivableStatus.Paid);

        await InsertPaymentAsync(order.Id, context.Customer.Id, 100, "PAY-OVER");
        var overpaid = await _sales.GetPaymentSummaryAsync(order.Id);
        overpaid.PaidAmount.ShouldBe(2_100);
        overpaid.RemainingAmount.ShouldBe(-100);
        overpaid.PaymentStatus.ShouldBe(SalesOrderReceivableStatus.Overpaid);

        var payments = await _sales.GetPaymentsAsync(order.Id);
        payments.Count.ShouldBe(3);
        payments.ShouldAllBe(x => x.Status == SalesOrderPaymentStatus.Posted);
        payments.Select(x => x.ReferenceNo).ShouldContain("PAY-PARTIAL");

        var listed = await _sales.GetListAsync(new GetSalesOrderListInput { CustomerId = context.Customer.Id });
        listed.Items.Single(x => x.Id == order.Id).PaymentSummary.PaymentStatus.ShouldBe(SalesOrderReceivableStatus.Overpaid);

        var reloaded = await _sales.GetAsync(order.Id);
        reloaded.TotalRevenueAmount.ShouldBe(confirmed.TotalRevenueAmount);
        reloaded.TotalCostAmount.ShouldBe(confirmed.TotalCostAmount);
        reloaded.TotalProfitAmount.ShouldBe(confirmed.TotalProfitAmount);
        reloaded.Lines.Single().InventoryTransactionId.ShouldBe(line.InventoryTransactionId);
        var transactionCountAfterPayment = await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            return await db.InventoryTransactions.CountAsync();
        });
        transactionCountAfterPayment.ShouldBe(transactionCountBeforePayment);
    }

    [Fact]
    public async Task Confirmed_Unpaid_Cancel_Should_Rollback_Inventory_To_Original_Lots()
    {
        var context = await CreateBaseAsync();
        var component = await CreateComponentWithStockAsync(context.Warehouse.Id, 10, 500);
        var stockItem = await GetComponentStockItemAsync(component.Id);
        var (product, _) = await CreateProductForComponentAsync(component);
        var order = await _sales.CreateAsync(Input(context, product.Id, 2, 1_000));
        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });
        var confirmed = await _sales.GetAsync(order.Id);
        var line = confirmed.Lines.Single();
        (await _inventoryQuery.GetBalancesAsync(context.Warehouse.Id, stockItem.Id)).Single().QuantityOnHand.ShouldBe(8);
        var issuedLot = await GetSingleLotAsync(stockItem.Id);
        issuedLot.AvailableQuantity.ShouldBe(8);

        var cancellation = await _postConfirmation.CancelConfirmedAsync(order.Id, new CancelConfirmedSalesOrderDto
        {
            ReasonGroup = "CustomerChangedMind",
            Reason = "Customer cancelled before installation"
        });

        cancellation.StockStatus.ShouldBe(SalesOrderCancellationStockStatus.PendingReturn);
        (await _inventoryQuery.GetBalancesAsync(context.Warehouse.Id, stockItem.Id)).Single().QuantityOnHand.ShouldBe(8);
        cancellation = await _postConfirmation.ConfirmReturnedGoodsAsync(cancellation.Id, new ConfirmCancellationReturnedGoodsDto
        {
            IsEligibleForRestock = true,
            Reason = "Warehouse received original goods",
            IdempotencyKey = Guid.NewGuid().ToString("N")
        });

        var cancelled = await _sales.GetAsync(order.Id);
        cancelled.Status.ShouldBe(SalesOrderStatus.Cancelled);
        cancelled.PaymentSummary.PaymentStatus.ShouldBe(SalesOrderReceivableStatus.NotApplicable);
        var balance = (await _inventoryQuery.GetBalancesAsync(context.Warehouse.Id, stockItem.Id)).Single();
        balance.QuantityOnHand.ShouldBe(10);
        balance.InventoryValue.ShouldBe(5_000);
        var restoredLot = await GetSingleLotAsync(stockItem.Id);
        restoredLot.AvailableQuantity.ShouldBe(10);
        restoredLot.Status.ShouldBe(InventoryLotStatus.Available);

        var ledger = await _inventoryQuery.GetLedgerAsync(context.Warehouse.Id, stockItem.Id);
        ledger.Count(x => x.Type == InventoryTransactionType.SalesIssue && x.ReferenceId == line.Id).ShouldBe(1);
        var rollback = ledger.Single(x => x.Type == InventoryTransactionType.AdjustmentIncrease && x.ReferenceId == cancellation.Id);
        rollback.ReferenceType.ShouldBe(nameof(SalesOrderCancellation));
        rollback.Lines.Single().LotNo.ShouldBe(restoredLot.LotNo);
        rollback.Lines.Single().Quantity.ShouldBe(2);
        rollback.Lines.Single().UnitCost.ShouldBe(500);
    }

    [Fact]
    public async Task Confirmed_Unpaid_Cancel_Should_Restore_All_Fifo_Lots_For_Multiple_Lines()
    {
        var context = await CreateBaseAsync();
        var component = await _components.CreateAsync(new CreateComponentDto
        {
            Code = Unique("MF"),
            Name = "Multi FIFO Component",
            Unit = "Piece"
        });
        var stockItem = await GetComponentStockItemAsync(component.Id);
        var firstLotNo = Unique("FIFA");
        var secondLotNo = Unique("FIFB");
        await PostReceiptAsync(context.Warehouse.Id, stockItem.Id, 5, 100, firstLotNo);
        await PostReceiptAsync(context.Warehouse.Id, stockItem.Id, 7, 200, secondLotNo);
        var (firstProduct, _) = await CreateProductForComponentAsync(component, 2);
        var (secondProduct, _) = await CreateProductForComponentAsync(component, 3);
        await _warranty.SetMachineSettingAsync(firstProduct.Id, new SetProductMachineSettingDto { IsMachine = true });
        var order = await _sales.CreateAsync(Input(context, firstProduct.Id, 2, 1_000));
        await _sales.AddLineAsync(order.Id, new CreateSalesOrderLineDto
        {
            ProductId = secondProduct.Id,
            Quantity = 2,
            ActualSellingPrice = 1_000
        });

        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });

        var confirmed = await _sales.GetAsync(order.Id);
        confirmed.Lines.Count.ShouldBe(2);
        confirmed.Lines.Sum(x => x.BomSnapshotItems.Sum(item => item.TotalRequiredQuantity)).ShouldBe(10);
        var issuedLots = await GetLotsAsync(stockItem.Id);
        issuedLots.Single(x => x.LotNo == firstLotNo).AvailableQuantity.ShouldBe(0);
        issuedLots.Single(x => x.LotNo == firstLotNo).Status.ShouldBe(InventoryLotStatus.Depleted);
        issuedLots.Single(x => x.LotNo == secondLotNo).AvailableQuantity.ShouldBe(2);
        var issuedBalance = (await _inventoryQuery.GetBalancesAsync(context.Warehouse.Id, stockItem.Id)).Single();
        issuedBalance.QuantityOnHand.ShouldBe(2);
        issuedBalance.InventoryValue.ShouldBe(400);

        var cancellation = await _postConfirmation.CancelConfirmedAsync(order.Id, new CancelConfirmedSalesOrderDto
        {
            ReasonGroup = "CustomerChangedMind",
            Reason = "Customer cancelled before installation"
        });
        await _postConfirmation.ConfirmReturnedGoodsAsync(cancellation.Id, new ConfirmCancellationReturnedGoodsDto
        {
            IsEligibleForRestock = true,
            Reason = "Warehouse received all goods",
            IdempotencyKey = Guid.NewGuid().ToString("N")
        });

        var cancelled = await _sales.GetAsync(order.Id);
        cancelled.Status.ShouldBe(SalesOrderStatus.Cancelled);
        var restoredLots = await GetLotsAsync(stockItem.Id);
        restoredLots.Single(x => x.LotNo == firstLotNo).AvailableQuantity.ShouldBe(5);
        restoredLots.Single(x => x.LotNo == firstLotNo).Status.ShouldBe(InventoryLotStatus.Available);
        restoredLots.Single(x => x.LotNo == secondLotNo).AvailableQuantity.ShouldBe(7);
        restoredLots.Single(x => x.LotNo == secondLotNo).Status.ShouldBe(InventoryLotStatus.Available);
        var restoredBalance = (await _inventoryQuery.GetBalancesAsync(context.Warehouse.Id, stockItem.Id)).Single();
        restoredBalance.QuantityOnHand.ShouldBe(12);
        restoredBalance.InventoryValue.ShouldBe(1_900);

        var ledger = await _inventoryQuery.GetLedgerAsync(context.Warehouse.Id, stockItem.Id);
        ledger.Count(x => x.Type == InventoryTransactionType.SalesIssue &&
                          confirmed.Lines.Select(line => line.Id).Contains(x.ReferenceId!.Value)).ShouldBe(2);
        var rollbackTransactions = ledger
            .Where(x => x.Type == InventoryTransactionType.AdjustmentIncrease && x.ReferenceId == cancellation.Id)
            .ToList();
        rollbackTransactions.Count.ShouldBe(1);
        rollbackTransactions.SelectMany(x => x.Lines).Where(x => x.LotNo == firstLotNo).Sum(x => x.Quantity).ShouldBe(5);
        rollbackTransactions.SelectMany(x => x.Lines).Where(x => x.LotNo == secondLotNo).Sum(x => x.Quantity).ShouldBe(5);
        rollbackTransactions.SelectMany(x => x.Lines).Where(x => x.LotNo == firstLotNo).ShouldAllBe(x => x.UnitCost == 100);
        rollbackTransactions.SelectMany(x => x.Lines).Where(x => x.LotNo == secondLotNo).ShouldAllBe(x => x.UnitCost == 200);

        (await _postConfirmation.CancelConfirmedAsync(order.Id, new CancelConfirmedSalesOrderDto
        {
            ReasonGroup = "Replay",
            Reason = "Replay"
        })).Id.ShouldBe(cancellation.Id);
        var ledgerAfterSecondCancel = await _inventoryQuery.GetLedgerAsync(context.Warehouse.Id, stockItem.Id);
        ledgerAfterSecondCancel.Count(x => x.Type == InventoryTransactionType.AdjustmentIncrease &&
                                           x.ReferenceId == cancellation.Id).ShouldBe(1);
        (await _inventoryQuery.GetBalancesAsync(context.Warehouse.Id, stockItem.Id)).Single().QuantityOnHand.ShouldBe(12);
    }

    [Fact]
    public async Task Confirm_Should_Skip_Tracked_Depleted_Fifo_Lot_When_Later_Line_Uses_Same_Component()
    {
        var context = await CreateBaseAsync();
        var component = await _components.CreateAsync(new CreateComponentDto
        {
            Code = Unique("SFL"),
            Name = "Shared FIFO Line Component",
            Unit = "Piece"
        });
        var stockItem = await GetComponentStockItemAsync(component.Id);
        var firstLotNo = Unique("SFLA");
        var secondLotNo = Unique("SFLB");
        await PostReceiptAsync(context.Warehouse.Id, stockItem.Id, 2, 100, firstLotNo);
        await PostReceiptAsync(context.Warehouse.Id, stockItem.Id, 50, 200, secondLotNo);
        var (firstProduct, _) = await CreateProductForComponentAsync(component);
        var (secondProduct, _) = await CreateProductForComponentAsync(component);
        var (thirdProduct, _) = await CreateProductForComponentAsync(component);
        var order = await _sales.CreateAsync(Input(context, firstProduct.Id, 1, 1_000));
        await _sales.AddLineAsync(order.Id, new CreateSalesOrderLineDto
        {
            ProductId = secondProduct.Id,
            Quantity = 1,
            ActualSellingPrice = 1_000
        });
        await _sales.AddLineAsync(order.Id, new CreateSalesOrderLineDto
        {
            ProductId = thirdProduct.Id,
            Quantity = 1,
            ActualSellingPrice = 1_000
        });

        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });

        var confirmed = await _sales.GetAsync(order.Id);
        confirmed.Lines.Count.ShouldBe(3);
        var issuedLots = await GetLotsAsync(stockItem.Id);
        issuedLots.Single(x => x.LotNo == firstLotNo).AvailableQuantity.ShouldBe(0);
        issuedLots.Single(x => x.LotNo == secondLotNo).AvailableQuantity.ShouldBe(49);
        var balance = (await _inventoryQuery.GetBalancesAsync(context.Warehouse.Id, stockItem.Id)).Single();
        balance.QuantityOnHand.ShouldBe(49);
    }

    [Fact]
    public async Task Confirmed_Paid_Cancel_Should_Be_Blocked_And_Leave_Inventory_Unchanged()
    {
        var context = await CreateBaseAsync();
        var component = await CreateComponentWithStockAsync(context.Warehouse.Id, 10, 500);
        var stockItem = await GetComponentStockItemAsync(component.Id);
        var (product, _) = await CreateProductForComponentAsync(component);
        var order = await _sales.CreateAsync(Input(context, product.Id, 2, 1_000));
        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });
        await _sales.AddPaymentAsync(order.Id, new CreateSalesOrderPaymentDto
        {
            Amount = 500,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = SalesPaymentMethod.Cash,
            ReferenceNo = "CANCEL-BLOCKED",
            IdempotencyKey = Guid.NewGuid().ToString("N")
        });

        var exception = await Should.ThrowAsync<BusinessException>(() => _sales.CancelAsync(order.Id));

        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.SalesRevisionNotAllowed);
        (await _sales.GetAsync(order.Id)).Status.ShouldBe(SalesOrderStatus.Confirmed);
        var balance = (await _inventoryQuery.GetBalancesAsync(context.Warehouse.Id, stockItem.Id)).Single();
        balance.QuantityOnHand.ShouldBe(8);
        balance.InventoryValue.ShouldBe(4_000);
        (await GetSingleLotAsync(stockItem.Id)).AvailableQuantity.ShouldBe(8);
    }

    [Fact]
    public async Task Price_Only_Revision_Should_Carry_Payment_And_Not_Post_Inventory()
    {
        var context = await CreateBaseAsync();
        var component = await CreateComponentWithStockAsync(context.Warehouse.Id, 10, 500);
        var (product, _) = await CreateProductForComponentAsync(component);
        var order = await _sales.CreateAsync(Input(context, product.Id, 2, 1_000));
        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });
        await InsertPaymentAsync(order.Id, context.Customer.Id, 1_500, "REV-PAY");
        var beforeTransactionCount = await InventoryTransactionCountAsync();

        var revision = await _postConfirmation.OpenRevisionAsync(order.Id, new OpenSalesOrderRevisionDto { Reason = "Correct selling price" });
        var revisionLine = revision.Lines.Single();
        revision = await _postConfirmation.UpdateRevisionAsync(revision.Id, new UpdateSalesOrderRevisionDto
        {
            CustomerId = context.Customer.Id,
            Lines =
            [
                new UpdateSalesOrderRevisionLineDto
                {
                    RevisionLineId = revisionLine.Id,
                    ProductId = product.Id,
                    Quantity = 2,
                    ActualSellingPrice = 600,
                    OverrideReason = "Manager correction"
                }
            ]
        });
        revision = await _postConfirmation.ApplyRevisionAsync(revision.Id, new ApplySalesOrderRevisionDto
        {
            IdempotencyKey = Guid.NewGuid().ToString("N")
        });

        revision.Status.ShouldBe(SalesOrderRevisionStatus.Applied);
        revision.RefundDue.ShouldBe(300);
        (await _sales.GetAsync(order.Id)).TotalRevenueAmount.ShouldBe(1_200);
        (await _sales.GetPaymentSummaryAsync(order.Id)).RefundDue.ShouldBe(300);
        (await InventoryTransactionCountAsync()).ShouldBe(beforeTransactionCount);
    }

    [Fact]
    public async Task Revision_Eligibility_Should_Allow_NonMachine_Paid_And_Mixed_Orders()
    {
        var context = await CreateBaseAsync();
        var component = await CreateComponentWithStockAsync(context.Warehouse.Id, 10, 100);
        var (product, _) = await CreateProductForComponentAsync(component);
        var draft = await _sales.CreateAsync(Input(context, product.Id, 1, 1_000));
        (await Should.ThrowAsync<BusinessException>(() => _postConfirmation.OpenRevisionAsync(
            draft.Id, new OpenSalesOrderRevisionDto { Reason = "Draft is not eligible" })))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.SalesRevisionNotAllowed);

        await _sales.ConfirmAsync(draft.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });
        await InsertPaymentAsync(draft.Id, context.Customer.Id, 100, "OPEN-WITH-PAYMENT");
        var revision = await _postConfirmation.OpenRevisionAsync(
            draft.Id, new OpenSalesOrderRevisionDto { Reason = "Non-machine paid correction" });
        (await Should.ThrowAsync<BusinessException>(() => _postConfirmation.OpenRevisionAsync(
            draft.Id, new OpenSalesOrderRevisionDto { Reason = "Duplicate active revision" })))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.SalesRevisionAlreadyActive);
        var line = revision.Lines.Single();
        (await Should.ThrowAsync<BusinessException>(() => _postConfirmation.UpdateRevisionAsync(
            revision.Id,
            new UpdateSalesOrderRevisionDto
            {
                CustomerId = Guid.NewGuid(),
                Lines = [new UpdateSalesOrderRevisionLineDto { RevisionLineId = line.Id, ProductId = product.Id, Quantity = 1, ActualSellingPrice = 1_000 }]
            }))).Code.ShouldBe(VPureLuxDomainErrorCodes.SalesRevisionNotAllowed);

        await _postConfirmation.CancelRevisionAsync(revision.Id, new ReasonDto { Reason = "Eligibility test complete" });
        var secondComponent = await CreateComponentWithStockAsync(context.Warehouse.Id, 10, 120);
        var (machineProduct, _) = await CreateProductForComponentAsync(secondComponent);
        await _warranty.SetMachineSettingAsync(machineProduct.Id, new SetProductMachineSettingDto { IsMachine = true });
        var mixed = await _sales.CreateAsync(new CreateSalesOrderDto
        {
            CustomerId = context.Customer.Id,
            WarehouseId = context.Warehouse.Id,
            Lines =
            [
                new CreateSalesOrderLineDto { ProductId = product.Id, Quantity = 1, ActualSellingPrice = 1_000 },
                new CreateSalesOrderLineDto { ProductId = machineProduct.Id, Quantity = 1, ActualSellingPrice = 2_000 }
            ]
        });
        await _sales.ConfirmAsync(mixed.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });
        var mixedRevision = await _postConfirmation.OpenRevisionAsync(
            mixed.Id, new OpenSalesOrderRevisionDto { Reason = "Mixed-order correction" });
        mixedRevision.Lines.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Product_Replacement_Revision_Should_Reverse_Old_And_Issue_New()
    {
        var context = await CreateBaseAsync();
        var oldComponent = await CreateComponentWithStockAsync(context.Warehouse.Id, 5, 100);
        var newComponent = await CreateComponentWithStockAsync(context.Warehouse.Id, 5, 300);
        var oldStock = await GetComponentStockItemAsync(oldComponent.Id);
        var newStock = await GetComponentStockItemAsync(newComponent.Id);
        var (oldProduct, _) = await CreateProductForComponentAsync(oldComponent);
        var (newProduct, _) = await CreateProductForComponentAsync(newComponent);
        var order = await _sales.CreateAsync(Input(context, oldProduct.Id, 1, 1_000));
        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });
        var revision = await _postConfirmation.OpenRevisionAsync(order.Id, new OpenSalesOrderRevisionDto { Reason = "Replace product" });
        var line = revision.Lines.Single();
        await _postConfirmation.UpdateRevisionAsync(revision.Id, new UpdateSalesOrderRevisionDto
        {
            CustomerId = context.Customer.Id,
            Lines = [new UpdateSalesOrderRevisionLineDto { RevisionLineId = line.Id, ProductId = newProduct.Id, Quantity = 1, ActualSellingPrice = 1_200 }]
        });
        await _postConfirmation.ConfirmRevisionReturnedGoodsAsync(revision.Id, new ConfirmRevisionReturnedGoodsDto
        {
            RevisionLineIds = [line.Id],
            Reason = "Old machine returned"
        });
        var applied = await _postConfirmation.ApplyRevisionAsync(revision.Id, new ApplySalesOrderRevisionDto { IdempotencyKey = Guid.NewGuid().ToString("N") });

        applied.Lines.Single().IssueInventoryTransactionId.ShouldNotBeNull();
        applied.Lines.Single().ReversalInventoryTransactionId.ShouldNotBeNull();
        (await _inventoryQuery.GetBalancesAsync(context.Warehouse.Id, oldStock.Id)).Single().QuantityOnHand.ShouldBe(5);
        (await _inventoryQuery.GetBalancesAsync(context.Warehouse.Id, newStock.Id)).Single().QuantityOnHand.ShouldBe(4);
        var effective = (await _sales.GetAsync(order.Id)).Lines.Single();
        effective.ProductId.ShouldBe(newProduct.Id);
        effective.CostAmountSnapshot.ShouldBe(300);
    }

    [Fact]
    public async Task Installed_Machine_Should_Lock_Adjust_And_Cancel_While_Cancellation_Blocks_Install()
    {
        var context = await CreateBaseAsync();
        var component = await CreateComponentWithStockAsync(context.Warehouse.Id, 10, 100);
        var (product, _) = await CreateProductForComponentAsync(component);
        await _warranty.SetMachineSettingAsync(product.Id, new SetProductMachineSettingDto { IsMachine = true });

        var installedOrder = await _sales.CreateAsync(Input(context, product.Id, 1, 1_000));
        await _sales.ConfirmAsync(installedOrder.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });
        var installedAssetId = await CreatePendingAssetAsync(await _sales.GetAsync(installedOrder.Id));
        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            var asset = await db.CustomerAssets.SingleAsync(x => x.Id == installedAssetId);
            asset.ConfirmInstallation(DateTime.UtcNow, "Test", Guid.NewGuid(), Guid.NewGuid().ToString("N"));
            await db.SaveChangesAsync();
        });
        (await Should.ThrowAsync<BusinessException>(() => _postConfirmation.OpenRevisionAsync(
            installedOrder.Id, new OpenSalesOrderRevisionDto { Reason = "Too late" })))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.SalesInstallationLocksModification);
        (await Should.ThrowAsync<BusinessException>(() => _postConfirmation.CancelConfirmedAsync(
            installedOrder.Id, new CancelConfirmedSalesOrderDto { ReasonGroup = "Customer", Reason = "Too late" })))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.SalesInstallationLocksModification);

        var cancelledOrder = await _sales.CreateAsync(Input(context, product.Id, 1, 1_000));
        await _sales.ConfirmAsync(cancelledOrder.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });
        var pendingAssetId = await CreatePendingAssetAsync(await _sales.GetAsync(cancelledOrder.Id));
        await _postConfirmation.CancelConfirmedAsync(cancelledOrder.Id, new CancelConfirmedSalesOrderDto
        {
            ReasonGroup = "Customer",
            Reason = "Cancelled before install"
        });
        (await Should.ThrowAsync<BusinessException>(() => _warranty.ConfirmInstallationAsync(
            pendingAssetId,
            new ConfirmAssetInstallationDto
            {
                InstalledAt = DateTime.UtcNow,
                IdempotencyKey = Guid.NewGuid().ToString("N"),
                Positions = [new AssetInstallationPositionDto { PositionCode = "P1", PositionName = "Position 1", Quantity = 1 }]
            }))).Code.ShouldBe(VPureLuxDomainErrorCodes.SalesRevisionNotAllowed);
    }

    [Fact]
    public async Task Added_And_Removed_Lines_Should_Post_Only_Their_Own_Deltas()
    {
        var context = await CreateBaseAsync();
        var componentA = await CreateComponentWithStockAsync(context.Warehouse.Id, 5, 100);
        var componentB = await CreateComponentWithStockAsync(context.Warehouse.Id, 5, 200);
        var componentC = await CreateComponentWithStockAsync(context.Warehouse.Id, 5, 300);
        var stockA = await GetComponentStockItemAsync(componentA.Id);
        var stockB = await GetComponentStockItemAsync(componentB.Id);
        var stockC = await GetComponentStockItemAsync(componentC.Id);
        var (productA, _) = await CreateProductForComponentAsync(componentA);
        var (productB, _) = await CreateProductForComponentAsync(componentB);
        var (productC, _) = await CreateProductForComponentAsync(componentC);
        var order = await _sales.CreateAsync(Input(context, productA.Id, 1, 1_000));
        await _sales.AddLineAsync(order.Id, new CreateSalesOrderLineDto { ProductId = productB.Id, Quantity = 1, ActualSellingPrice = 1_000 });
        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });
        var revision = await _postConfirmation.OpenRevisionAsync(order.Id, new OpenSalesOrderRevisionDto { Reason = "Remove A and add C" });
        var lineA = revision.Lines.Single(x => x.ProductId == productA.Id);
        var lineB = revision.Lines.Single(x => x.ProductId == productB.Id);
        await _postConfirmation.UpdateRevisionAsync(revision.Id, new UpdateSalesOrderRevisionDto
        {
            CustomerId = context.Customer.Id,
            Lines =
            [
                new UpdateSalesOrderRevisionLineDto { RevisionLineId = lineA.Id, IsRemoved = true, ProductId = productA.Id, Quantity = 1, ActualSellingPrice = 1_000 },
                new UpdateSalesOrderRevisionLineDto { RevisionLineId = lineB.Id, ProductId = productB.Id, Quantity = 1, ActualSellingPrice = 1_000 },
                new UpdateSalesOrderRevisionLineDto { ProductId = productC.Id, Quantity = 1, ActualSellingPrice = 1_000 }
            ]
        });
        await _postConfirmation.ConfirmRevisionReturnedGoodsAsync(revision.Id, new ConfirmRevisionReturnedGoodsDto
        {
            RevisionLineIds = [lineA.Id],
            Reason = "A returned"
        });
        await _postConfirmation.ApplyRevisionAsync(revision.Id, new ApplySalesOrderRevisionDto { IdempotencyKey = Guid.NewGuid().ToString("N") });

        var effective = await _sales.GetAsync(order.Id);
        effective.Lines.Select(x => x.ProductId).ShouldBe(new[] { productB.Id, productC.Id }, ignoreOrder: true);
        (await _inventoryQuery.GetBalancesAsync(context.Warehouse.Id, stockA.Id)).Single().QuantityOnHand.ShouldBe(5);
        (await _inventoryQuery.GetBalancesAsync(context.Warehouse.Id, stockB.Id)).Single().QuantityOnHand.ShouldBe(4);
        (await _inventoryQuery.GetBalancesAsync(context.Warehouse.Id, stockC.Id)).Single().QuantityOnHand.ShouldBe(4);
        (await _inventoryQuery.GetLedgerAsync(context.Warehouse.Id, stockB.Id))
            .Count(x => x.ReferenceType == nameof(SalesOrderRevisionLine)).ShouldBe(0);
    }

    [Fact]
    public async Task Install_Vs_Cancel_Should_Allow_Exactly_One_Outcome()
    {
        var context = await CreateBaseAsync();
        var component = await CreateComponentWithStockAsync(context.Warehouse.Id, 5, 100);
        var (product, _) = await CreateProductForComponentAsync(component);
        var order = await _sales.CreateAsync(Input(context, product.Id, 1, 1_000));
        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });
        var assetId = await CreatePendingAssetAsync(await _sales.GetAsync(order.Id));
        var install = TryActionAsync(() => _warranty.ConfirmInstallationAsync(assetId, InstallationInput()));
        var cancel = TryActionAsync(() => _postConfirmation.CancelConfirmedAsync(order.Id, new CancelConfirmedSalesOrderDto
        {
            ReasonGroup = "Customer",
            Reason = "Concurrent cancellation"
        }));

        var outcomes = await Task.WhenAll(install, cancel);
        outcomes.Count(x => x).ShouldBe(1);
        var finalOrder = await _sales.GetAsync(order.Id);
        var finalAsset = await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            return await db.CustomerAssets.AsNoTracking().SingleAsync(x => x.Id == assetId);
        });
        (finalOrder.Status == SalesOrderStatus.Cancelled || finalAsset.InstalledAt.HasValue).ShouldBeTrue();
        (finalOrder.Status == SalesOrderStatus.Cancelled && finalAsset.InstalledAt.HasValue).ShouldBeFalse();
    }

    [Fact]
    public async Task Install_Vs_Open_Revision_Should_Allow_Exactly_One_Outcome()
    {
        var context = await CreateBaseAsync();
        var component = await CreateComponentWithStockAsync(context.Warehouse.Id, 5, 100);
        var (product, _) = await CreateProductForComponentAsync(component);
        await _warranty.SetMachineSettingAsync(product.Id, new SetProductMachineSettingDto { IsMachine = true });
        var order = await _sales.CreateAsync(Input(context, product.Id, 1, 1_000));
        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });
        var assetId = await CreatePendingAssetAsync(await _sales.GetAsync(order.Id));
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var open = Task.Run(async () =>
        {
            await start.Task;
            return await TryActionAsync(() => _postConfirmation.OpenRevisionAsync(
                order.Id, new OpenSalesOrderRevisionDto { Reason = "Concurrent adjustment" }));
        });
        var install = Task.Run(async () =>
        {
            await start.Task;
            return await TryActionAsync(() => _warranty.ConfirmInstallationAsync(assetId, InstallationInput()));
        });

        start.SetResult();
        var outcomes = await Task.WhenAll(open, install);
        outcomes.Count(x => x).ShouldBe(1);
    }

    [Fact]
    public async Task Quantity_Increase_Revision_Should_Issue_Only_Delta_And_Replay_Idempotently()
    {
        var context = await CreateBaseAsync();
        var component = await CreateComponentWithStockAsync(context.Warehouse.Id, 10, 100);
        var stockItem = await GetComponentStockItemAsync(component.Id);
        var (product, _) = await CreateProductForComponentAsync(component);
        var order = await _sales.CreateAsync(Input(context, product.Id, 2, 1_000));
        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });
        var revision = await _postConfirmation.OpenRevisionAsync(order.Id, new OpenSalesOrderRevisionDto { Reason = "Increase quantity" });
        var line = revision.Lines.Single();
        await _postConfirmation.UpdateRevisionAsync(revision.Id, new UpdateSalesOrderRevisionDto
        {
            CustomerId = context.Customer.Id,
            Lines = [new UpdateSalesOrderRevisionLineDto { RevisionLineId = line.Id, ProductId = product.Id, Quantity = 3, ActualSellingPrice = 1_000 }]
        });
        var key = Guid.NewGuid().ToString("N");
        var applied = await _postConfirmation.ApplyRevisionAsync(revision.Id, new ApplySalesOrderRevisionDto { IdempotencyKey = key });
        var replay = await _postConfirmation.ApplyRevisionAsync(revision.Id, new ApplySalesOrderRevisionDto { IdempotencyKey = key });

        replay.Id.ShouldBe(applied.Id);
        (await _inventoryQuery.GetBalancesAsync(context.Warehouse.Id, stockItem.Id)).Single().QuantityOnHand.ShouldBe(7);
        (await _sales.GetAsync(order.Id)).Lines.Single().CostAmountSnapshot.ShouldBe(300);
        (await _inventoryQuery.GetLedgerAsync(context.Warehouse.Id, stockItem.Id))
            .Count(x => x.ReferenceType == nameof(SalesOrderRevisionLine)).ShouldBe(1);
    }

    [Fact]
    public async Task Quantity_Decrease_Revision_Should_Require_Return_And_Reverse_Original_Cost()
    {
        var context = await CreateBaseAsync();
        var component = await CreateComponentWithStockAsync(context.Warehouse.Id, 10, 125);
        var stockItem = await GetComponentStockItemAsync(component.Id);
        var (product, _) = await CreateProductForComponentAsync(component);
        var order = await _sales.CreateAsync(Input(context, product.Id, 3, 1_000));
        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });
        var revision = await _postConfirmation.OpenRevisionAsync(order.Id, new OpenSalesOrderRevisionDto { Reason = "Decrease quantity" });
        var line = revision.Lines.Single();
        await _postConfirmation.UpdateRevisionAsync(revision.Id, new UpdateSalesOrderRevisionDto
        {
            CustomerId = context.Customer.Id,
            Lines = [new UpdateSalesOrderRevisionLineDto { RevisionLineId = line.Id, ProductId = product.Id, Quantity = 2, ActualSellingPrice = 1_000 }]
        });
        (await Should.ThrowAsync<BusinessException>(() => _postConfirmation.ApplyRevisionAsync(
            revision.Id, new ApplySalesOrderRevisionDto { IdempotencyKey = Guid.NewGuid().ToString("N") })))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.SalesRevisionReturnConfirmationRequired);
        await _postConfirmation.ConfirmRevisionReturnedGoodsAsync(revision.Id, new ConfirmRevisionReturnedGoodsDto
        {
            RevisionLineIds = [line.Id],
            Reason = "Warehouse received one unit"
        });
        await _postConfirmation.ApplyRevisionAsync(revision.Id, new ApplySalesOrderRevisionDto { IdempotencyKey = Guid.NewGuid().ToString("N") });

        (await _inventoryQuery.GetBalancesAsync(context.Warehouse.Id, stockItem.Id)).Single().QuantityOnHand.ShouldBe(8);
        var effectiveLine = (await _sales.GetAsync(order.Id)).Lines.Single();
        effectiveLine.Quantity.ShouldBe(2);
        effectiveLine.CostAmountSnapshot.ShouldBe(250);
    }

    [Fact]
    public async Task Post_Confirmation_Task_Lists_Should_Query_Filter_And_Page_Server_Side()
    {
        var context = await CreateBaseAsync();
        var component = await CreateComponentWithStockAsync(context.Warehouse.Id, 10, 100);
        var (product, _) = await CreateProductForComponentAsync(component);

        var adjustedOrder = await _sales.CreateAsync(Input(context, product.Id, 2, 1_000));
        await _sales.ConfirmAsync(adjustedOrder.Id, new ConfirmSalesOrderDto
        {
            IdempotencyKey = Guid.NewGuid().ToString("N")
        });
        var revision = await _postConfirmation.OpenRevisionAsync(adjustedOrder.Id, new OpenSalesOrderRevisionDto
        {
            Reason = "Warehouse task query"
        });
        var revisionLine = revision.Lines.Single();
        await _postConfirmation.UpdateRevisionAsync(revision.Id, new UpdateSalesOrderRevisionDto
        {
            CustomerId = context.Customer.Id,
            Lines =
            [
                new UpdateSalesOrderRevisionLineDto
                {
                    RevisionLineId = revisionLine.Id,
                    ProductId = product.Id,
                    Quantity = 1,
                    ActualSellingPrice = 1_000
                }
            ]
        });

        var returnTasks = await _postConfirmation.GetReturnTasksAsync(new GetSalesReturnTasksInput
        {
            SearchText = adjustedOrder.OrderNo,
            SkipCount = 0,
            MaxResultCount = 1
        });
        returnTasks.TotalCount.ShouldBe(1);
        returnTasks.Items.Single().TaskType.ShouldBe(SalesReturnTaskType.RevisionLine);
        returnTasks.Items.Single().RevisionLineId.ShouldBe(revisionLine.Id);
        returnTasks.Items.Single().Quantity.ShouldBe(1);

        var cancelledOrder = await _sales.CreateAsync(Input(context, product.Id, 1, 1_000));
        await _sales.ConfirmAsync(cancelledOrder.Id, new ConfirmSalesOrderDto
        {
            IdempotencyKey = Guid.NewGuid().ToString("N")
        });
        await InsertPaymentAsync(cancelledOrder.Id, context.Customer.Id, 600, "TASK-REFUND");
        var cancellation = await _postConfirmation.CancelConfirmedAsync(cancelledOrder.Id, new CancelConfirmedSalesOrderDto
        {
            ReasonGroup = "CustomerChangedMind",
            Reason = "Accounting task query"
        });

        var sortedReturnTasks = await _postConfirmation.GetReturnTasksAsync(new GetSalesReturnTasksInput
        {
            Sorting = "orderNo desc",
            SkipCount = 0,
            MaxResultCount = 10
        });
        sortedReturnTasks.TotalCount.ShouldBe(2);
        sortedReturnTasks.Items.Select(x => x.OrderNo).ShouldBe([
            cancelledOrder.OrderNo,
            adjustedOrder.OrderNo
        ]);

        var refundTasks = await _postConfirmation.GetRefundTasksAsync(new GetSalesRefundTasksInput
        {
            SearchText = cancelledOrder.OrderNo,
            Sorting = "remainingAmount desc",
            SkipCount = 0,
            MaxResultCount = 1
        });
        refundTasks.TotalCount.ShouldBe(1);
        refundTasks.Items.Single().TaskType.ShouldBe(SalesRefundTaskType.Cancellation);
        refundTasks.Items.Single().OperationId.ShouldBe(cancellation.Id);
        refundTasks.Items.Single().RemainingAmount.ShouldBe(600);
    }

    [Fact]
    public async Task Machine_Quantity_Revisions_Should_Reconcile_Only_Pending_Assets()
    {
        var context = await CreateBaseAsync();
        var component = await CreateComponentWithStockAsync(context.Warehouse.Id, 10, 100);
        var (product, _) = await CreateProductForComponentAsync(component);
        await _warranty.SetMachineSettingAsync(product.Id, new SetProductMachineSettingDto { IsMachine = true });
        var order = await _sales.CreateAsync(Input(context, product.Id, 1, 1_000));
        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });
        var originalAssetId = await CreatePendingAssetAsync(await _sales.GetAsync(order.Id));

        var increase = await _postConfirmation.OpenRevisionAsync(order.Id, new OpenSalesOrderRevisionDto { Reason = "Add two machines" });
        var increaseLine = increase.Lines.Single();
        await _postConfirmation.UpdateRevisionAsync(increase.Id, new UpdateSalesOrderRevisionDto
        {
            CustomerId = context.Customer.Id,
            Lines = [new UpdateSalesOrderRevisionLineDto { RevisionLineId = increaseLine.Id, ProductId = product.Id, Quantity = 3, ActualSellingPrice = 1_000 }]
        });
        await _postConfirmation.ApplyRevisionAsync(increase.Id, new ApplySalesOrderRevisionDto { IdempotencyKey = Guid.NewGuid().ToString("N") });

        var afterIncrease = await GetOrderAssetsAsync(order.Id);
        afterIncrease.Count.ShouldBe(3);
        afterIncrease.ShouldContain(x => x.Id == originalAssetId);
        afterIncrease.ShouldAllBe(x => x.Status == CustomerAssetStatus.PendingInstallation);

        var decrease = await _postConfirmation.OpenRevisionAsync(order.Id, new OpenSalesOrderRevisionDto { Reason = "Return two machines" });
        var decreaseLine = decrease.Lines.Single();
        await _postConfirmation.UpdateRevisionAsync(decrease.Id, new UpdateSalesOrderRevisionDto
        {
            CustomerId = context.Customer.Id,
            Lines = [new UpdateSalesOrderRevisionLineDto { RevisionLineId = decreaseLine.Id, ProductId = product.Id, Quantity = 1, ActualSellingPrice = 1_000 }]
        });
        await _postConfirmation.ConfirmRevisionReturnedGoodsAsync(decrease.Id, new ConfirmRevisionReturnedGoodsDto
        {
            RevisionLineIds = [decreaseLine.Id], Reason = "Warehouse received two machines"
        });
        await _postConfirmation.ApplyRevisionAsync(decrease.Id, new ApplySalesOrderRevisionDto { IdempotencyKey = Guid.NewGuid().ToString("N") });

        var afterDecrease = await GetOrderAssetsAsync(order.Id);
        afterDecrease.Single(x => x.SourceUnitIndex == 1).Status.ShouldBe(CustomerAssetStatus.PendingInstallation);
        afterDecrease.Where(x => x.SourceUnitIndex > 1).ShouldAllBe(x => x.Status == CustomerAssetStatus.Cancelled);
    }

    [Fact]
    public async Task Machine_Product_Replacement_Should_Cancel_Old_Asset_And_Create_New_Asset()
    {
        var context = await CreateBaseAsync();
        var oldComponent = await CreateComponentWithStockAsync(context.Warehouse.Id, 5, 100);
        var newComponent = await CreateComponentWithStockAsync(context.Warehouse.Id, 5, 200);
        var (oldProduct, _) = await CreateProductForComponentAsync(oldComponent);
        var (newProduct, _) = await CreateProductForComponentAsync(newComponent);
        await _warranty.SetMachineSettingAsync(oldProduct.Id, new SetProductMachineSettingDto { IsMachine = true });
        await _warranty.SetMachineSettingAsync(newProduct.Id, new SetProductMachineSettingDto { IsMachine = true });
        var order = await _sales.CreateAsync(Input(context, oldProduct.Id, 1, 1_000));
        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });
        var originalLineId = (await _sales.GetAsync(order.Id)).Lines.Single().Id;
        var originalAssetId = await CreatePendingAssetAsync(await _sales.GetAsync(order.Id));

        var revision = await _postConfirmation.OpenRevisionAsync(order.Id, new OpenSalesOrderRevisionDto { Reason = "Replace machine" });
        var line = revision.Lines.Single();
        await _postConfirmation.UpdateRevisionAsync(revision.Id, new UpdateSalesOrderRevisionDto
        {
            CustomerId = context.Customer.Id,
            Lines = [new UpdateSalesOrderRevisionLineDto { RevisionLineId = line.Id, ProductId = newProduct.Id, Quantity = 1, ActualSellingPrice = 1_200 }]
        });
        await _postConfirmation.ConfirmRevisionReturnedGoodsAsync(revision.Id, new ConfirmRevisionReturnedGoodsDto
        {
            RevisionLineIds = [line.Id], Reason = "Old machine returned"
        });
        await _postConfirmation.ApplyRevisionAsync(revision.Id, new ApplySalesOrderRevisionDto { IdempotencyKey = Guid.NewGuid().ToString("N") });

        var effectiveLine = (await _sales.GetAsync(order.Id)).Lines.Single();
        effectiveLine.Id.ShouldNotBe(originalLineId);
        var assets = await GetOrderAssetsAsync(order.Id);
        assets.Single(x => x.Id == originalAssetId).Status.ShouldBe(CustomerAssetStatus.Cancelled);
        var replacement = assets.Single(x => x.Id != originalAssetId);
        replacement.ProductId.ShouldBe(newProduct.Id);
        replacement.SalesOrderLineId.ShouldBe(effectiveLine.Id);
        replacement.Status.ShouldBe(CustomerAssetStatus.PendingInstallation);
    }

    [Fact]
    public async Task Added_And_Removed_Machine_Line_Should_Create_Then_Cancel_Pending_Asset()
    {
        var context = await CreateBaseAsync();
        var regularComponent = await CreateComponentWithStockAsync(context.Warehouse.Id, 5, 100);
        var machineComponent = await CreateComponentWithStockAsync(context.Warehouse.Id, 5, 200);
        var (regularProduct, _) = await CreateProductForComponentAsync(regularComponent);
        var (machineProduct, _) = await CreateProductForComponentAsync(machineComponent);
        await _warranty.SetMachineSettingAsync(machineProduct.Id, new SetProductMachineSettingDto { IsMachine = true });
        var order = await _sales.CreateAsync(Input(context, regularProduct.Id, 1, 500));
        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });

        var add = await _postConfirmation.OpenRevisionAsync(order.Id, new OpenSalesOrderRevisionDto { Reason = "Add machine" });
        var regular = add.Lines.Single();
        await _postConfirmation.UpdateRevisionAsync(add.Id, new UpdateSalesOrderRevisionDto
        {
            CustomerId = context.Customer.Id,
            Lines =
            [
                new UpdateSalesOrderRevisionLineDto { RevisionLineId = regular.Id, ProductId = regularProduct.Id, Quantity = 1, ActualSellingPrice = 500 },
                new UpdateSalesOrderRevisionLineDto { ProductId = machineProduct.Id, Quantity = 1, ActualSellingPrice = 1_000 }
            ]
        });
        await _postConfirmation.ApplyRevisionAsync(add.Id, new ApplySalesOrderRevisionDto { IdempotencyKey = Guid.NewGuid().ToString("N") });
        var createdAsset = (await GetOrderAssetsAsync(order.Id)).Single();
        createdAsset.Status.ShouldBe(CustomerAssetStatus.PendingInstallation);

        var remove = await _postConfirmation.OpenRevisionAsync(order.Id, new OpenSalesOrderRevisionDto { Reason = "Remove machine" });
        var regularRevisionLine = remove.Lines.Single(x => x.ProductId == regularProduct.Id);
        var machineRevisionLine = remove.Lines.Single(x => x.ProductId == machineProduct.Id);
        await _postConfirmation.UpdateRevisionAsync(remove.Id, new UpdateSalesOrderRevisionDto
        {
            CustomerId = context.Customer.Id,
            Lines =
            [
                new UpdateSalesOrderRevisionLineDto { RevisionLineId = regularRevisionLine.Id, ProductId = regularProduct.Id, Quantity = 1, ActualSellingPrice = 500 },
                new UpdateSalesOrderRevisionLineDto { RevisionLineId = machineRevisionLine.Id, IsRemoved = true, ProductId = machineProduct.Id, Quantity = 1, ActualSellingPrice = 1_000 }
            ]
        });
        await _postConfirmation.ConfirmRevisionReturnedGoodsAsync(remove.Id, new ConfirmRevisionReturnedGoodsDto
        {
            RevisionLineIds = [machineRevisionLine.Id], Reason = "Warehouse received machine"
        });
        await _postConfirmation.ApplyRevisionAsync(remove.Id, new ApplySalesOrderRevisionDto { IdempotencyKey = Guid.NewGuid().ToString("N") });

        (await GetOrderAssetsAsync(order.Id)).Single().Status.ShouldBe(CustomerAssetStatus.Cancelled);
    }

    [Fact]
    public async Task Insufficient_Delta_Stock_Should_Roll_Back_Entire_Revision()
    {
        var context = await CreateBaseAsync();
        var component = await CreateComponentWithStockAsync(context.Warehouse.Id, 2, 100);
        var stockItem = await GetComponentStockItemAsync(component.Id);
        var (product, _) = await CreateProductForComponentAsync(component);
        var order = await _sales.CreateAsync(Input(context, product.Id, 1, 1_000));
        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });
        var revision = await _postConfirmation.OpenRevisionAsync(order.Id, new OpenSalesOrderRevisionDto { Reason = "Too large increase" });
        var line = revision.Lines.Single();
        await _postConfirmation.UpdateRevisionAsync(revision.Id, new UpdateSalesOrderRevisionDto
        {
            CustomerId = context.Customer.Id,
            Lines = [new UpdateSalesOrderRevisionLineDto { RevisionLineId = line.Id, ProductId = product.Id, Quantity = 5, ActualSellingPrice = 1_000 }]
        });

        await Should.ThrowAsync<BusinessException>(() => _postConfirmation.ApplyRevisionAsync(
            revision.Id, new ApplySalesOrderRevisionDto { IdempotencyKey = Guid.NewGuid().ToString("N") }));

        (await _postConfirmation.GetRevisionAsync(revision.Id)).Status.ShouldBe(SalesOrderRevisionStatus.Draft);
        (await _sales.GetAsync(order.Id)).Lines.Single().Quantity.ShouldBe(1);
        (await _inventoryQuery.GetBalancesAsync(context.Warehouse.Id, stockItem.Id)).Single().QuantityOnHand.ShouldBe(1);
    }

    [Fact]
    public async Task Paid_Cancellation_Should_Be_Effective_Immediately_With_Independent_Obligations()
    {
        var context = await CreateBaseAsync();
        var component = await CreateComponentWithStockAsync(context.Warehouse.Id, 5, 100);
        var (product, _) = await CreateProductForComponentAsync(component);
        var order = await _sales.CreateAsync(Input(context, product.Id, 1, 1_000));
        await _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });
        await InsertPaymentAsync(order.Id, context.Customer.Id, 600, "CANCEL-PAID");

        var cancellation = await _postConfirmation.CancelConfirmedAsync(order.Id, new CancelConfirmedSalesOrderDto
        {
            ReasonGroup = "CustomerChangedMind",
            Reason = "Cancelled before installation"
        });

        (await _sales.GetAsync(order.Id)).Status.ShouldBe(SalesOrderStatus.Cancelled);
        cancellation.StockStatus.ShouldBe(SalesOrderCancellationStockStatus.PendingReturn);
        cancellation.PaymentStatus.ShouldBe(SalesOrderCancellationPaymentStatus.PendingRefund);
        cancellation.RefundDue.ShouldBe(600);
        (await _sales.GetPaymentsAsync(order.Id)).Single().Status.ShouldBe(SalesOrderPaymentStatus.Posted);
        await _postConfirmation.RecordCancellationRefundAsync(cancellation.Id, new RecordSalesOrderRefundDto
        {
            Amount = 600,
            RefundedAt = DateTime.UtcNow,
            PaymentMethod = SalesPaymentMethod.Cash,
            Reason = "Refunded customer",
            IdempotencyKey = Guid.NewGuid().ToString("N")
        });
        cancellation = await _postConfirmation.GetCancellationAsync(cancellation.Id);
        cancellation.PaymentStatus.ShouldBe(SalesOrderCancellationPaymentStatus.Completed);
        cancellation.ClosedAt.ShouldBeNull();
        cancellation = await _postConfirmation.ConfirmReturnedGoodsAsync(cancellation.Id, new ConfirmCancellationReturnedGoodsDto
        {
            IsEligibleForRestock = true,
            Reason = "Goods received",
            IdempotencyKey = Guid.NewGuid().ToString("N")
        });
        cancellation.ClosedAt.ShouldNotBeNull();
        (await _sales.GetPaymentsAsync(order.Id)).Single().Status.ShouldBe(SalesOrderPaymentStatus.Posted);
    }

    [Fact]
    public async Task Inventory_Failure_Should_Leave_Order_Draft_Without_Snapshot()
    {
        var context = await CreateBaseAsync();
        var component = await _components.CreateAsync(new CreateComponentDto { Code = Unique("NS"), Name = "No Stock", Unit = "Piece" });
        var (product, _) = await CreateProductForComponentAsync(component);
        var order = await _sales.CreateAsync(Input(context, product.Id, 1, 100));

        (await Should.ThrowAsync<BusinessException>(() =>
            _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") })))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.SalesInventoryValidationFailed);

        var reloaded = await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            db.ChangeTracker.Clear();
            return await GetRequiredService<ISalesOrderRepository>().GetAsync(order.Id, includeDetails: true);
        });
        reloaded.Status.ShouldBe(SalesOrderStatus.Draft);
        reloaded.CustomerCodeSnapshot.ShouldBeEmpty();
        reloaded.Lines.Single().InventoryTransactionId.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Validate_Customer_Warehouse_And_Bom()
    {
        var context = await CreateBaseAsync();
        var component = await _components.CreateAsync(new CreateComponentDto { Code = Unique("VC"), Name = "Validation Component", Unit = "Piece" });
        var (validProduct, _) = await CreateProductForComponentAsync(component);
        var valid = Input(context, validProduct.Id, 1, 100);
        valid.CustomerId = Guid.NewGuid();
        (await Should.ThrowAsync<BusinessException>(() => _sales.CreateAsync(valid))).Code.ShouldBe(VPureLuxDomainErrorCodes.CustomerNotFound);

        valid = Input(context, validProduct.Id, 1, 100);
        valid.WarehouseId = Guid.NewGuid();
        (await Should.ThrowAsync<BusinessException>(() => _sales.CreateAsync(valid))).Code.ShouldBe(VPureLuxDomainErrorCodes.WarehouseNotFound);

        var product = await _products.CreateAsync(new CreateProductDto { Code = Unique("VP"), Name = "Validation Product" });
        (await Should.ThrowAsync<BusinessException>(() =>
            _sales.CreateAsync(Input(context, product.Id, 1, 100))))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.SalesBomMustBePublished);
    }

    [Fact]
    public async Task Should_Reject_Inactive_CustomerGroup_And_Warehouse()
    {
        var group = await _groups.CreateAsync(new CreateCustomerGroupDto { Code = Unique("IG"), Name = "Inactive Group" });
        var customer = await _customers.CreateAsync(new CreateCustomerDto { Code = Unique("IC"), Name = "Inactive Group Customer", CustomerGroupId = group.Id });
        var warehouse = await _warehouses.CreateAsync(new CreateWarehouseDto { Code = Unique("IW"), Name = "Inactive Warehouse" });
        var component = await _components.CreateAsync(new CreateComponentDto { Code = Unique("II"), Name = "Validation Component", Unit = "Piece" });
        var (product, _) = await CreateProductForComponentAsync(component);
        await _groups.DeactivateAsync(group.Id);
        var context = (customer, warehouse);
        var order = await _sales.CreateAsync(Input(context, product.Id, 1, 100));
        (await Should.ThrowAsync<BusinessException>(() =>
            _sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") })))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.CustomerGroupInactive);

        await _warehouses.DeactivateAsync(warehouse.Id);
        (await Should.ThrowAsync<BusinessException>(() =>
            _sales.CreateAsync(Input(context, product.Id, 1, 100))))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.WarehouseInactive);
    }

    [Fact]
    public async Task Different_Order_Using_Same_Confirmation_Key_Should_Be_Rejected()
    {
        var context = await CreateBaseAsync();
        var component = await CreateComponentWithStockAsync(context.Warehouse.Id, 10, 10);
        var (product, _) = await CreateProductForComponentAsync(component);
        var first = await _sales.CreateAsync(Input(context, product.Id, 1, 20));
        var second = await _sales.CreateAsync(Input(context, product.Id, 1, 20));
        var key = Guid.NewGuid().ToString("N");
        await _sales.ConfirmAsync(first.Id, new ConfirmSalesOrderDto { IdempotencyKey = key });
        (await Should.ThrowAsync<BusinessException>(() =>
            _sales.ConfirmAsync(second.Id, new ConfirmSalesOrderDto { IdempotencyKey = key })))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.DuplicateConfirmationKey);
    }

    [Fact]
    public async Task Stale_Concurrent_Update_Should_Be_Translated_To_Sales_003()
    {
        var context = await CreateBaseAsync();
        var component = await _components.CreateAsync(new CreateComponentDto { Code = Unique("CC"), Name = "Concurrent Component", Unit = "Piece" });
        var (product, _) = await CreateProductForComponentAsync(component);
        var order = await _sales.CreateAsync(Input(context, product.Id, 1, 100));

        var exception = await Should.ThrowAsync<BusinessException>(() => WithUnitOfWorkAsync(async () =>
        {
            var repository = GetRequiredService<ISalesOrderRepository>();
            var aggregate = await repository.GetAsync(order.Id, includeDetails: true);
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            db.Entry(aggregate).Property(x => x.RowVersion).OriginalValue = [99];
            aggregate.CancelDraft(DateTime.UtcNow);
            await repository.UpdateAsync(aggregate, autoSave: true);
        }));
        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.SalesConcurrentModification);
    }

    [Fact]
    public async Task Database_Should_Translate_Duplicate_OrderNo_To_Sales_001()
    {
        var context = await CreateBaseAsync();
        var component = await _components.CreateAsync(new CreateComponentDto { Code = Unique("DO"), Name = "Duplicate Order Component", Unit = "Piece" });
        var (product, _) = await CreateProductForComponentAsync(component);
        var order = await _sales.CreateAsync(Input(context, product.Id, 1, 100));

        var exception = await Should.ThrowAsync<BusinessException>(() => WithUnitOfWorkAsync(async () =>
        {
            var duplicate = await GetRequiredService<SalesManager>().CreateAsync(context.Customer.Id, context.Warehouse.Id, DateTime.UtcNow);
            typeof(SalesOrder).GetProperty(nameof(SalesOrder.OrderNo))!.SetValue(duplicate, order.OrderNo);
            await GetRequiredService<ISalesOrderRepository>().InsertAsync(duplicate, autoSave: true);
        }));
        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.DuplicateOrderNo);
    }

    [Fact]
    public async Task Database_Should_Translate_Duplicate_ConfirmationKey_To_Sales_002()
    {
        var context = await CreateBaseAsync();
        var component = await CreateComponentWithStockAsync(context.Warehouse.Id, 10, 10);
        var (product, bom) = await CreateProductForComponentAsync(component);
        var first = await _sales.CreateAsync(Input(context, product.Id, 1, 20));
        var second = await _sales.CreateAsync(Input(context, product.Id, 1, 20));
        var key = Guid.NewGuid().ToString("N");
        await _sales.ConfirmAsync(first.Id, new ConfirmSalesOrderDto { IdempotencyKey = key });
        var firstConfirmed = await _sales.GetAsync(first.Id);

        var exception = await Should.ThrowAsync<BusinessException>(() => WithUnitOfWorkAsync(async () =>
        {
            var repository = GetRequiredService<ISalesOrderRepository>();
            var aggregate = await repository.GetAsync(second.Id, includeDetails: true);
            aggregate.ApplyCustomerSnapshot(
                context.Customer.Code, context.Customer.Name, context.Customer.CustomerGroupId,
                context.Customer.CustomerGroupCode, context.Customer.CustomerGroupName);
            aggregate.ApplyLineConfirmationSnapshot(
                aggregate.Lines.Single().Id, product.Code, product.Name, SalesConsts.DefaultProductUnit, bom.VersionNo,
                firstConfirmed.Lines.Single().InventoryTransactionId!.Value, 10);
            aggregate.Confirm(key, DateTime.UtcNow);
            await repository.UpdateAsync(aggregate, autoSave: true);
        }));
        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.DuplicateConfirmationKey);
    }

    private async Task<(CustomerDto Customer, WarehouseDto Warehouse)> CreateBaseAsync()
    {
        var group = await _groups.CreateAsync(new CreateCustomerGroupDto { Code = Unique("SG"), Name = "Sales Group" });
        var customer = await _customers.CreateAsync(new CreateCustomerDto { Code = Unique("SC"), Name = "Sales Customer", CustomerGroupId = group.Id });
        var warehouse = await _warehouses.CreateAsync(new CreateWarehouseDto { Code = Unique("SW"), Name = "Sales Warehouse" });
        return (customer, warehouse);
    }

    private async Task<VPureLux.Catalog.Components.ComponentDto> CreateComponentWithStockAsync(Guid warehouseId, decimal quantity, decimal cost)
    {
        var component = await _components.CreateAsync(new CreateComponentDto { Code = Unique("SI"), Name = "Sales Inventory Component", Unit = "Piece" });
        var stockItem = (await _stockItems.FindByCatalogItemAsync(StockItemType.Component, component.Id))!;
        await PostReceiptAsync(warehouseId, stockItem.Id, quantity, cost, Unique("LOT"));
        return component;
    }

    private async Task<StockItem> GetComponentStockItemAsync(Guid componentId) =>
        await _stockItems.FindByCatalogItemAsync(StockItemType.Component, componentId)
        ?? throw new InvalidOperationException($"Component StockItem was not synchronized for {componentId}.");

    private async Task<InventoryLot> GetSingleLotAsync(Guid stockItemId) =>
        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            return await db.InventoryLots.AsNoTracking().SingleAsync(x => x.StockItemId == stockItemId);
        });

    private async Task<List<InventoryLot>> GetLotsAsync(Guid stockItemId) =>
        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            return await db.InventoryLots
                .AsNoTracking()
                .Where(x => x.StockItemId == stockItemId)
                .OrderBy(x => x.ReceivedAt)
                .ThenBy(x => x.CreationTime)
                .ThenBy(x => x.Id)
                .ToListAsync();
        });

    private async Task PostReceiptAsync(Guid warehouseId, Guid stockItemId, decimal quantity, decimal unitCost, string lotNo)
    {
        await _inventory.PostReceiptAsync(new PostReceiptDto
        {
            WarehouseId = warehouseId,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines =
            [
                new ReceiptLineInput
                {
                    StockItemId = stockItemId,
                    Quantity = quantity,
                    UnitCost = unitCost,
                    LotNo = lotNo,
                    ReceivedAt = DateTime.UtcNow
                }
            ]
        });
    }

    private async Task InsertPaymentAsync(Guid salesOrderId, Guid customerId, decimal amount, string referenceNo)
    {
        await WithUnitOfWorkAsync(async () =>
        {
            await _payments.InsertAsync(new SalesOrderPayment(
                Guid.NewGuid(),
                salesOrderId,
                customerId,
                amount,
                DateTime.UtcNow,
                SalesPaymentMethod.Cash,
                referenceNo,
                "Test payment",
                Guid.NewGuid().ToString("N")), autoSave: true);
        });
    }

    private async Task<int> InventoryTransactionCountAsync() =>
        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            return await db.InventoryTransactions.CountAsync();
        });

    private async Task<Guid> CreatePendingAssetAsync(SalesOrderDto order, int unitIndex = 1)
    {
        var line = order.Lines.Single();
        var asset = CustomerAsset.CreateSoldMachine(
            Guid.NewGuid(), order.CustomerId, line.ProductId, order.Id, line.Id, line.LineNo, unitIndex,
            Unique("ASSET"), order.OrderNo, order.CustomerCodeSnapshot, order.CustomerNameSnapshot,
            line.ItemCodeSnapshot, line.ItemNameSnapshot, order.ConfirmedAt ?? order.OrderDate);
        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            db.CustomerAssets.Add(asset);
            await db.SaveChangesAsync();
        });
        return asset.Id;
    }

    private async Task<List<CustomerAsset>> GetOrderAssetsAsync(Guid orderId) =>
        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            return await db.CustomerAssets.AsNoTracking()
                .Where(x => x.SalesOrderId == orderId)
                .OrderBy(x => x.SalesOrderLineId)
                .ThenBy(x => x.SourceUnitIndex)
                .ToListAsync();
        });

    private static ConfirmAssetInstallationDto InstallationInput() => new()
    {
        InstalledAt = DateTime.UtcNow,
        IdempotencyKey = Guid.NewGuid().ToString("N"),
        Positions = [new AssetInstallationPositionDto { PositionCode = "P1", PositionName = "Position 1", Quantity = 1 }]
    };

    private static async Task<bool> TryActionAsync(Func<Task> action)
    {
        try
        {
            await action();
            return true;
        }
        catch (BusinessException)
        {
            return false;
        }
    }

    private static async Task<bool> TryActionAsync<T>(Func<Task<T>> action)
    {
        try
        {
            await action();
            return true;
        }
        catch (BusinessException)
        {
            return false;
        }
    }

    private async Task<(ProductDto Product, BomVersionDto Bom)> CreateProductForComponentAsync(
        VPureLux.Catalog.Components.ComponentDto component,
        decimal quantity = 1)
    {
        var product = await _products.CreateAsync(new CreateProductDto { Code = Unique("SP"), Name = $"SKU {component.Code}" });
        var bom = await _boms.CreateAsync(product.Id, new CreateBomVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Items = [new CreateBomItemDto { ComponentId = component.Id, Quantity = quantity }]
        });
        await _boms.PublishAsync(bom.Id);
        return (product, bom);
    }

    private static CreateSalesOrderDto Input(
        (CustomerDto Customer, WarehouseDto Warehouse) context,
        Guid productId,
        decimal quantity,
        decimal? actual) => new()
    {
        CustomerId = context.Customer.Id,
        WarehouseId = context.Warehouse.Id,
        OrderDate = DateTime.Now.Date,
        Lines = [new CreateSalesOrderLineDto { ProductId = productId, Quantity = quantity, ActualSellingPrice = actual }]
    };

    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];

    private static void DisableCustomerCareIntake(CustomerCareOptions options)
    {
        options.IsEnabled = false;
        options.IsSalesIntakeEnabled = false;
        options.SalesIntakeGoLiveFrom = null;
    }
}
