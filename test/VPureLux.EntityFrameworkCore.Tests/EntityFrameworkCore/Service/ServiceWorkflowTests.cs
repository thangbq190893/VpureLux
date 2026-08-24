using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using VPureLux.Catalog.Components;
using VPureLux.Customers;
using VPureLux.Customers.CustomerGroups;
using VPureLux.Inventory;
using VPureLux.Reports;
using VPureLux.Warranty;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Service;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class ServiceWorkflowTests : VPureLuxEntityFrameworkCoreTestBase
{
    [Fact]
    public async Task Completing_service_should_issue_fifo_and_restart_position_schedule()
    {
        var groups = GetRequiredService<ICustomerGroupAppService>();
        var customers = GetRequiredService<ICustomerAppService>();
        var components = GetRequiredService<IComponentAppService>();
        var warehouses = GetRequiredService<IWarehouseAppService>();
        var stockItems = GetRequiredService<IStockItemRepository>();
        var inventory = GetRequiredService<IInventoryTransactionAppService>();
        var balances = GetRequiredService<IInventoryQueryAppService>();
        var warranty = GetRequiredService<IWarrantyAppService>();
        var service = GetRequiredService<VPureLux.Service.IServiceAppService>();
        var works = GetRequiredService<VPureLux.Service.IServiceWorkAppService>();
        var reports = GetRequiredService<IBusinessRevenueAppService>();

        var group = await groups.CreateAsync(new CreateCustomerGroupDto { Code = Unique("SG"), Name = "Service group" });
        var customer = await customers.CreateAsync(new CreateCustomerDto { Code = Unique("SC"), Name = "Service customer", CustomerGroupId = group.Id });
        var component = await components.CreateAsync(new CreateComponentDto { Code = Unique("CORE"), Name = "Replacement core", Unit = "Piece" });
        await warranty.SetPolicyAsync(component.Id, new SetComponentReplacementPolicyDto { IsEnabled = true, CycleMonths = 3, WarningDaysBeforeDue = 14 });
        var warehouse = await warehouses.CreateAsync(new CreateWarehouseDto { Code = Unique("SWH"), Name = "Service warehouse", IsDefault = true });
        var stockItem = (await stockItems.FindByCatalogItemAsync(StockItemType.Component, component.Id))!;
        await inventory.PostReceiptAsync(new PostReceiptDto
        {
            WarehouseId = warehouse.Id, IdempotencyKey = Guid.NewGuid().ToString("N"), LotNo = Unique("LOT"), ReceivedAt = DateTime.UtcNow.AddDays(-1),
            Lines = [new ReceiptLineInput { StockItemId = stockItem.Id, Quantity = 10, UnitCost = 40_000 }]
        });
        var baseline = new DateTime(2026, 8, 1);
        var assetResult = await warranty.CreateExternalAssetAsync(new CreateExternalCustomerAssetDto
        {
            CustomerId = customer.Id, Model = "External machine", Brand = "Other", IdempotencyKey = Guid.NewGuid().ToString("N"),
            Positions = [new ExternalAssetPositionInput { PositionCode = "CORE-01", PositionName = "Core 1", ComponentId = component.Id, Quantity = 1, ReplacementBaselineDate = baseline }]
        });
        var asset = await warranty.GetAssetDetailsAsync(assetResult.AssetId);
        var work = await works.CreateAsync(new VPureLux.Service.CreateUpdateServiceWorkDto { Code = Unique("WORK"), Name = "Installation labor", DefaultPrice = 100_000 });
        var order = await service.CreateAsync(new VPureLux.Service.CreateServiceOrderDto
        {
            CustomerAssetId = asset.Id, WarehouseId = warehouse.Id,
            Lines =
            [
                new VPureLux.Service.ServiceOrderLineInput { LineType = VPureLux.Service.ServiceOrderLineType.Material, CatalogItemId = component.Id, CustomerAssetComponentId = asset.Positions.Single().Id, Quantity = 2, UnitPrice = 80_000 },
                new VPureLux.Service.ServiceOrderLineInput { LineType = VPureLux.Service.ServiceOrderLineType.Labor, CatalogItemId = work.Id, Quantity = 1, UnitPrice = 100_000 }
            ]
        });
        var advance = await service.AddPaymentAsync(order.Id, new VPureLux.Service.CreateServicePaymentDto
        {
            Amount = 50_000, PaymentDate = DateTime.UtcNow, PaymentMethod = VPureLux.Sales.SalesPaymentMethod.Cash,
            IdempotencyKey = Guid.NewGuid().ToString("N")
        });
        await service.VoidPaymentAsync(order.Id, advance.Id);
        await service.AddPaymentAsync(order.Id, new VPureLux.Service.CreateServicePaymentDto
        {
            Amount = 40_000, PaymentDate = DateTime.UtcNow, PaymentMethod = VPureLux.Sales.SalesPaymentMethod.BankTransfer,
            IdempotencyKey = Guid.NewGuid().ToString("N")
        });
        var advanceOnly = await reports.GetServiceSummaryAsync(new GetBusinessRevenueListInput
        {
            FromDate = DateTime.UtcNow.Date.AddDays(-1), ToDate = DateTime.UtcNow.Date.AddDays(1),
            SearchText = order.OrderNo, MaxResultCount = 10
        });
        advanceOnly.ServiceRevenue.ShouldBe(0);
        advanceOnly.TotalPaid.ShouldBe(0);
        await service.ConfirmAsync(order.Id);
        var completedAt = new DateTime(2026, 8, 24, 10, 0, 0);
        var completed = await service.CompleteAsync(order.Id, new VPureLux.Service.CompleteServiceOrderDto
        {
            CompletedAt = completedAt, IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines = order.Lines.Select(x => new VPureLux.Service.CompleteServiceOrderLineDto { LineId = x.Id, ActualQuantity = x.PlannedQuantity }).ToList()
        });

        completed.Status.ShouldBe(VPureLux.Service.ServiceOrderStatus.Completed);
        completed.TotalRevenueAmount.ShouldBe(260_000);
        completed.TotalCostAmount.ShouldBe(80_000);
        (await balances.GetBalancesAsync(warehouse.Id, stockItem.Id)).Single().QuantityOnHand.ShouldBe(8);
        var reminders = await warranty.GetReminderListAsync(new GetWarrantyReminderListInput { SearchText = asset.AssetNo, Status = AssetReplacementReminderStatus.Pending, MaxResultCount = 10 });
        reminders.Items.ShouldHaveSingleItem().DueDate.ShouldBe(completedAt.Date.AddMonths(3));
        var history = await warranty.GetAssetHistoryAsync(new GetAssetMaintenanceHistoryInput { CustomerAssetId = asset.Id, MaxResultCount = 10 });
        history.Items.ShouldContain(x => x.EventType == AssetMaintenanceEventType.Replacement && x.SourceType == AssetMaintenanceSourceType.ServiceOrder);
        var ledger = await balances.GetLedgerAsync(warehouse.Id, stockItem.Id);
        ledger.ShouldContain(x => x.Type == InventoryTransactionType.ServiceIssue && x.ReferenceId == order.Id);
        var reportInput = new GetBusinessRevenueListInput
        {
            FromDate = completedAt.Date,
            ToDate = completedAt.Date,
            SearchText = completed.OrderNo,
            MaxResultCount = 10
        };
        var serviceSummary = await reports.GetServiceSummaryAsync(reportInput);
        serviceSummary.ServiceRevenue.ShouldBe(260_000);
        serviceSummary.TotalPaid.ShouldBe(40_000);
        serviceSummary.SalesRevenue.ShouldBe(0);
        var consolidated = await reports.GetConsolidatedListAsync(reportInput);
        consolidated.Items.ShouldHaveSingleItem().DocumentId.ShouldBe(order.Id);

        var insufficient = await service.CreateAsync(new VPureLux.Service.CreateServiceOrderDto
        {
            CustomerAssetId = asset.Id, WarehouseId = warehouse.Id,
            Lines =
            [
                new VPureLux.Service.ServiceOrderLineInput
                {
                    LineType = VPureLux.Service.ServiceOrderLineType.Material,
                    CatalogItemId = component.Id,
                    CustomerAssetComponentId = asset.Positions.Single().Id,
                    Quantity = 9,
                    UnitPrice = 80_000
                }
            ]
        });
        await service.ConfirmAsync(insufficient.Id);
        var exception = await Should.ThrowAsync<Volo.Abp.BusinessException>(() => service.CompleteAsync(
            insufficient.Id,
            new VPureLux.Service.CompleteServiceOrderDto
            {
                CompletedAt = completedAt.AddHours(1), IdempotencyKey = Guid.NewGuid().ToString("N"),
                Lines = insufficient.Lines.Select(x => new VPureLux.Service.CompleteServiceOrderLineDto
                    { LineId = x.Id, ActualQuantity = x.PlannedQuantity }).ToList()
            }));
        exception.Code.ShouldBe(VPureLuxDomainErrorCodes.InsufficientInventory);
        (await service.GetAsync(insufficient.Id)).Status.ShouldBe(VPureLux.Service.ServiceOrderStatus.Confirmed);
        (await balances.GetBalancesAsync(warehouse.Id, stockItem.Id)).Single().QuantityOnHand.ShouldBe(8);
        (await balances.GetLedgerAsync(warehouse.Id, stockItem.Id))
            .ShouldNotContain(x => x.ReferenceId == insufficient.Id);
    }

    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];
}
