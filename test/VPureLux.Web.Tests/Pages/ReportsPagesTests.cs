using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
        menuSource.ShouldContain("RequirePermissions(VPureLuxPermissions.Reports.Sales.View)");
        menuSource.ShouldNotContain("ReportsSalesRevenue,\r\n            l[\"Reports:SalesRevenue\"],\r\n            \"~/Reports/SalesRevenue\",\r\n            icon: \"fa fa-line-chart\"\r\n        ).RequirePermissions(VPureLuxPermissions.Sales.View)");
        menuSource.ShouldNotContain("ReportsProfit");
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
    public async Task SalesRevenue_PageModel_Should_Default_Current_Month_And_Call_Report_Service()
    {
        SalesRevenueReportInput? captured = null;
        var reports = Substitute.For<ISalesReportsAppService>();
        reports.GetSalesRevenueAsync(Arg.Do<SalesRevenueReportInput>(input => captured = input))
            .Returns(new SalesRevenueReportDto());
        var model = CreateModel(reports);
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
        var model = CreateModel(reports, productId, customerId, warehouseId);
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
    public async Task SalesRevenue_PageModel_Should_Show_Friendly_Validation_For_Invalid_Date_Range()
    {
        var reports = Substitute.For<ISalesReportsAppService>();
        var model = CreateModel(reports);
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

    private SalesRevenueModel CreateModel(
        ISalesReportsAppService reports,
        Guid? productId = null,
        Guid? customerId = null,
        Guid? warehouseId = null)
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

        return new SalesRevenueModel(reports, products, customers, warehouses);
    }

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
