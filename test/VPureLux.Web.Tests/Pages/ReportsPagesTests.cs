using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using VPureLux.Bom;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using VPureLux.Customers;
using VPureLux.Customers.CustomerGroups;
using VPureLux.Inventory;
using VPureLux.Permissions;
using VPureLux.Reports;
using VPureLux.Sales;
using VPureLux.Web.Pages.Reports;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DependencyInjection;
using Xunit;

namespace VPureLux.Pages;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class ReportsPagesTests : VPureLuxWebTestBase
{
    [Fact]
    public async Task Reports_Menu_Should_Expose_Sales_Revenue_With_Report_Permission_Only()
    {
        var menuSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Menus/VPureLuxMenuContributor.cs"));
        var menuConstantsSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Menus/VPureLuxMenus.cs"));

        menuConstantsSource.ShouldContain("ReportsSalesRevenue");
        menuSource.ShouldContain("var reports = new ApplicationMenuItem");
        menuSource.ShouldContain("VPureLuxMenus.ReportsSalesRevenue");
        menuSource.ShouldContain("\"~/Reports/SalesRevenue\"");
        menuSource.ShouldContain("l[\"Reports:SalesRevenue\"]");
        menuSource.ShouldContain("VPureLuxMenus.ReportsSalesProfit");
        menuSource.ShouldContain("\"~/Reports/SalesProfit\"");
        menuSource.ShouldContain("l[\"Reports:SalesProfit\"]");
        menuSource.ShouldContain("RequirePermissions(VPureLuxPermissions.Reports.Sales.View)");
        menuSource.ShouldContain("RequirePermissions(VPureLuxPermissions.Reports.Profit.View)");
        menuSource.ShouldNotContain("RequirePermissions(VPureLuxPermissions.Reports.Sales.View, VPureLuxPermissions.Reports.Profit.View)");
        menuSource.ShouldNotContain("ReportsSalesRevenue,\r\n            l[\"Reports:SalesRevenue\"],\r\n            \"~/Reports/SalesRevenue\",\r\n            icon: \"fa fa-line-chart\"\r\n        ).RequirePermissions(VPureLuxPermissions.Sales.View)");
    }

    [Fact]
    public async Task SalesRevenue_Page_Should_Require_Report_Sales_View_Permission()
    {
        var pageModelSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Reports/SalesRevenue.cshtml.cs"));
        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Reports/SalesRevenue.cshtml"));

        pageModelSource.ShouldContain("[Authorize(VPureLuxPermissions.Reports.Sales.View)]");
        pageModelSource.ShouldNotContain("VPureLuxPermissions.Reports.Profit.View");
        pageModelSource.ShouldNotContain("VPureLuxPermissions.Sales.View");
        pageSource.ShouldContain("@page");
    }

    [Fact]
    public async Task SalesProfit_Page_Should_Require_Report_Profit_View_Permission()
    {
        var pageModelSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Reports/SalesProfit.cshtml.cs"));
        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Reports/SalesProfit.cshtml"));

        pageModelSource.ShouldContain("[Authorize(VPureLuxPermissions.Reports.Profit.View)]");
        pageModelSource.ShouldNotContain("VPureLuxPermissions.Reports.Sales.View");
        pageModelSource.ShouldNotContain("VPureLuxPermissions.Sales.View");
        pageSource.ShouldContain("@page");
    }

    [Fact]
    public async Task SalesRevenue_PageModel_Should_Default_Current_Month_And_Call_Report_Service()
    {
        SalesRevenueReportInput? captured = null;
        var reports = Substitute.For<ISalesReportsAppService>();
        reports.GetSalesRevenueAsync(Arg.Do<SalesRevenueReportInput>(input => captured = input))
            .Returns(new SalesRevenueReportDto());
        var model = CreateRevenueModel(reports);
        SetPageContext(model);

        await model.OnGetAsync();

        captured.ShouldNotBeNull();
        var expectedFromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        captured!.FromDate.ShouldBe(expectedFromDate);
        captured.ToDate.ShouldBe(expectedFromDate.AddMonths(1).AddDays(-1));
        captured.GroupBy.ShouldBe(ReportPeriodGroup.Day);
        captured.ProductId.ShouldBeNull();
        captured.CustomerId.ShouldBeNull();
        captured.WarehouseId.ShouldBeNull();
        captured.PaymentStatus.ShouldBeNull();
    }

    [Fact]
    public async Task SalesRevenue_PageModel_Should_Preserve_Filter_Values_And_Pass_Input()
    {
        SalesRevenueReportInput? captured = null;
        var reports = Substitute.For<ISalesReportsAppService>();
        reports.GetSalesRevenueAsync(Arg.Do<SalesRevenueReportInput>(input => captured = input))
            .Returns(new SalesRevenueReportDto());
        var productId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var model = CreateRevenueModel(reports, productId, customerId, warehouseId);
        SetPageContext(model);
        model.FromDate = new DateTime(2026, 8, 1);
        model.ToDate = new DateTime(2026, 8, 31);
        model.GroupBy = ReportPeriodGroup.Month;
        model.ProductId = productId;
        model.CustomerId = customerId;
        model.WarehouseId = warehouseId;
        model.PaymentStatus = SalesOrderReceivableStatus.PartiallyPaid;

        await model.OnGetAsync();

        captured.ShouldNotBeNull();
        captured!.FromDate.ShouldBe(model.FromDate);
        captured.ToDate.ShouldBe(model.ToDate);
        captured.GroupBy.ShouldBe(ReportPeriodGroup.Month);
        captured.ProductId.ShouldBe(productId);
        captured.CustomerId.ShouldBe(customerId);
        captured.WarehouseId.ShouldBe(warehouseId);
        captured.PaymentStatus.ShouldBe(SalesOrderReceivableStatus.PartiallyPaid);
        model.ProductOptions.Single().Selected.ShouldBeFalse();
        model.ProductOptions.Single().Value.ShouldBe(productId.ToString());
    }

    [Fact]
    public async Task SalesRevenue_PageModel_Should_Parse_Us_And_Iso_Query_Date_Inputs()
    {
        SalesRevenueReportInput? captured = null;
        var reports = Substitute.For<ISalesReportsAppService>();
        reports.GetSalesRevenueAsync(Arg.Do<SalesRevenueReportInput>(input => captured = input))
            .Returns(new SalesRevenueReportDto());
        var model = CreateRevenueModel(reports);
        SetPageContext(model);
        model.FromDateInput = "08/01/2026";
        model.ToDateInput = "08/31/2026";

        await model.OnGetAsync();

        model.ModelState.IsValid.ShouldBeTrue();
        captured.ShouldNotBeNull();
        captured!.FromDate.ShouldBe(new DateTime(2026, 8, 1));
        captured.ToDate.ShouldBe(new DateTime(2026, 8, 31));
        model.FromDateInput.ShouldBe("2026-08-01");
        model.ToDateInput.ShouldBe("2026-08-31");
    }

    [Fact]
    public async Task SalesRevenue_PageModel_Should_Parse_Vietnamese_Query_Date_Inputs()
    {
        SalesRevenueReportInput? captured = null;
        var reports = Substitute.For<ISalesReportsAppService>();
        reports.GetSalesRevenueAsync(Arg.Do<SalesRevenueReportInput>(input => captured = input))
            .Returns(new SalesRevenueReportDto());
        var model = CreateRevenueModel(reports);
        SetPageContext(model);
        model.FromDateInput = "1/8/2026";
        model.ToDateInput = "31/8/2026";

        await model.OnGetAsync();

        model.ModelState.IsValid.ShouldBeTrue();
        captured.ShouldNotBeNull();
        captured!.FromDate.ShouldBe(new DateTime(2026, 8, 1));
        captured.ToDate.ShouldBe(new DateTime(2026, 8, 31));
        model.FromDateInput.ShouldBe("2026-08-01");
        model.ToDateInput.ShouldBe("2026-08-31");
    }

    [Fact]
    public async Task SalesRevenue_PageModel_Should_Show_Friendly_Validation_For_Invalid_Date_Range()
    {
        var reports = Substitute.For<ISalesReportsAppService>();
        var model = CreateRevenueModel(reports);
        SetPageContext(model);
        model.FromDate = new DateTime(2026, 8, 31);
        model.ToDate = new DateTime(2026, 8, 1);

        await model.OnGetAsync();

        model.ModelState.IsValid.ShouldBeFalse();
        model.ModelState[string.Empty]!.Errors.Single().ErrorMessage.ShouldContain("Khoảng thời gian báo cáo không hợp lệ");
        await reports.DidNotReceive().GetSalesRevenueAsync(Arg.Any<SalesRevenueReportInput>());
    }

    [Fact]
    public async Task SalesRevenue_Page_Should_Render_Kpis_Tables_Order_Link_And_Statuses()
    {
        var context = await CreateSalesContextAsync("RPT-REV");
        var sales = GetRequiredService<ISalesOrderAppService>();
        var order = await sales.CreateAsync(new CreateSalesOrderDto
        {
            CustomerId = context.CustomerId,
            WarehouseId = context.WarehouseId,
            Lines = [new CreateSalesOrderLineDto { ProductId = context.ProductId, Quantity = 2, ActualSellingPrice = 100 }]
        });
        await sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });
        await sales.AddPaymentAsync(order.Id, new CreateSalesOrderPaymentDto
        {
            Amount = 50,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = SalesPaymentMethod.Cash,
            ReferenceNo = "RPT-PAY",
            IdempotencyKey = Guid.NewGuid().ToString("N")
        });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync(
            $"/Reports/SalesRevenue?FromDate={DateTime.Today.AddDays(-1):yyyy-MM-dd}&ToDate={DateTime.Today.AddDays(1):yyyy-MM-dd}&GroupBy=Day"));

        html.ShouldContain("Báo cáo doanh số bán hàng");
        html.ShouldContain("Tổng doanh số");
        html.ShouldContain("Số đơn đã xác nhận");
        html.ShouldContain("Số lượng sản phẩm bán");
        html.ShouldContain("Giá trị đơn trung bình");
        html.ShouldContain("Đã thanh toán");
        html.ShouldContain("Còn nợ");
        html.ShouldContain("Doanh số theo thời gian");
        html.ShouldContain("Top sản phẩm theo doanh số");
        html.ShouldContain("Doanh số theo khách hàng");
        html.ShouldContain("Danh sách đơn hàng");
        html.ShouldContain(order.OrderNo);
        html.ShouldContain($"/Sales/Details/{order.Id}");
        html.ShouldContain("Thanh toán một phần");
        html.ShouldContain(context.ProductCode);
        html.ShouldContain(context.CustomerCode);
    }

    [Fact]
    public async Task SalesRevenue_Page_Should_Render_Empty_State()
    {
        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync(
            "/Reports/SalesRevenue?FromDate=1999-01-01&ToDate=1999-01-31"));

        html.ShouldContain("Không có dữ liệu doanh số trong khoảng thời gian đã chọn.");
        html.ShouldContain("name=\"FromDate\"");
        html.ShouldContain("name=\"ToDate\"");
        html.ShouldNotContain("Danh sách đơn hàng</h5>");
    }

    [Fact]
    public async Task SalesRevenue_Razor_Page_Should_Use_Native_Selects_And_No_Profit_UI()
    {
        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Reports/SalesRevenue.cshtml"));

        pageSource.ShouldContain("asp-for=\"ProductId\"");
        pageSource.ShouldContain("asp-for=\"CustomerId\"");
        pageSource.ShouldContain("asp-for=\"WarehouseId\"");
        pageSource.ShouldContain("asp-for=\"PaymentStatus\"");
        pageSource.IndexOf("select2", StringComparison.OrdinalIgnoreCase).ShouldBe(-1);
        pageSource.ShouldNotContain("data-vpl-select2");
        pageSource.ShouldNotContain("Reports:SalesProfit");
        pageSource.ShouldNotContain("Profit");
    }

    [Fact]
    public async Task SalesProfit_PageModel_Should_Default_Current_Month_And_Call_Report_Service()
    {
        SalesProfitReportInput? captured = null;
        var reports = Substitute.For<ISalesReportsAppService>();
        reports.GetSalesProfitAsync(Arg.Do<SalesProfitReportInput>(input => captured = input))
            .Returns(new SalesProfitReportDto());
        var model = CreateProfitModel(reports);
        SetPageContext(model);

        await model.OnGetAsync();

        captured.ShouldNotBeNull();
        var expectedFromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        captured!.FromDate.ShouldBe(expectedFromDate);
        captured.ToDate.ShouldBe(expectedFromDate.AddMonths(1).AddDays(-1));
        captured.GroupBy.ShouldBe(ReportPeriodGroup.Day);
        captured.LossOnly.ShouldBeFalse();
        captured.MissingCostOnly.ShouldBeFalse();
    }

    [Fact]
    public async Task SalesProfit_PageModel_Should_Preserve_Filter_Values_And_Pass_Input()
    {
        SalesProfitReportInput? captured = null;
        var reports = Substitute.For<ISalesReportsAppService>();
        reports.GetSalesProfitAsync(Arg.Do<SalesProfitReportInput>(input => captured = input))
            .Returns(new SalesProfitReportDto());
        var productId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var model = CreateProfitModel(reports, productId, customerId, warehouseId);
        SetPageContext(model);
        model.FromDate = new DateTime(2026, 8, 1);
        model.ToDate = new DateTime(2026, 8, 31);
        model.GroupBy = ReportPeriodGroup.Month;
        model.ProductId = productId;
        model.CustomerId = customerId;
        model.WarehouseId = warehouseId;
        model.LossOnly = true;
        model.MissingCostOnly = true;

        await model.OnGetAsync();

        captured.ShouldNotBeNull();
        captured!.FromDate.ShouldBe(model.FromDate);
        captured.ToDate.ShouldBe(model.ToDate);
        captured.GroupBy.ShouldBe(ReportPeriodGroup.Month);
        captured.ProductId.ShouldBe(productId);
        captured.CustomerId.ShouldBe(customerId);
        captured.WarehouseId.ShouldBe(warehouseId);
        captured.LossOnly.ShouldBeTrue();
        captured.MissingCostOnly.ShouldBeTrue();
    }

    [Fact]
    public async Task SalesProfit_PageModel_Should_Parse_Vietnamese_Query_Date_Inputs()
    {
        SalesProfitReportInput? captured = null;
        var reports = Substitute.For<ISalesReportsAppService>();
        reports.GetSalesProfitAsync(Arg.Do<SalesProfitReportInput>(input => captured = input))
            .Returns(new SalesProfitReportDto());
        var model = CreateProfitModel(reports);
        SetPageContext(model);
        model.FromDateInput = "01/08/2026";
        model.ToDateInput = "31/08/2026";

        await model.OnGetAsync();

        model.ModelState.IsValid.ShouldBeTrue();
        captured.ShouldNotBeNull();
        captured!.FromDate.ShouldBe(new DateTime(2026, 8, 1));
        captured.ToDate.ShouldBe(new DateTime(2026, 8, 31));
        model.FromDateInput.ShouldBe("2026-08-01");
        model.ToDateInput.ShouldBe("2026-08-31");
    }

    [Fact]
    public async Task SalesProfit_PageModel_Should_Show_Friendly_Validation_For_Invalid_Date_Range()
    {
        var reports = Substitute.For<ISalesReportsAppService>();
        var model = CreateProfitModel(reports);
        SetPageContext(model);
        model.FromDate = new DateTime(2026, 8, 31);
        model.ToDate = new DateTime(2026, 8, 1);

        await model.OnGetAsync();

        model.ModelState.IsValid.ShouldBeFalse();
        model.ModelState[string.Empty]!.Errors.Single().ErrorMessage.ShouldContain("Khoảng thời gian báo cáo không hợp lệ");
        await reports.DidNotReceive().GetSalesProfitAsync(Arg.Any<SalesProfitReportInput>());
    }

    [Fact]
    public async Task SalesProfit_Page_Should_Render_Kpis_Tables_Order_Link_Negative_Profit_And_Empty_State()
    {
        var context = await CreateSalesContextAsync("RPT-PFT");
        var sales = GetRequiredService<ISalesOrderAppService>();
        var order = await sales.CreateAsync(new CreateSalesOrderDto
        {
            CustomerId = context.CustomerId,
            WarehouseId = context.WarehouseId,
            Lines = [new CreateSalesOrderLineDto { ProductId = context.ProductId, Quantity = 1, ActualSellingPrice = 5 }]
        });
        await sales.ConfirmAsync(order.Id, new ConfirmSalesOrderDto { IdempotencyKey = Guid.NewGuid().ToString("N") });

        var html = WebUtility.HtmlDecode(await GetResponseAsStringAsync(
            $"/Reports/SalesProfit?FromDate={DateTime.Today.AddDays(-1):yyyy-MM-dd}&ToDate={DateTime.Today.AddDays(1):yyyy-MM-dd}&GroupBy=Day&LossOnly=true"));

        html.ShouldContain("Báo cáo lợi nhuận bán hàng");
        html.ShouldContain("Doanh số");
        html.ShouldContain("Giá vốn");
        html.ShouldContain("Lợi nhuận");
        html.ShouldContain("Tỷ suất lợi nhuận");
        html.ShouldContain("Dòng chưa có giá vốn");
        html.ShouldContain("Lợi nhuận theo thời gian");
        html.ShouldContain("Lợi nhuận theo sản phẩm");
        html.ShouldContain("Lợi nhuận theo khách hàng");
        html.ShouldContain("Chi tiết dòng bán hàng");
        html.ShouldContain(order.OrderNo);
        html.ShouldContain($"/Sales/Details/{order.Id}");
        html.ShouldContain("text-danger");
        html.ShouldContain(context.ProductCode);

        var emptyHtml = WebUtility.HtmlDecode(await GetResponseAsStringAsync(
            "/Reports/SalesProfit?FromDate=1999-01-01&ToDate=1999-01-31"));
        emptyHtml.ShouldContain("Không có dữ liệu lợi nhuận trong khoảng thời gian đã chọn.");
    }

    [Fact]
    public async Task SalesProfit_Razor_Page_Should_Render_Missing_Cost_As_Explicit_Text()
    {
        var pageSource = await File.ReadAllTextAsync(GetRepoFilePath("src/VPureLux.Web/Pages/Reports/SalesProfit.cshtml"));

        pageSource.ShouldContain("Reports:MissingCost");
        pageSource.ShouldContain("Reports:MissingCostNote");
        pageSource.ShouldContain("row.MissingCost");
        pageSource.ShouldContain("—");
        pageSource.IndexOf("select2", StringComparison.OrdinalIgnoreCase).ShouldBe(-1);
    }

    [Fact]
    public async Task Revenue_And_Profit_Export_Buttons_Should_Be_Permission_Aware()
    {
        var reports = Substitute.For<ISalesReportsAppService>();
        reports.GetSalesRevenueAsync(Arg.Any<SalesRevenueReportInput>()).Returns(new SalesRevenueReportDto());
        reports.GetSalesProfitAsync(Arg.Any<SalesProfitReportInput>()).Returns(new SalesProfitReportDto());

        var revenue = CreateRevenueModel(reports, authorization: Authorization(false));
        SetPageContext(revenue);
        await revenue.OnGetAsync();
        revenue.CanExport.ShouldBeFalse();

        var profit = CreateProfitModel(reports, authorization: Authorization(false));
        SetPageContext(profit);
        await profit.OnGetAsync();
        profit.CanExport.ShouldBeFalse();

        revenue = CreateRevenueModel(reports, authorization: Authorization(true));
        SetPageContext(revenue);
        await revenue.OnGetAsync();
        revenue.CanExport.ShouldBeTrue();
    }

    [Fact]
    public async Task Export_Handlers_Should_Require_Export_Permission()
    {
        var reports = Substitute.For<ISalesReportsAppService>();
        var revenue = CreateRevenueModel(reports, authorization: Authorization(false));
        SetPageContext(revenue);
        (await revenue.OnGetExportAsync()).ShouldBeOfType<ForbidResult>();

        var profit = CreateProfitModel(reports, authorization: Authorization(false));
        SetPageContext(profit);
        (await profit.OnGetExportAsync()).ShouldBeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Revenue_Export_Should_Respect_Filters_And_Write_Utf8Bom_Csv()
    {
        SalesRevenueReportInput? captured = null;
        var reports = Substitute.For<ISalesReportsAppService>();
        reports.GetSalesRevenueAsync(Arg.Do<SalesRevenueReportInput>(input => captured = input))
            .Returns(RevenueReportWithCsvCharacters());
        var model = CreateRevenueModel(reports, authorization: Authorization(true));
        SetPageContext(model);
        model.FromDate = new DateTime(2026, 8, 1);
        model.ToDate = new DateTime(2026, 8, 31);
        model.GroupBy = ReportPeriodGroup.Month;
        model.PaymentStatus = SalesOrderReceivableStatus.PartiallyPaid;

        var result = (FileContentResult)await model.OnGetExportAsync();

        captured.ShouldNotBeNull();
        captured!.PaymentStatus.ShouldBe(SalesOrderReceivableStatus.PartiallyPaid);
        result.ContentType.ShouldBe("text/csv; charset=utf-8");
        result.FileDownloadName.ShouldStartWith("bao-cao-doanh-so-ban-hang-");
        result.FileContents.Take(3).ToArray().ShouldBe([0xEF, 0xBB, 0xBF]);
        var csv = System.Text.Encoding.UTF8.GetString(result.FileContents);
        csv.ShouldContain("Tổng quan");
        csv.ShouldContain("Doanh số theo thời gian");
        csv.ShouldContain("Danh sách đơn hàng");
        csv.ShouldContain("\"SP,001\"");
        csv.ShouldContain("\"Khách \"\"VIP\"\"\"");
        csv.ShouldContain("Thanh toán một phần");
    }

    [Fact]
    public async Task Profit_Export_Should_Respect_Filters_And_Render_Missing_Cost()
    {
        SalesProfitReportInput? captured = null;
        var reports = Substitute.For<ISalesReportsAppService>();
        reports.GetSalesProfitAsync(Arg.Do<SalesProfitReportInput>(input => captured = input))
            .Returns(ProfitReportWithMissingCost());
        var model = CreateProfitModel(reports, authorization: Authorization(true));
        SetPageContext(model);
        model.FromDate = new DateTime(2026, 8, 1);
        model.ToDate = new DateTime(2026, 8, 31);
        model.GroupBy = ReportPeriodGroup.Month;
        model.LossOnly = true;
        model.MissingCostOnly = true;

        var result = (FileContentResult)await model.OnGetExportAsync();

        captured.ShouldNotBeNull();
        captured!.LossOnly.ShouldBeTrue();
        captured.MissingCostOnly.ShouldBeTrue();
        result.ContentType.ShouldBe("text/csv; charset=utf-8");
        result.FileDownloadName.ShouldStartWith("bao-cao-loi-nhuan-ban-hang-");
        result.FileContents.Take(3).ToArray().ShouldBe([0xEF, 0xBB, 0xBF]);
        var csv = System.Text.Encoding.UTF8.GetString(result.FileContents);
        csv.ShouldContain("Lợi nhuận theo thời gian");
        csv.ShouldContain("Chi tiết dòng bán hàng");
        csv.ShouldContain("Chưa có giá vốn");
        csv.ShouldContain("Thiếu giá vốn");
    }

    private SalesRevenueModel CreateRevenueModel(
        ISalesReportsAppService reports,
        Guid? productId = null,
        Guid? customerId = null,
        Guid? warehouseId = null,
        IAuthorizationService? authorization = null)
    {
        var products = Substitute.For<IProductAppService>();
        products.GetListAsync(Arg.Any<GetProductListInput>())
            .Returns(new PagedResultDto<ProductDto>(
                productId.HasValue ? 1 : 0,
                productId.HasValue
                    ? [new ProductDto { Id = productId.Value, Code = "P-RPT", Name = "Report Product" }]
                    : []));
        var customers = Substitute.For<ICustomerAppService>();
        customers.GetListAsync(Arg.Any<GetCustomerListInput>())
            .Returns(new PagedResultDto<CustomerDto>(
                customerId.HasValue ? 1 : 0,
                customerId.HasValue
                    ? [new CustomerDto { Id = customerId.Value, Code = "C-RPT", Name = "Report Customer" }]
                    : []));
        var warehouses = Substitute.For<IWarehouseAppService>();
        warehouses.GetListAsync(Arg.Any<GetInventoryListInput>())
            .Returns(new PagedResultDto<WarehouseDto>(
                warehouseId.HasValue ? 1 : 0,
                warehouseId.HasValue
                    ? [new WarehouseDto { Id = warehouseId.Value, Code = "W-RPT", Name = "Report Warehouse" }]
                    : []));

        return new SalesRevenueModel(reports, products, customers, warehouses, authorization ?? Authorization(false));
    }

    private SalesProfitModel CreateProfitModel(
        ISalesReportsAppService reports,
        Guid? productId = null,
        Guid? customerId = null,
        Guid? warehouseId = null,
        IAuthorizationService? authorization = null)
    {
        var products = Substitute.For<IProductAppService>();
        products.GetListAsync(Arg.Any<GetProductListInput>())
            .Returns(new PagedResultDto<ProductDto>(
                productId.HasValue ? 1 : 0,
                productId.HasValue
                    ? [new ProductDto { Id = productId.Value, Code = "P-RPT", Name = "Report Product" }]
                    : []));
        var customers = Substitute.For<ICustomerAppService>();
        customers.GetListAsync(Arg.Any<GetCustomerListInput>())
            .Returns(new PagedResultDto<CustomerDto>(
                customerId.HasValue ? 1 : 0,
                customerId.HasValue
                    ? [new CustomerDto { Id = customerId.Value, Code = "C-RPT", Name = "Report Customer" }]
                    : []));
        var warehouses = Substitute.For<IWarehouseAppService>();
        warehouses.GetListAsync(Arg.Any<GetInventoryListInput>())
            .Returns(new PagedResultDto<WarehouseDto>(
                warehouseId.HasValue ? 1 : 0,
                warehouseId.HasValue
                    ? [new WarehouseDto { Id = warehouseId.Value, Code = "W-RPT", Name = "Report Warehouse" }]
                    : []));

        return new SalesProfitModel(reports, products, customers, warehouses, authorization ?? Authorization(false));
    }

    private static IAuthorizationService Authorization(bool succeed)
    {
        var authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(succeed ? AuthorizationResult.Success() : AuthorizationResult.Failed());
        return authorization;
    }

    private static SalesRevenueReportDto RevenueReportWithCsvCharacters() => new()
    {
        Summary = new SalesRevenueSummaryDto
        {
            TotalRevenue = 100,
            ConfirmedOrderCount = 1,
            TotalQuantity = 1,
            AverageOrderValue = 100,
            PaidAmount = 50,
            RemainingAmount = 50,
            PartiallyPaidOrderCount = 1
        },
        ByPeriod =
        [
            new SalesRevenuePeriodRowDto
            {
                PeriodKey = "2026-08",
                PeriodLabel = "2026-08",
                OrderCount = 1,
                Quantity = 1,
                Revenue = 100,
                PaidAmount = 50,
                RemainingAmount = 50
            }
        ],
        ByProduct =
        [
            new SalesRevenueProductRowDto
            {
                ProductId = Guid.NewGuid(),
                ProductCode = "SP,001",
                ProductName = "Máy lọc\nnước",
                Quantity = 1,
                OrderCount = 1,
                Revenue = 100,
                RevenueSharePercent = 100
            }
        ],
        ByCustomer =
        [
            new SalesRevenueCustomerRowDto
            {
                CustomerId = Guid.NewGuid(),
                CustomerCode = "KH001",
                CustomerName = "Khách \"VIP\"",
                OrderCount = 1,
                Revenue = 100,
                PaidAmount = 50,
                RemainingAmount = 50
            }
        ],
        Orders =
        [
            new SalesRevenueOrderRowDto
            {
                SalesOrderId = Guid.NewGuid(),
                OrderNo = "SO-CSV-001",
                ConfirmationTime = new DateTime(2026, 8, 4, 10, 30, 0),
                CustomerId = Guid.NewGuid(),
                CustomerCode = "KH001",
                CustomerName = "Khách \"VIP\"",
                WarehouseId = Guid.NewGuid(),
                WarehouseCode = "KHO",
                WarehouseName = "Kho chính",
                TotalAmount = 100,
                PaidAmount = 50,
                RemainingAmount = 50,
                PaymentStatus = SalesOrderReceivableStatus.PartiallyPaid
            }
        ]
    };

    private static SalesProfitReportDto ProfitReportWithMissingCost() => new()
    {
        Summary = new SalesProfitSummaryDto
        {
            Revenue = 100,
            CostAmount = 0,
            ProfitAmount = 0,
            ProfitMarginPercent = 0,
            ConfirmedOrderCount = 1,
            MissingCostLineCount = 1
        },
        ByPeriod =
        [
            new SalesProfitPeriodRowDto
            {
                PeriodKey = "2026-08",
                PeriodLabel = "2026-08",
                Revenue = 100,
                CostAmount = 0,
                ProfitAmount = 0,
                ProfitMarginPercent = 0,
                OrderCount = 1
            }
        ],
        ByProduct =
        [
            new SalesProfitProductRowDto
            {
                ProductId = Guid.NewGuid(),
                ProductCode = "SP001",
                ProductName = "Sản phẩm",
                Quantity = 1,
                Revenue = 100,
                CostAmount = 0,
                ProfitAmount = 0,
                ProfitMarginPercent = 0
            }
        ],
        ByCustomer =
        [
            new SalesProfitCustomerRowDto
            {
                CustomerId = Guid.NewGuid(),
                CustomerCode = "KH001",
                CustomerName = "Khách hàng",
                OrderCount = 1,
                Revenue = 100,
                CostAmount = 0,
                ProfitAmount = 0,
                ProfitMarginPercent = 0,
                RemainingAmount = 100
            }
        ],
        Lines =
        [
            new SalesProfitLineRowDto
            {
                SalesOrderId = Guid.NewGuid(),
                OrderNo = "SO-MISSING-001",
                ConfirmationTime = new DateTime(2026, 8, 4, 10, 30, 0),
                CustomerId = Guid.NewGuid(),
                CustomerCode = "KH001",
                CustomerName = "Khách hàng",
                ProductId = Guid.NewGuid(),
                ProductCode = "SP001",
                ProductName = "Sản phẩm",
                Quantity = 1,
                UnitPrice = 100,
                Revenue = 100,
                MissingCost = true
            }
        ]
    };

    private async Task<(Guid CustomerId, Guid WarehouseId, Guid ProductId, string CustomerCode, string ProductCode)> CreateSalesContextAsync(string prefix)
    {
        var group = await GetRequiredService<ICustomerGroupAppService>()
            .CreateAsync(new CreateCustomerGroupDto { Code = Unique(prefix + "-G"), Name = "Report Group" });
        var customer = await GetRequiredService<ICustomerAppService>()
            .CreateAsync(new CreateCustomerDto { Code = Unique(prefix + "-C"), Name = "Report Customer", CustomerGroupId = group.Id });
        var warehouse = await GetRequiredService<IWarehouseAppService>()
            .CreateAsync(new CreateWarehouseDto { Code = Unique(prefix + "-W"), Name = "Report Warehouse" });
        var component = await GetRequiredService<IComponentAppService>()
            .CreateAsync(new CreateComponentDto { Code = Unique(prefix + "-M"), Name = "Report Material", Unit = "Piece" });
        var stockItem = (await GetRequiredService<IStockItemRepository>()
            .FindByCatalogItemAsync(StockItemType.Component, component.Id))!;
        await GetRequiredService<IInventoryTransactionAppService>().PostReceiptAsync(new PostReceiptDto
        {
            WarehouseId = warehouse.Id,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Lines =
            [
                new ReceiptLineInput
                {
                    StockItemId = stockItem.Id,
                    Quantity = 10,
                    UnitCost = 10,
                    LotNo = Unique(prefix + "-L"),
                    ReceivedAt = DateTime.UtcNow
                }
            ]
        });
        var product = await GetRequiredService<IProductAppService>()
            .CreateAsync(new CreateProductDto { Code = Unique(prefix + "-P"), Name = "Report Product" });
        var bom = await GetRequiredService<IBomAppService>().CreateAsync(product.Id, new CreateBomVersionDto
        {
            EffectiveFrom = DateTime.Today,
            Items = [new CreateBomItemDto { ComponentId = component.Id, Quantity = 1 }]
        });
        await GetRequiredService<IBomAppService>().PublishAsync(bom.Id);
        return (customer.Id, warehouse.Id, product.Id, customer.Code, product.Code);
    }

    private void SetPageContext(PageModel model)
    {
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity()),
                RequestServices = GetRequiredService<IServiceProvider>()
            }
        };

        if (model is global::VPureLux.Web.Pages.VPureLuxPageModel vplModel)
        {
            vplModel.LazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();
        }
    }

    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];

    private static string GetRepoFilePath(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "VPureLux.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull();
        return Path.Combine(directory.FullName, relativePath);
    }
}
