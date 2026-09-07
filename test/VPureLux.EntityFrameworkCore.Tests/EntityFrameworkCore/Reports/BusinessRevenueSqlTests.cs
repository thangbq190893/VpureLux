using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using VPureLux.Reports;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Reports;

public class BusinessRevenueSqlTests
{
    [Theory]
    [InlineData(null)] [InlineData(BusinessRevenueSource.Sales)] [InlineData(BusinessRevenueSource.Service)]
    public void S005_queries_translate_to_SQL_Server_without_a_connection(BusinessRevenueSource? source)
    {
        using var db = new VPureLuxDbContext(new DbContextOptionsBuilder<VPureLuxDbContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=S005_OFFLINE_ONLY;User Id=unused;Password=unused;Connect Timeout=1").Options);
        var query = (IQueryable<BusinessRevenueRowDto>)typeof(EfCoreBusinessRevenueReadRepository)
            .GetMethod("BuildQuery", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null,
                [db, new GetBusinessRevenueListInput { Source = source, FromDate = new DateTime(2026, 9, 7), SearchText = "filter" }, new DateTime(2026, 9, 8)])!;
        query = query.IgnoreQueryFilters();
        var sql = query.OrderBy(x => x.DocumentDate).ThenBy(x => x.Source).ThenBy(x => x.DocumentId).Skip(10).Take(10).ToQueryString();
        sql.ShouldContain("OFFSET"); sql.ShouldContain("FETCH NEXT"); sql.ShouldContain("GROUP BY");
        if (source == null) sql.ShouldContain("UNION ALL");
        if (source != BusinessRevenueSource.Service) { sql.ShouldContain("IsEffective"); sql.ShouldContain("AppSalesOrderRefunds"); }
        if (source != BusinessRevenueSource.Sales) sql.ShouldContain("AppServiceRefunds");
        var summary = (IQueryable<BusinessRevenueSummaryDto>)typeof(EfCoreBusinessRevenueReadRepository)
            .GetMethod("Totals", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [query])!;
        summary.ToQueryString().ShouldContain("SUM(");
        sql.ShouldNotContain("AppServiceWorks"); sql.ShouldNotContain("AppComponents"); sql.ShouldNotContain("AppBom");
    }
}
