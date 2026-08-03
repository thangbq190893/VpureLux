using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Shouldly;
using VPureLux.Permissions;
using Xunit;

namespace VPureLux.Reports;

public class SalesReportsContractsTests
{
    [Fact]
    public void Should_Require_Separate_Revenue_And_Profit_Report_Permissions()
    {
        Permission(nameof(SalesReportsAppService.GetSalesRevenueAsync))
            .ShouldBe(VPureLuxPermissions.Reports.Sales.View);
        Permission(nameof(SalesReportsAppService.GetSalesProfitAsync))
            .ShouldBe(VPureLuxPermissions.Reports.Profit.View);
        Permission(nameof(SalesReportsAppService.GetSalesProfitAsync))
            .ShouldNotBe(VPureLuxPermissions.Reports.Sales.View);
    }

    [Fact]
    public void Report_Contracts_Should_Use_Expected_Defaults()
    {
        var revenue = new SalesRevenueReportInput();
        revenue.GroupBy.ShouldBe(ReportPeriodGroup.Day);
        revenue.MaxDetailRows.ShouldBe(500);

        var profit = new SalesProfitReportInput();
        profit.GroupBy.ShouldBe(ReportPeriodGroup.Day);
        profit.MaxDetailRows.ShouldBe(500);
        profit.LossOnly.ShouldBeFalse();
        profit.MissingCostOnly.ShouldBeFalse();
    }

    private static string? Permission(string method) =>
        typeof(SalesReportsAppService)
            .GetMethod(method)!
            .GetCustomAttributes<AuthorizeAttribute>()
            .Single()
            .Policy;
}
