using System.Reflection;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VPureLux.EntityFrameworkCore;
using VPureLux.Reports;

internal static class LegacyBoundaries
{
    public static async Task<int> RunAsync(string output)
    {
        if (File.Exists(output)) throw new InvalidOperationException("Immutable evidence");
        var b = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("S006_SQL_CONNECTION") ?? throw new ArgumentException("Explicit clone connection required"));
        const string target = "VPL_SERVICE_REHEARSAL_20260907_113345";
        if (b.InitialCatalog != target || Environment.GetEnvironmentVariable("S006_ALLOW_CLONE_FIXTURES") != "1") throw new InvalidOperationException("Only explicit isolated clone fixture opt-in");
        await using var sql = new SqlConnection(b.ConnectionString); await sql.OpenAsync();
        await using var command = sql.CreateCommand(); command.CommandText = "SELECT DB_NAME()";
        if ((string?)await command.ExecuteScalarAsync() != target) throw new InvalidOperationException("Catalog mismatch");
        command.CommandText = "SELECT name FROM sys.columns WHERE object_id=OBJECT_ID('dbo.AppServiceOrders') AND system_type_id<>189 AND is_computed=0 AND is_identity=0 ORDER BY column_id";
        var columns = new List<string>();
        await using (var reader = await command.ExecuteReaderAsync()) while (await reader.ReadAsync()) columns.Add(reader.GetString(0));
        if (columns.Any(x => !System.Text.RegularExpressions.Regex.IsMatch(x, "^[A-Za-z0-9_]+$"))) throw new InvalidOperationException("Unexpected column identifier");
        await using var transaction = (SqlTransaction)await sql.BeginTransactionAsync(); command.Transaction = transaction;
        var evidence = new List<object>();
        try
        {
            foreach (var time in new[] { "00:00", "00:30", "01:30", "06:59", "07:00", "23:59" })
            {
                var id = Guid.NewGuid(); var wall = DateTime.ParseExact("2026-09-06 " + time, "yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                var expressions = columns.Select(x => x switch { "Id" => "@id", "OrderNo" => "@number", "CompletedAt" => "@wall", "CompletionIdempotencyKey" => "@number", "Note" => "N'UATSVC_20260907 isolated synthetic legacy timestamp probe'", _ => "[" + x + "]" });
                command.CommandText = "INSERT INTO AppServiceOrders (" + string.Join(',', columns.Select(x => "[" + x + "]")) + ") SELECT " + string.Join(',', expressions) + " FROM AppServiceOrders WHERE Id='e27239d9-37e3-f148-4773-3a234a99d2b7' AND CompletionCommandHash IS NULL AND Status=4";
                command.Parameters.Clear(); command.Parameters.AddWithValue("@id", id); command.Parameters.AddWithValue("@number", "UATSVC_20260907_L" + time.Replace(":", "")); command.Parameters.AddWithValue("@wall", wall);
                if (await command.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("Expected one NEW synthetic clone row, no existing row update");
                await using var db = new VPureLuxDbContext(new DbContextOptionsBuilder<VPureLuxDbContext>().UseSqlServer(sql).Options);
                await db.Database.UseTransactionAsync(transaction);
                var stored = await db.ServiceOrders.IgnoreQueryFilters().SingleAsync(x => x.Id == id);
                if (stored.CompletedAt != wall || stored.CompletionCommandHash != null) throw new InvalidOperationException("Legacy facts unexpectedly reinterpreted");
                IQueryable<BusinessRevenueRowDto> Query(DateTime from, DateTime end) => ((IQueryable<BusinessRevenueRowDto>)typeof(EfCoreBusinessRevenueReadRepository)
                    .GetMethod("BuildQuery", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [db, new GetBusinessRevenueListInput { Source = BusinessRevenueSource.Service, FromDate = from }, end])!).IgnoreQueryFilters().Where(x => x.DocumentId == id);
                var row = await Query(wall.Date, wall.Date.AddDays(1)).SingleAsync();
                var before = await Query(wall.Date.AddDays(-1), wall.Date).CountAsync();
                var after = await Query(wall.Date.AddDays(1), wall.Date.AddDays(2)).CountAsync();
                if (row.DocumentDate != wall || before != 0 || after != 0) throw new InvalidOperationException("Legacy report boundary shifted: " + time);
                evidence.Add(new { Id = id, Clock = time, Stored = stored.CompletedAt, Report = row.DocumentDate, AdjacentDaysCount = before + after });
                Console.WriteLine("LEGACY_SQL_BOUNDARY_PASS " + time);
            }
        }
        finally { await transaction.RollbackAsync(); }
        File.WriteAllText(output, JsonSerializer.Serialize(new { Database = target, Mode = "Six NEW synthetic legacy-shaped order rows inside rolled-back clone-only transaction. No existing row changed, no VPL write, no backfill.", Cases = evidence, RolledBack = true }, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
}
