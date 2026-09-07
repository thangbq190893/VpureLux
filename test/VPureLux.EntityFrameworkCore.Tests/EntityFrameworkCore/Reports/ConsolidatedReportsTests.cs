using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Shouldly;
using VPureLux.EntityFrameworkCore.Warranty;
using VPureLux.Permissions;
using VPureLux.Reports;
using VPureLux.Sales;
using VPureLux.Service;
using VPureLux.Warranty;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Reports;

public partial class SalesReportsReadModelTests
{
    [Fact]
    public async Task S005_consolidated_reconciles_effective_sales_refunds_and_completed_service()
    {
        GetRequiredService<IOptions<ServiceOptions>>().Value.IsEnabled = true;
        var context = await CreateBaseAsync();
        var component = await CreateComponentWithStockAsync(context.Warehouse.Id, 20, 200);
        var (product, _) = await CreateProductForComponentAsync(component);
        var normal = await CreateConfirmedOrderAsync(context, product.Id, 1, 6000);
        var adjusted = await _sales.CreateAsync(Input(context, product.Id, 1, 6000));
        await _sales.AddLineAsync(adjusted.Id, new() { ProductId = product.Id, Quantity = 1, ActualSellingPrice = 1000 });
        await _sales.ConfirmAsync(adjusted.Id, new() { IdempotencyKey = Unique("confirm") });
        await InsertPaymentAsync(adjusted.Id, context.Customer.Id, 6000, SalesOrderPaymentStatus.Posted);
        var post = GetRequiredService<ISalesPostConfirmationAppService>();
        var revision = await post.OpenRevisionAsync(adjusted.Id, new() { Reason = "Report revision fixture" });
        var keep = revision.Lines.OrderBy(x => x.LineNo).First(); var remove = revision.Lines.OrderBy(x => x.LineNo).Last();
        await post.UpdateRevisionAsync(revision.Id, new()
        {
            CustomerId = context.Customer.Id,
            Lines = [new() { RevisionLineId = keep.Id, ProductId = product.Id, Quantity = 1, ActualSellingPrice = 4000 },
                new() { RevisionLineId = remove.Id, ProductId = product.Id, Quantity = 1, ActualSellingPrice = 1000, IsRemoved = true }]
        });
        await post.ConfirmRevisionReturnedGoodsAsync(revision.Id, new() { RevisionLineIds = [remove.Id], Reason = "Returned" });
        await post.ApplyRevisionAsync(revision.Id, new() { IdempotencyKey = Unique("apply") });
        await post.RecordRevisionRefundAsync(revision.Id, new() { Amount = 1000, RefundedAt = DateTime.UtcNow,
            PaymentMethod = SalesPaymentMethod.Cash, Reason = "Partial return", IdempotencyKey = Unique("refund") });
        var cancelled = await CreateConfirmedOrderAsync(context, product.Id, 1, 9000);
        await post.CancelConfirmedAsync(cancelled.Id, new() { Reason = "Cancelled", ReasonGroup = "Customer" });
        await _sales.CreateAsync(Input(context, product.Id, 1, 9000));

        var asset = CustomerAsset.CreateExternal(Guid.NewGuid(), context.Customer.Id, Unique("ASSET"), context.Customer.Code, context.Customer.Name, "External machine");
        await WithUnitOfWorkAsync(async () => await GetRequiredService<IRepository<CustomerAsset, Guid>>().InsertAsync(asset, autoSave: true));
        var work = await GetRequiredService<IServiceWorkAppService>().CreateAsync(new() { Code = Unique("WORK"), Name = "Labor", Unit = "Visit", DefaultPrice = 3000, StandardCost = 0 });
        var service = GetRequiredService<IServiceOrderAppService>();
        var money = GetRequiredService<IServicePaymentAppService>();
        var o = await service.CreateAsync(new() { CustomerAssetId = asset.Id, WarehouseId = context.Warehouse.Id,
            Lines = [new() { LineType = ServiceOrderLineType.Labor, CatalogItemId = work.Id, Quantity = 2, UnitPrice = 3000 }] });
        o = await service.ConfirmAsync(o.Id, new() { ConcurrencyStamp = o.ConcurrencyStamp });
        o = await service.StartAsync(o.Id, new() { ConcurrencyStamp = o.ConcurrencyStamp });
        await money.AddPaymentAsync(o.Id, new() { Amount = 4000, PaymentDate = DateTimeOffset.UtcNow, PaymentMethod = SalesPaymentMethod.Cash, IdempotencyKey = Unique("advance") });
        var reports = GetRequiredService<IBusinessRevenueAppService>();
        GetBusinessRevenueListInput InputReport() => new() { FromDate = DateTime.Today.AddDays(-1), ToDate = DateTime.Today.AddDays(1) };
        (await reports.GetConsolidatedSummaryAsync(InputReport())).TotalRevenue.ShouldBe(10000);
        await service.CompleteAsync(o.Id, new() { ConcurrencyStamp = o.ConcurrencyStamp, CompletedAt = DateTimeOffset.UtcNow,
            IdempotencyKey = Unique("complete"), Lines = [new() { LineId = o.Lines.Single().Id, ActualQuantity = 1 }] });
        await money.RefundAsync(o.Id, new() { Amount = 1000, RefundDate = DateTimeOffset.UtcNow, Method = SalesPaymentMethod.Cash, Reason = "Actual refund", IdempotencyKey = Unique("service-refund") });
        var summary = await reports.GetConsolidatedSummaryAsync(InputReport());
        var acceptedSales = await _reports.GetSalesRevenueAsync(new() { FromDate = DateTime.Today.AddDays(-1), ToDate = DateTime.Today.AddDays(1) });
        summary.SalesRevenue.ShouldBe(acceptedSales.Summary.TotalRevenue); summary.SalesRevenue.ShouldBe(10000);
        summary.ServiceRevenue.ShouldBe(3000); summary.TotalRevenue.ShouldBe(13000); summary.DocumentCount.ShouldBe(3);
        var rows = await reports.GetConsolidatedListAsync(InputReport());
        var salesRow = rows.Items.Single(x => x.DocumentId == adjusted.Id);
        salesRow.Revenue.ShouldBe(4000); salesRow.GrossPosted.ShouldBe(6000); salesRow.GrossRefunded.ShouldBe(1000);
        salesRow.NetPaid.ShouldBe(5000); salesRow.RefundDue.ShouldBe(1000);
        rows.Items.Single(x => x.DocumentId == o.Id).NetPaid.ShouldBe(3000);
        rows.Items.Select(x => x.DocumentId).ShouldNotContain(cancelled.Id);
        foreach (var source in new[] { BusinessRevenueSource.Sales, BusinessRevenueSource.Service })
        {
            var input = InputReport(); input.Source = source;
            (await reports.GetConsolidatedListAsync(input)).Items.All(x => x.Source == source).ShouldBeTrue();
        }
        foreach (var permission in new[] { VPureLuxPermissions.Sales.ViewCost, VPureLuxPermissions.Sales.ViewProfit, VPureLuxPermissions.Reports.Profit.View })
        {
            using (WarrantyMatrixAuthorizationService.Deny(permission))
            {
                var denied = await reports.GetConsolidatedListAsync(InputReport());
                denied.Items.Where(x => x.Source == BusinessRevenueSource.Sales).All(x => x.Profit == null).ShouldBeTrue();
                (await reports.GetConsolidatedSummaryAsync(InputReport())).Profit.ShouldBeNull();
                denied.Items.Single(x => x.Source == BusinessRevenueSource.Service).Profit.ShouldBe(3000);
            }
        }
    }
}
