using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Shouldly;
using VPureLux.Migrations;
using VPureLux.Service;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Service;

public class ServiceMoneySchemaTests
{
    [Fact]
    public void Money_migration_is_additive_service_only_without_legacy_backfill()
    {
        var migration = new AddServicePaymentSettlement();
        migration.UpOperations.Count.ShouldBe(14);
        migration.UpOperations.OfType<SqlOperation>().ShouldBeEmpty();
        migration.UpOperations.OfType<CreateTableOperation>().Single().Name.ShouldBe("AppServiceRefunds");
        var columns = migration.UpOperations.OfType<AddColumnOperation>().ToList();
        columns.Count.ShouldBe(8);
        foreach (var column in columns)
        {
            column.Table.ShouldBe("AppServicePayments"); column.IsNullable.ShouldBeTrue();
            column.DefaultValue.ShouldBeNull(); column.DefaultValueSql.ShouldBeNull();
        }
        migration.UpOperations.All(x => x is AddColumnOperation or CreateTableOperation or CreateIndexOperation).ShouldBeTrue();
        var before = new AddServiceCompletionFacts().TargetModel;
        foreach (var entity in before.GetEntityTypes().Where(x => x.ClrType != typeof(ServicePayment) && !x.Name.EndsWith("ServicePayment")))
            migration.TargetModel.FindEntityType(entity.Name)!.ToDebugString(MetadataDebugStringOptions.LongDefault)
                .ShouldBe(entity.ToDebugString(MetadataDebugStringOptions.LongDefault), entity.Name);
    }

    [Fact]
    public void Money_customer_projection_translates_on_SQL_Server_without_opening_connection()
    {
        using var db = new VPureLuxDbContext(new DbContextOptionsBuilder<VPureLuxDbContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=S004_OFFLINE_ONLY;User Id=unused;Password=unused;Connect Timeout=1").Options);
        var method = typeof(EfCoreServiceMoneyReadRepository).GetMethod("SummaryQuery", BindingFlags.Static | BindingFlags.NonPublic)!;
        var summary = (IQueryable<ServiceMoneySummary>)method.Invoke(null,
            [db, db.ServiceOrders.IgnoreQueryFilters().Where(x => x.CustomerId == Guid.Empty)])!;
        var sql = summary.IgnoreQueryFilters().GroupBy(x => x.CustomerId).Select(g => new
        {
            Receivable = g.Sum(x => x.Receivable), Advance = g.Sum(x => x.AdvancePaid), Credit = g.Sum(x => x.RefundDue)
        }).ToQueryString();
        sql.ShouldContain("SUM("); sql.ShouldContain("AppServicePayments"); sql.ShouldContain("AppServiceRefunds");
        sql.ShouldNotContain("AppSales"); sql.ShouldContain("GROUP BY");
    }
}
