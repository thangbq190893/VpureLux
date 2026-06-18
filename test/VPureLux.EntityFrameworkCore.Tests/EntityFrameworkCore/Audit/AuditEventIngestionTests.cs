using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using global::VPureLux.Audit;
using VPureLux.Bom.Events;
using VPureLux.Catalog.Events;
using VPureLux.Customers.Events;
using VPureLux.Inventory;
using VPureLux.Inventory.Events;
using VPureLux.Pricing.Events;
using VPureLux.Sales.Events;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Audit;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class AuditEventIngestionTests : VPureLuxEntityFrameworkCoreTestBase
{
    [Fact]
    public async Task Should_Ingest_All_Required_Module_Events_As_Metadata_Only()
    {
        var handler = GetRequiredService<BusinessAuditEventHandler>();
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await WithUnitOfWorkAsync(async () =>
        {
            await handler.HandleEventAsync(new ProductCreatedEvent(id, "P", "Product"));
            await handler.HandleEventAsync(new ProductUpdatedEvent(id, "P", "Updated"));
            await handler.HandleEventAsync(new ProductDeactivatedEvent(id, "P"));
            await handler.HandleEventAsync(new ProductImageChangedEvent(id, "P", null, Hash("N"), "image/png", "p.png"));
            await handler.HandleEventAsync(new ProductImageRemovedEvent(id, "P", Hash("N")));
            await handler.HandleEventAsync(new ComponentCreatedEvent(id, "C", "Component"));
            await handler.HandleEventAsync(new ComponentUpdatedEvent(id, "C", "Updated"));
            await handler.HandleEventAsync(new ComponentDeactivatedEvent(id, "C"));
            await handler.HandleEventAsync(new ComponentImageChangedEvent(id, "C", null, Hash("N"), "image/webp", "c.webp"));
            await handler.HandleEventAsync(new ComponentImageRemovedEvent(id, "C", Hash("N")));
            await handler.HandleEventAsync(new BomVersionCreatedEvent(id, id, 1));
            await handler.HandleEventAsync(new BomPublishedEvent(id, id, 1));
            await handler.HandleEventAsync(new BomArchivedEvent(id, id, 1));
            await handler.HandleEventAsync(new BomVersionClonedEvent(id, Guid.NewGuid(), id, 2));
            await handler.HandleEventAsync(new CustomerCreatedEvent(id, "CU", id));
            await handler.HandleEventAsync(new CustomerUpdatedEvent(id, "CU"));
            await handler.HandleEventAsync(new CustomerActivatedEvent(id, "CU"));
            await handler.HandleEventAsync(new CustomerDeactivatedEvent(id, "CU"));
            await handler.HandleEventAsync(new ComponentSuggestedSellingPriceVersionCreatedEvent(id, id, 1, now));
            await handler.HandleEventAsync(new ProductSuggestedPriceVersionCreatedEvent(id, id, 1, now));
            await handler.HandleEventAsync(new InventoryReceiptPostedEvent(id, id, now));
            await handler.HandleEventAsync(new InventoryIssuePostedEvent(id, id, 650000, now));
            await handler.HandleEventAsync(new InventoryAdjustedEvent(id, id, InventoryTransactionType.AdjustmentIncrease, "Count"));
            await handler.HandleEventAsync(new SalesOrderCreatedEvent(id, "SO-1", id));
            await handler.HandleEventAsync(new SalesOrderConfirmedEvent(id, "SO-1", id, 1000000, 350000));
            await handler.HandleEventAsync(new SalesOrderCancelledEvent(id, "SO-1", id));
        });

        var rows = await GetRequiredService<IBusinessAuditAppService>()
            .GetListAsync(new AuditSearchInput { MaxResultCount = 100 });
        rows.Items.Count.ShouldBeGreaterThanOrEqualTo(26);
        foreach (var module in new[] { "Catalog", "BOM", "Customer", "Pricing", "Inventory", "Sales" })
        {
            rows.Items.ShouldContain(x => x.Module == module);
        }
        rows.Items.Where(x => x.EntityId == id).ShouldAllBe(x =>
            x.OldValueJson == null && x.NewValueJson == null &&
            x.MetadataJson != null &&
            !x.MetadataJson.Contains("Base64", StringComparison.OrdinalIgnoreCase) &&
            !x.MetadataJson.Contains("Thumbnail", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Handler_Should_Declare_All_Required_Event_Contracts()
    {
        var interfaces = typeof(BusinessAuditEventHandler).GetInterfaces();
        foreach (var type in new[]
                 {
                     typeof(ProductCreatedEvent), typeof(ProductUpdatedEvent), typeof(ProductDeactivatedEvent),
                     typeof(ComponentCreatedEvent), typeof(ComponentUpdatedEvent), typeof(ComponentDeactivatedEvent),
                     typeof(ProductImageChangedEvent), typeof(ProductImageRemovedEvent),
                     typeof(ComponentImageChangedEvent), typeof(ComponentImageRemovedEvent),
                     typeof(BomVersionCreatedEvent), typeof(BomPublishedEvent), typeof(BomArchivedEvent), typeof(BomVersionClonedEvent),
                     typeof(CustomerCreatedEvent), typeof(CustomerUpdatedEvent), typeof(CustomerActivatedEvent), typeof(CustomerDeactivatedEvent),
                     typeof(ComponentSuggestedSellingPriceVersionCreatedEvent), typeof(ProductSuggestedPriceVersionCreatedEvent),
                     typeof(InventoryReceiptPostedEvent), typeof(InventoryIssuePostedEvent), typeof(InventoryAdjustedEvent),
                     typeof(SalesOrderCreatedEvent), typeof(SalesOrderConfirmedEvent), typeof(SalesOrderCancelledEvent)
                 })
        {
            interfaces.ShouldContain(x => x.IsGenericType && x.GenericTypeArguments.Single() == type);
        }
    }

    private static string Hash(string marker) => marker.PadRight(64, '0');
}
