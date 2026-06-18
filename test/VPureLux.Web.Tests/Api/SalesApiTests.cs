using System;
using System.Net;
using System.Net.Http.Json;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using VPureLux.Bom;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using VPureLux.Customers;
using VPureLux.Customers.CustomerGroups;
using VPureLux.Inventory;
using VPureLux.Sales;
using Xunit;

namespace VPureLux.Api;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class SalesApiTests : VPureLuxWebTestBase
{
    [Fact]
    public async Task Should_Expose_Order_Line_Confirmation_Cancellation_And_History_Routes()
    {
        var context = await CreateContextAsync();
        var create = await Client.PostAsJsonAsync("/api/sales/orders", OrderInput(context));
        create.StatusCode.ShouldBe(HttpStatusCode.OK, await create.Content.ReadAsStringAsync());
        var order = await create.Content.ReadFromJsonAsync<SalesOrderDto>();

        (await Client.GetAsync($"/api/sales/orders/{order!.Id}")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await Client.GetAsync("/api/sales/orders")).StatusCode.ShouldBe(HttpStatusCode.OK);

        var add = await Client.PostAsJsonAsync($"/api/sales/orders/{order.Id}/lines", new CreateSalesOrderLineDto
        {
            ProductId = context.ProductId, Quantity = 1, ActualSellingPrice = 100
        });
        add.StatusCode.ShouldBe(HttpStatusCode.OK, await add.Content.ReadAsStringAsync());
        var added = await add.Content.ReadFromJsonAsync<SalesOrderDto>();
        var line = added!.Lines.Last();

        (await Client.PutAsJsonAsync($"/api/sales/orders/{order.Id}/lines/{line.Id}", new UpdateSalesOrderLineDto
        {
            Quantity = 2, ActualSellingPrice = 100
        })).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await Client.DeleteAsync($"/api/sales/orders/{order.Id}/lines/{line.Id}")).StatusCode.ShouldBe(HttpStatusCode.OK);

        var confirm = await Client.PostAsJsonAsync($"/api/sales/orders/{order.Id}/confirm", new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });
        confirm.StatusCode.ShouldBe(HttpStatusCode.OK, await confirm.Content.ReadAsStringAsync());
        (await Client.GetAsync($"/api/sales/customers/{context.CustomerId}/purchase-history")).StatusCode.ShouldBe(HttpStatusCode.OK);

        var cancelOrderResponse = await Client.PostAsJsonAsync("/api/sales/orders", OrderInput(context));
        var cancelOrder = await cancelOrderResponse.Content.ReadFromJsonAsync<SalesOrderDto>();
        (await Client.PostAsJsonAsync($"/api/sales/orders/{cancelOrder!.Id}/cancel", new { })).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Should_Reject_Invalid_Create_Request()
    {
        var response = await Client.PostAsJsonAsync("/api/sales/orders", new CreateSalesOrderDto());
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private async Task<(Guid CustomerId, Guid WarehouseId, Guid ProductId)> CreateContextAsync()
    {
        var groups = GetRequiredService<ICustomerGroupAppService>();
        var customers = GetRequiredService<ICustomerAppService>();
        var warehouses = GetRequiredService<IWarehouseAppService>();
        var components = GetRequiredService<IComponentAppService>();
        var products = GetRequiredService<IProductAppService>();
        var boms = GetRequiredService<IBomAppService>();
        var stockItems = GetRequiredService<IStockItemRepository>();
        var inventory = GetRequiredService<IInventoryTransactionAppService>();
        var group = await groups.CreateAsync(new CreateCustomerGroupDto { Code = Unique("AG"), Name = "API Group" });
        var customer = await customers.CreateAsync(new CreateCustomerDto { Code = Unique("AC"), Name = "API Customer", CustomerGroupId = group.Id });
        var warehouse = await warehouses.CreateAsync(new CreateWarehouseDto { Code = Unique("AW"), Name = "API Warehouse" });
        var component = await components.CreateAsync(new CreateComponentDto { Code = Unique("AI"), Name = "API Component", Unit = "Piece" });
        var stockItem = (await stockItems.FindByCatalogItemAsync(StockItemType.Component, component.Id))!;
        await inventory.PostReceiptAsync(new PostReceiptDto
        {
            WarehouseId = warehouse.Id, IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines = [new ReceiptLineInput { StockItemId = stockItem.Id, Quantity = 20, UnitCost = 50, LotNo = Unique("AL"), ReceivedAt = DateTime.UtcNow }]
        });
        var product = await products.CreateAsync(new CreateProductDto { Code = Unique("AP"), Name = "API Product" });
        var bom = await boms.CreateAsync(product.Id, new CreateBomVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Items = [new CreateBomItemDto { ComponentId = component.Id, Quantity = 1 }]
        });
        await boms.PublishAsync(bom.Id);
        return (customer.Id, warehouse.Id, product.Id);
    }

    private static CreateSalesOrderDto OrderInput((Guid CustomerId, Guid WarehouseId, Guid ProductId) context) => new()
    {
        CustomerId = context.CustomerId,
        WarehouseId = context.WarehouseId,
        Lines = [new CreateSalesOrderLineDto { ProductId = context.ProductId, Quantity = 1, ActualSellingPrice = 100 }]
    };

    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];
}
