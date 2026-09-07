using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Shouldly;
using VPureLux.Permissions;
using VPureLux.Reports;
using VPureLux.Service;
using Volo.Abp.Application.Dtos;
using Volo.Abp.UI.Navigation;
using Xunit;

namespace VPureLux.Pages;

public partial class ServiceOrderWebTests
{
    private async Task SeedReportAsync(int count)
    {
        Enable();
        var f = await CreateFixtureAsync("S005-WEB", "Work");
        var service = GetRequiredService<IServiceOrderAppService>();
        for (var i = 0; i < count; i++)
        {
            var o = await service.CreateAsync(new() { CustomerAssetId = f.AssetId, WarehouseId = f.WarehouseId,
                Lines = [new() { LineType = ServiceOrderLineType.Labor, CatalogItemId = f.WorkId, Quantity = 1, UnitPrice = 1500000.50m + i }] });
            o = await service.ConfirmAsync(o.Id, new() { ConcurrencyStamp = o.ConcurrencyStamp });
            o = await service.StartAsync(o.Id, new() { ConcurrencyStamp = o.ConcurrencyStamp });
            await service.CompleteAsync(o.Id, new() { ConcurrencyStamp = o.ConcurrencyStamp, CompletedAt = new DateTimeOffset(2026, 9, 7, 1, 30, 0, TimeSpan.FromHours(7)),
                IdempotencyKey = Unique("complete"), Lines = [new() { LineId = o.Lines.Single().Id, ActualQuantity = 1 }] });
        }
    }

    private const string ReportRange = "fromDateText=07%2F09%2F2026&toDateText=07%2F09%2F2026";

    [Fact]
    public async Task S005_web_pages_source_filter_real_page2_sort_search_and_dates()
    {
        await SeedReportAsync(12);
        var menu = await GetRequiredService<IMenuManager>().GetAsync(StandardMenus.Main);
        menu.Items.SelectMany(x => x.Items).Any(x => x.Name == "VPureLux.Reports.ServiceRevenue").ShouldBeTrue();
        menu.Items.SelectMany(x => x.Items).Any(x => x.Name == "VPureLux.Reports.BusinessRevenue").ShouldBeTrue();
        foreach (var path in new[] { "ServiceRevenue", "BusinessRevenue" })
        {
            var page = await GetResponseAsStringAsync("/Reports/" + path);
            page.ShouldContain("BusinessReportTable"); page.ShouldContain("type=\"date\"");
            if (path == "BusinessRevenue") page.ShouldContain("ReportSource");
            var rows = await GetResponseAsObjectAsync<PagedResultDto<BusinessRevenueRowDto>>($"/Reports/{path}?handler=List&{ReportRange}&source=2&skipCount=10&maxResultCount=10&sorting=Revenue%20asc");
            rows.TotalCount.ShouldBe(12); rows.Items.Count.ShouldBe(2); rows.Items.First().Revenue.ShouldBe(1500010.50m);
            rows.Items.All(x => x.CostIncomplete == true && x.Profit == null).ShouldBeTrue();
            var summary = await GetResponseAsObjectAsync<BusinessRevenueSummaryDto>($"/Reports/{path}?handler=Summary&{ReportRange}");
            summary.DocumentCount.ShouldBe(12); summary.CostIncompleteCount.ShouldBe(12); summary.Profit.ShouldBeNull();
            var search = await GetResponseAsObjectAsync<PagedResultDto<BusinessRevenueRowDto>>($"/Reports/{path}?handler=List&{ReportRange}&searchText={rows.Items.Last().DocumentNo}");
            search.TotalCount.ShouldBe(1);
        }
        (await GetResponseAsObjectAsync<PagedResultDto<BusinessRevenueRowDto>>($"/Reports/BusinessRevenue?handler=List&{ReportRange}&source=1")).TotalCount.ShouldBe(0);
        (await Client.GetAsync("/Reports/ServiceRevenue?handler=List&fromDateText=invalid")).IsSuccessStatusCode.ShouldBeFalse();
    }

    [Fact]
    public async Task S005_web_permission_deny_allow_masked_server_values_and_disabled_service()
    {
        await SeedReportAsync(1);
        async Task<HttpResponseMessage> Denied(string path, string permission)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Add("X-S001-Deny", permission);
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            return await Client.SendAsync(request);
        }
        foreach (var (path, permission) in new[] { ("ServiceRevenue", VPureLuxPermissions.Reports.Service.View), ("BusinessRevenue", VPureLuxPermissions.Reports.Consolidated.View) })
        {
            (await Denied("/Reports/" + path, permission)).IsSuccessStatusCode.ShouldBeFalse();
            (await Denied($"/Reports/{path}?handler=List&{ReportRange}", permission)).IsSuccessStatusCode.ShouldBeFalse();
        }
        foreach (var permission in new[] { VPureLuxPermissions.Service.ViewCost, VPureLuxPermissions.Service.ViewProfit })
        {
            var response = await Denied($"/Reports/ServiceRevenue?handler=List&{ReportRange}", permission);
            response.StatusCode.ShouldBe(HttpStatusCode.OK, permission + " " + response.Headers.Location + " " + await response.Content.ReadAsStringAsync());
            var row = System.Text.Json.JsonSerializer.Deserialize<PagedResultDto<BusinessRevenueRowDto>>(await response.Content.ReadAsStringAsync(),
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))!.Items.Single();
            row.Profit.ShouldBeNull();
            if (permission == VPureLuxPermissions.Service.ViewCost) { row.TotalKnownCost.ShouldBeNull(); row.MaterialCost.ShouldBeNull(); row.LaborCostKnown.ShouldBeNull(); }
        }
        (await Denied($"/Reports/BusinessRevenue?handler=List&{ReportRange}", VPureLuxPermissions.Reports.Sales.View)).IsSuccessStatusCode.ShouldBeFalse();
        GetRequiredService<IOptions<ServiceOptions>>().Value.IsEnabled = false;
        (await Client.GetAsync($"/Reports/ServiceRevenue?handler=List&{ReportRange}")).IsSuccessStatusCode.ShouldBeFalse();
        (await Client.GetAsync($"/Reports/BusinessRevenue?handler=List&{ReportRange}&source=1")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}

public class BusinessReportUiTests
{
    [Fact]
    public void S005_report_script_is_encoded_server_paged_and_minifies()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "VPureLux.slnx"))) root = root.Parent;
        root.ShouldNotBeNull();
        var js = File.ReadAllText(Path.Combine(root!.FullName, "src/VPureLux.Web/Pages/Reports/BusinessRevenue.js"));
        js.ShouldContain("const summary = () =>"); js.ShouldContain("serverSide: true"); js.ShouldContain("createAjax");
        js.ShouldContain(".text("); js.ShouldContain("enc(v)"); js.ShouldContain("toLocaleString('vi-VN'");
        js.ShouldNotContain("prompt("); js.ShouldNotContain("alert("); js.ShouldNotContain("window.confirm(");
        var minified = NUglify.Uglify.Js(js);
        minified.HasErrors.ShouldBeFalse(string.Join("\n", minified.Errors)); minified.Code.ShouldNotBeNullOrWhiteSpace();
    }
}
