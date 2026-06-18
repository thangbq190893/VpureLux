using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Shouldly;
using VPureLux.Catalog.Components;
using VPureLux.Inventory;
using Xunit;

namespace VPureLux.Api;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class InventoryApiTests : VPureLuxWebTestBase
{
    [Fact]
    public async Task Should_Expose_Receipt_Issue_Adjustment_And_Query_Routes()
    {
        var context = await CreateContextAsync();
        var receipt = await Client.PostAsJsonAsync("/api/inventory/transactions/receipts", new PostReceiptDto
        {
            WarehouseId = context.WarehouseId, IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines = [new ReceiptLineInput { StockItemId = context.StockItemId, Quantity = 10, LotNo = "API-LOT", UnitCost = 30000, ReceivedAt = DateTime.UtcNow }]
        });
        receipt.StatusCode.ShouldBe(HttpStatusCode.OK, await receipt.Content.ReadAsStringAsync());

        var issue = await Client.PostAsJsonAsync("/api/inventory/transactions/issues", new PostIssueDto
        {
            WarehouseId = context.WarehouseId, IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines = [new IssueLineInput { StockItemId = context.StockItemId, Quantity = 2 }]
        });
        issue.StatusCode.ShouldBe(HttpStatusCode.OK, await issue.Content.ReadAsStringAsync());

        var adjustment = await Client.PostAsJsonAsync("/api/inventory/transactions/adjustments", new PostAdjustmentDto
        {
            WarehouseId = context.WarehouseId, IdempotencyKey = Guid.NewGuid().ToString("N"),
            Type = InventoryTransactionType.AdjustmentDecrease, Reason = "Damage",
            DecreaseLines = [new IssueLineInput { StockItemId = context.StockItemId, Quantity = 1 }]
        });
        adjustment.StatusCode.ShouldBe(HttpStatusCode.OK, await adjustment.Content.ReadAsStringAsync());

        (await GetResponseAsObjectAsync<List<InventoryBalanceDto>>("/api/inventory/balances")).ShouldNotBeEmpty();
        (await GetResponseAsObjectAsync<List<InventoryLotDto>>("/api/inventory/lots")).ShouldNotBeEmpty();
        (await GetResponseAsObjectAsync<List<InventoryTransactionDto>>("/api/inventory/ledger")).Count.ShouldBeGreaterThanOrEqualTo(3);
        (await GetResponseAsObjectAsync<Volo.Abp.Application.Dtos.PagedResultDto<StockItemDto>>("/api/inventory/stock-items")).Items.ShouldNotBeEmpty();
        (await GetResponseAsObjectAsync<Volo.Abp.Application.Dtos.PagedResultDto<WarehouseDto>>("/api/inventory/warehouses")).Items.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Should_Reject_Invalid_Adjustment_Request()
    {
        var response = await Client.PostAsJsonAsync("/api/inventory/transactions/adjustments", new PostAdjustmentDto
        {
            WarehouseId = Guid.NewGuid(), IdempotencyKey = Guid.NewGuid().ToString("N"),
            Type = InventoryTransactionType.AdjustmentDecrease, Reason = string.Empty
        });
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private async Task<(Guid WarehouseId, Guid StockItemId)> CreateContextAsync()
    {
        var component = await GetRequiredService<IComponentAppService>().CreateAsync(new CreateComponentDto { Code = Unique("API-I"), Name = "API Inventory", Unit = "Piece" });
        var item = (await GetRequiredService<IStockItemRepository>().FindByCatalogItemAsync(StockItemType.Component, component.Id))!;
        var warehouse = await GetRequiredService<IWarehouseAppService>().CreateAsync(new CreateWarehouseDto { Code = Unique("API-W"), Name = "API Warehouse" });
        return (warehouse.Id, item.Id);
    }
    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];
}
