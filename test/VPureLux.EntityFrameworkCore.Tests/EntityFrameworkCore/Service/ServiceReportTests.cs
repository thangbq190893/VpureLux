using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shouldly;
using VPureLux.Catalog.Components;
using VPureLux.EntityFrameworkCore.Warranty;
using VPureLux.Permissions;
using VPureLux.Reports;
using VPureLux.Service;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.EntityFrameworkCore;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Service;

public partial class ServiceOrderWorkflowTests
{
    private IBusinessRevenueAppService Reports => GetRequiredService<IBusinessRevenueAppService>();
    private static GetBusinessRevenueListInput ReportInput() => new() { FromDate = new(2026, 9, 1), ToDate = new(2026, 9, 30), MaxResultCount = 10 };

    [Theory]
    [InlineData(null)] [InlineData(0)] [InlineData(50)]
    public async Task S005_cost_snapshot_recognition_and_settlement_remain_separate(int? laborCost)
    {
        var f = await CreateFixtureAsync("S005-COST");
        var work = await CreateWorkAsync(Unique("S005-W"), "Original labor", "A", 800, laborCost);
        await ReceiptAsync(f, 5, 200, 1);
        var o = await StartOrderAsync(f, Material(f.Component.Id, 1, 200), Labor(work.Id, 2, 800));
        await Money.AddPaymentAsync(o.Id, Pay(1500));
        (await Reports.GetServiceSummaryAsync(ReportInput())).TotalRevenue.ShouldBe(0);
        var cmd = Completion(o); cmd.Lines[1].ActualQuantity = 1;
        await Orders.CompleteAsync(o.Id, cmd);
        var row = (await Reports.GetServiceListAsync(ReportInput())).Items.Single();
        row.Revenue.ShouldBe(1000); row.MaterialCost.ShouldBe(200); row.LaborCost.ShouldBe(laborCost);
        row.TotalKnownCost.ShouldBe(200 + (laborCost ?? 0)); row.CostIncomplete.ShouldBe(laborCost == null);
        row.Profit.ShouldBe(laborCost.HasValue ? 800 - laborCost : null);
        row.NetPaid.ShouldBe(1500); row.RefundDue.ShouldBe(500);
        var summary = await Reports.GetServiceSummaryAsync(ReportInput());
        summary.CostIncompleteCount.ShouldBe(laborCost.HasValue ? 0 : 1); summary.Profit.ShouldBe(row.Profit);
        await Money.RefundAsync(o.Id, RefundInput(300));
        await Works.UpdateAsync(work.Id, new() { Code = work.Code, ConcurrencyStamp = (await Works.GetAsync(work.Id)).ConcurrencyStamp, Name = "Changed", Unit = "B", DefaultPrice = 9999, StandardCost = 9999 });
        await GetRequiredService<IComponentAppService>().UpdateAsync(f.Component.Id, new() { Name = "Changed material", Unit = "Different" });
        var after = (await Reports.GetServiceListAsync(ReportInput())).Items.Single();
        after.Revenue.ShouldBe(row.Revenue); after.MaterialCost.ShouldBe(row.MaterialCost); after.LaborCost.ShouldBe(row.LaborCost); after.Profit.ShouldBe(row.Profit);
        after.NetPaid.ShouldBe(1200); after.RefundDue.ShouldBe(200); after.GrossRefunded.ShouldBe(300);
        (await Orders.GetAsync(o.Id)).Lines[1].Unit.ShouldBe("A");
        var cancelled = await StartOrderAsync(f, Labor(f.Work.Id));
        await Money.AddPaymentAsync(cancelled.Id, Pay(50));
        await Orders.CancelAsync(cancelled.Id, new() { ConcurrencyStamp = cancelled.ConcurrencyStamp, Reason = "cancel" });
        (await Reports.GetServiceSummaryAsync(ReportInput())).DocumentCount.ShouldBe(1);
        (await Money.GetSummaryAsync(cancelled.Id)).RefundDue.ShouldBe(50);
    }

    [Fact]
    public async Task S005_permissions_mask_rows_totals_and_block_unauthorized_sources()
    {
        var f = await CreateFixtureAsync("S005-AUTH");
        var o = await StartOrderAsync(f, Labor(f.Work.Id));
        await Orders.CompleteAsync(o.Id, Completion(o));
        using (WarrantyMatrixAuthorizationService.Deny(VPureLuxPermissions.Reports.Service.View))
            await Should.ThrowAsync<AbpAuthorizationException>(() => Reports.GetServiceListAsync(ReportInput()));
        using (WarrantyMatrixAuthorizationService.Deny(VPureLuxPermissions.Service.ViewCost))
        {
            var input = ReportInput(); input.Sorting = "TotalKnownCost desc";
            var row = (await Reports.GetServiceListAsync(input)).Items.Single();
            row.MaterialCost.ShouldBeNull(); row.LaborCost.ShouldBeNull(); row.TotalKnownCost.ShouldBeNull(); row.Profit.ShouldBeNull(); row.CostIncomplete.ShouldBeNull();
            (await Reports.GetServiceSummaryAsync(ReportInput())).TotalKnownCost.ShouldBeNull();
        }
        using (WarrantyMatrixAuthorizationService.Deny(VPureLuxPermissions.Service.ViewProfit))
        {
            (await Reports.GetServiceListAsync(ReportInput())).Items.Single().Profit.ShouldBeNull();
            (await Reports.GetServiceSummaryAsync(ReportInput())).Profit.ShouldBeNull();
        }
        (await Reports.GetServiceListAsync(ReportInput())).Items.Single().Profit.ShouldBe(50);
        using (WarrantyMatrixAuthorizationService.Deny(VPureLuxPermissions.Reports.Sales.View))
            await Should.ThrowAsync<AbpAuthorizationException>(() => Reports.GetConsolidatedListAsync(ReportInput()));
        using (WarrantyMatrixAuthorizationService.Deny(VPureLuxPermissions.Reports.Consolidated.View))
            await Should.ThrowAsync<AbpAuthorizationException>(() => Reports.GetConsolidatedSummaryAsync(ReportInput()));
        GetRequiredService<IOptions<ServiceOptions>>().Value.IsEnabled = false;
        await Should.ThrowAsync<BusinessException>(() => Reports.GetServiceListAsync(ReportInput()));
    }

    [Fact]
    public async Task S005_paging_sort_search_and_UTC7_day_boundaries_are_database_filtered()
    {
        var f = await CreateFixtureAsync("S005-PAGE");
        for (var i = 0; i < 13; i++)
        {
            var o = await StartOrderAsync(f, Labor(f.Work.Id, 1, i));
            var cmd = Completion(o); cmd.CompletedAt = new DateTimeOffset(2026, 9, 7, 0, 0, 0, TimeSpan.FromHours(7)).AddMinutes(i);
            await Orders.CompleteAsync(o.Id, cmd);
        }
        var last = await StartOrderAsync(f, Labor(f.Work.Id)); var end = Completion(last);
        end.CompletedAt = new DateTimeOffset(2026, 9, 8, 0, 0, 0, TimeSpan.FromHours(7));
        await Orders.CompleteAsync(last.Id, end);
        var input = ReportInput(); input.FromDate = input.ToDate = new DateTime(2026, 9, 7); input.Sorting = "Revenue asc"; input.SkipCount = 10;
        var page = await Reports.GetServiceListAsync(input);
        page.TotalCount.ShouldBe(13); page.Items.Select(x => x.Revenue).ShouldBe(new decimal[] { 10, 11, 12 });
        (await Reports.GetServiceSummaryAsync(input)).TotalRevenue.ShouldBe(78);
        input.SearchText = page.Items.Last().DocumentNo; input.SkipCount = 0;
        (await Reports.GetServiceListAsync(input)).TotalCount.ShouldBe(1);
        input.FromDate = new(2026, 9, 9);
        await Should.ThrowAsync<UserFriendlyException>(() => Reports.GetServiceListAsync(input));
    }

    [Fact]
    public async Task S005_unperformed_unknown_labor_is_not_missing_cost_and_legacy_times_are_not_shifted()
    {
        var f = await CreateFixtureAsync("S005-LEGACY");
        var unknown = await CreateWorkAsync(Unique("UNKNOWN"), "Unknown labor", "Visit", 100, null);
        var zero = await StartOrderAsync(f, Labor(unknown.Id));
        var command = Completion(zero); command.Lines[0].ActualQuantity = 0;
        await Orders.CompleteAsync(zero.Id, command);
        (await Orders.GetAsync(zero.Id)).IsLegacyCompletion.ShouldBeFalse();
        var zeroRow = (await Reports.GetServiceListAsync(ReportInput())).Items.Single();
        zeroRow.Revenue.ShouldBe(0); zeroRow.CostIncomplete.ShouldBe(false); zeroRow.LaborCost.ShouldBe(0); zeroRow.Profit.ShouldBe(0);
        await ReceiptAsync(f, 1, 200, 1);
        var legacy = await StartOrderAsync(f, Material(f.Component.Id, 1, 500), Labor(unknown.Id, 1, 500));
        await Orders.CompleteAsync(legacy.Id, Completion(legacy));
        await WithUnitOfWorkAsync(async () =>
        {
            var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
            var entity = await db.ServiceOrders.Include(x => x.Lines).SingleAsync(x => x.Id == legacy.Id);
            db.Entry(entity).Property(x => x.CompletionCommandHash).CurrentValue = null;
            db.Entry(entity).Property(x => x.CompletedAt).CurrentValue = new DateTime(2026, 9, 7, 23, 59, 59).AddTicks(9999999);
            foreach (var line in entity.Lines) db.Entry(line).Property(x => x.ActualCostAmount).CurrentValue = null;
            await db.SaveChangesAsync();
        });
        var input = ReportInput(); input.FromDate = input.ToDate = new DateTime(2026, 9, 7);
        var legacyDetails = await Orders.GetAsync(legacy.Id);
        legacyDetails.IsLegacyCompletion.ShouldBeTrue();
        legacyDetails.CompletedAt!.Value.Date.ShouldBe(new DateTime(2026, 9, 7));
        var row = (await Reports.GetServiceListAsync(input)).Items.Single(x => x.DocumentId == legacy.Id);
        row.DocumentDate.Date.ShouldBe(new DateTime(2026, 9, 7)); row.MaterialCost.ShouldBe(200);
        row.LaborCost.ShouldBeNull(); row.CostIncomplete.ShouldBe(true); row.Profit.ShouldBeNull();
        (await Reports.GetServiceSummaryAsync(input)).KnownMaterialCost.ShouldBe(200);
    }
}
