using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VPureLux.EntityFrameworkCore;
using VPureLux.Reports;
using VPureLux.Service;
using VPureLux.Warranty;

internal static class LiveReadChecks
{
    public static async Task<int> RunAsync(string directory)
    {
        if (Directory.Exists(directory)) throw new InvalidOperationException("New evidence directory required");
        var b = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("S006_SQL_CONNECTION") ?? throw new ArgumentException("Explicit connection required"));
        if (b.InitialCatalog != "VPL") throw new InvalidOperationException("VPL only before connection");
        await using var sql = new SqlConnection(b.ConnectionString);
        await sql.OpenAsync();
        await using var proof = sql.CreateCommand(); proof.CommandText = "SELECT DB_NAME()";
        if ((string?)await proof.ExecuteScalarAsync() != "VPL") throw new InvalidOperationException("Target mismatch");
        Directory.CreateDirectory(directory);
        await using var db = new VPureLuxDbContext(new DbContextOptionsBuilder<VPureLuxDbContext>().UseSqlServer(sql).Options);
        var fixture = await db.ServiceOrders.IgnoreQueryFilters().Where(x => x.CustomerCodeSnapshot.StartsWith("UATSVC_20260907")).FirstAsync();
        var positions = await db.CustomerAssetComponents.IgnoreQueryFilters().Where(x => x.CustomerAssetId == fixture.CustomerAssetId).Select(x => x.Id).Take(3).ToArrayAsync();
        // Mirrors GetOrderListAsync projection; reports and money invoke their actual query builders.
        var list = db.ServiceOrders.IgnoreQueryFilters().Where(x => !x.IsDeleted).OrderByDescending(x => x.OrderDate).ThenBy(x => x.Id).Skip(10).Take(10)
            .Select(x => new { x.Id, x.OrderNo, x.OrderDate, x.ScheduledAt, x.Status, x.CustomerId, x.CustomerCodeSnapshot, x.CustomerNameSnapshot,
                x.CustomerAssetId, x.AssetNoSnapshot, x.AssetNameSnapshot, PlannedTotal = x.Lines.Sum(l => l.UnitPrice * l.PlannedQuantity), LineCount = x.Lines.Count });
        IQueryable<BusinessRevenueRowDto> Report(BusinessRevenueSource? source) => ((IQueryable<BusinessRevenueRowDto>)typeof(EfCoreBusinessRevenueReadRepository)
            .GetMethod("BuildQuery", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null,
                [db, new GetBusinessRevenueListInput { FromDate = new DateTime(2026, 1, 1), Source = source }, new DateTime(2027, 1, 1)])!).IgnoreQueryFilters();
        var service = Report(BusinessRevenueSource.Service).OrderByDescending(x => x.DocumentDate).ThenBy(x => x.DocumentId).Take(10);
        var consolidated = Report(null).OrderByDescending(x => x.DocumentDate).ThenBy(x => x.Source).ThenBy(x => x.DocumentId).Take(10);
        // Same set-based pending-reminder predicate used by CustomerCareServiceCompletion, no per-position queries.
        var care = db.AssetReplacementReminders.IgnoreQueryFilters().Where(x => !x.IsDeleted && x.CustomerAssetId == fixture.CustomerAssetId &&
            x.CustomerAssetComponentId.HasValue && positions.Contains(x.CustomerAssetComponentId.Value) && x.Status == AssetReplacementReminderStatus.Pending);
        var money = (IQueryable<ServiceMoneySummary>)typeof(EfCoreServiceMoneyReadRepository).GetMethod("SummaryQuery", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [db, db.ServiceOrders.IgnoreQueryFilters().Where(x => !x.IsDeleted && x.CustomerId == fixture.CustomerId)])!;
        var customer = money.IgnoreQueryFilters().GroupBy(x => x.CustomerId).Select(g => new { Receivable = g.Sum(x => x.Receivable), AdvancePaid = g.Sum(x => x.AdvancePaid),
            CustomerCredit = g.Sum(x => x.CustomerCredit), RefundDue = g.Sum(x => x.RefundDue), Inconsistent = g.LongCount(x => x.HasInconsistentLedger) });
        var queries = new Dictionary<string, string> { ["service-list"] = list.ToQueryString(), ["service-report"] = service.ToQueryString(),
            ["consolidated-report"] = consolidated.ToQueryString(), ["care-successors"] = care.ToQueryString(), ["customer-money"] = customer.ToQueryString() };
        var results = new List<object>();
        foreach (var (name, query) in queries)
        {
            File.WriteAllText(Path.Combine(directory, name + ".sql"), query);
            var messages = new List<string>();
            SqlInfoMessageEventHandler handler = (_, e) => messages.Add(e.Message);
            sql.InfoMessage += handler;
            try
            {
                await using var command = sql.CreateCommand(); command.CommandTimeout = 60;
                command.CommandText = "SET STATISTICS IO ON; SET STATISTICS TIME ON; SET STATISTICS XML ON;\n" + query + "\nSET STATISTICS XML OFF; SET STATISTICS IO OFF; SET STATISTICS TIME OFF;";
                var timer = Stopwatch.StartNew(); var rows = 0; var plans = 0;
                await using (var reader = await command.ExecuteReaderAsync())
                {
                    do
                    {
                        var isPlan = reader.FieldCount == 1 && reader.GetName(0).Contains("Showplan", StringComparison.OrdinalIgnoreCase);
                        while (await reader.ReadAsync())
                        {
                            if (isPlan) { File.WriteAllText(Path.Combine(directory, name + "-" + (++plans) + ".sqlplan"), reader.GetString(0)); }
                            else rows++;
                        }
                    } while (await reader.NextResultAsync());
                }
                timer.Stop(); File.WriteAllLines(Path.Combine(directory, name + ".io.txt"), messages);
                if (plans == 0) throw new InvalidOperationException("Actual plan not returned: " + name);
                results.Add(new { Query = name, Rows = rows, Plans = plans, WallMilliseconds = timer.ElapsedMilliseconds });
                Console.WriteLine($"PLAN_PASS {name} rows={rows} ms={timer.ElapsedMilliseconds}");
            }
            finally { sql.InfoMessage -= handler; }
        }
        File.WriteAllText(Path.Combine(directory, "summary.json"), JsonSerializer.Serialize(new { Database = "VPL", Mode = "SELECT + session statistics only",
            FilterNote = "Report/money builders invoked directly with ABP filters disabled for standalone plan probe; HTTP permission/soft-delete behavior verified separately. Small VPL dataset, not a load benchmark.", Results = results }, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
}
