using System.Text.Json;
using System.Text.RegularExpressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using VPureLux.EntityFrameworkCore;
using VPureLux.Reports;
using VPureLux.Migrations;

if (args.Length == 2 && args[0] == "performance") return await LiveReadChecks.RunAsync(args[1]);
if (args.Length == 2 && args[0] == "legacy-boundaries") return await LegacyBoundaries.RunAsync(args[1]);

// Offline comparison only: no DbContext, SQL connection, migration application or seed.
if (args.Length == 2 && args[0] == "read-smoke")
{
    var b = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("S006_SQL_CONNECTION") ?? throw new ArgumentException("Explicit S006_SQL_CONNECTION required"));
    if (!Regex.IsMatch(b.InitialCatalog, "^VPL_SERVICE_REHEARSAL_20260907_[0-9]{6}$")) throw new InvalidOperationException("Only isolated S006 rehearsal catalog allowed");
    if (File.Exists(args[1])) throw new InvalidOperationException("Do not overwrite evidence");
    await using var connection = new SqlConnection(b.ConnectionString);
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT DB_NAME()";
    if ((string?)await command.ExecuteScalarAsync() != b.InitialCatalog) throw new InvalidOperationException("Catalog mismatch");
    await using var db = new VPureLuxDbContext(new DbContextOptionsBuilder<VPureLuxDbContext>().UseSqlServer(connection).Options);
    var results = new List<object>();
    foreach (var source in new BusinessRevenueSource?[] { BusinessRevenueSource.Sales, BusinessRevenueSource.Service, null })
    {
        var query = (IQueryable<BusinessRevenueRowDto>)typeof(EfCoreBusinessRevenueReadRepository).GetMethod("BuildQuery",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,
            [db,new GetBusinessRevenueListInput { Source=source,FromDate=new DateTime(2026,1,1)},new DateTime(2027,1,1)])!;
        query=query.IgnoreQueryFilters();
        var count=await query.CountAsync();
        var page=await query.OrderBy(x=>x.DocumentDate).ThenBy(x=>x.Source).ThenBy(x=>x.DocumentId).Take(10).ToListAsync();
        var totals=(IQueryable<BusinessRevenueSummaryDto>)typeof(EfCoreBusinessRevenueReadRepository).GetMethod("Totals",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,[query])!;
        File.WriteAllText(args[1] + "." + (source?.ToString() ?? "All") + ".sql", totals.ToQueryString());
        try { results.Add(new { Source=source,Count=count,Rows=page,Totals=await totals.SingleOrDefaultAsync() }); }
        catch (SqlException ex)
        {
            File.WriteAllText(args[1],JsonSerializer.Serialize(new { Gate="BLOCKED",Source=source,ex.Number,ex.Message,CompletedScopes=results },new JsonSerializerOptions{WriteIndented=true}));
            Console.Error.WriteLine($"SQL_READ_BLOCKED Source={source} Error={ex.Number}: {ex.Message}");
            return 2;
        }
    }
    File.WriteAllText(args[1],JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
    Console.WriteLine("SQL_SERVER_REPORT_READ_SMOKE_PASS " + b.InitialCatalog);
    return 0;
}
if (args.Length != 2) throw new ArgumentException("Expected inventory JSON and new evidence output path.");
if (File.Exists(args[1])) throw new InvalidOperationException("Do not overwrite evidence.");
using var input = JsonDocument.Parse(File.ReadAllText(args[0]));
var root = input.RootElement;
var columns = root.GetProperty("Columns").EnumerateArray().ToArray();
var indexes = root.GetProperty("Indexes").EnumerateArray().ToArray();
var foreignKeys = root.GetProperty("ForeignKeys").EnumerateArray().ToArray();
var checks = root.GetProperty("Checks").EnumerateArray().ToArray();
var migration = new AddServiceModule();
var failures = new List<string>();
var expectedTables = migration.UpOperations.OfType<CreateTableOperation>().ToArray();
string Text(JsonElement e, string name) => e.GetProperty(name).ValueKind == JsonValueKind.Null ? "" : e.GetProperty(name).ToString();
string Normal(string s) => Regex.Replace(s.ToLowerInvariant(), @"[\s()]", "");
foreach (var table in expectedTables)
{
    var actual = columns.Where(x => Text(x, "TableName") == table.Name).ToArray();
    if (actual.Length != table.Columns.Count) failures.Add($"{table.Name}: columns {actual.Length}, expected {table.Columns.Count}");
    foreach (var c in table.Columns)
    {
        var match = actual.Where(x => Text(x, "ColumnName") == c.Name).ToArray();
        if (match.Length != 1) { failures.Add($"{table.Name}.{c.Name}: missing/duplicate"); continue; }
        var a = match[0];
        var type = Text(a, "TypeName");
        if (type == "timestamp") type = "rowversion";
        else if (type == "nvarchar") type += "(" + (a.GetProperty("max_length").GetInt32() == -1 ? "max" : (a.GetProperty("max_length").GetInt32() / 2).ToString()) + ")";
        else if (type == "decimal") type += $"({Text(a, "precision")},{Text(a, "scale")})";
        if (type != c.ColumnType || a.GetProperty("is_nullable").GetBoolean() != c.IsNullable || a.GetProperty("is_identity").GetBoolean() || a.GetProperty("is_computed").GetBoolean()) failures.Add($"{table.Name}.{c.Name}: type/nullability/identity mismatch ({type} vs {c.ColumnType})");
        var defaultValue = Normal(Text(a, "DefaultDefinition"));
        if (c.DefaultValue is false && defaultValue == "convert[bit],0") defaultValue = "0";
        var expectedDefault = c.DefaultValue is false ? "0" : Normal(c.DefaultValueSql ?? "");
        if (defaultValue != expectedDefault) failures.Add($"{table.Name}.{c.Name}: default differs");
    }
    var pk = indexes.Where(x => Text(x, "TableName") == table.Name && x.GetProperty("is_primary_key").GetBoolean()).OrderBy(x => x.GetProperty("key_ordinal").GetInt32()).ToArray();
    if (!pk.Select(x => Text(x,"ColumnName")).SequenceEqual(table.PrimaryKey!.Columns) || pk.Any(x => Text(x,"name") != table.PrimaryKey.Name || !x.GetProperty("is_unique").GetBoolean() || x.GetProperty("is_disabled").GetBoolean())) failures.Add($"{table.Name}: primary key differs");
    foreach (var fk in table.ForeignKeys)
    {
        var found = foreignKeys.Where(x => Text(x,"name") == fk.Name).ToArray();
        if (found.Length != fk.Columns.Length || !found.Select(x => Text(x,"ColumnName")).SequenceEqual(fk.Columns) || !found.Select(x => Text(x,"PrincipalColumn")).SequenceEqual(fk.PrincipalColumns!) || found.Any(x => Text(x,"PrincipalTable") != fk.PrincipalTable || Text(x,"delete_referential_action_desc") != (fk.OnDelete.ToString() == "Cascade" ? "CASCADE" : "NO_ACTION") || x.GetProperty("is_disabled").GetBoolean() || x.GetProperty("is_not_trusted").GetBoolean())) failures.Add($"{fk.Name}: FK mismatch");
    }
    if (foreignKeys.Count(x => Text(x,"TableName") == table.Name) != table.ForeignKeys.Sum(x => x.Columns.Length)) failures.Add($"{table.Name}: unexpected foreign keys");
    if (checks.Any(x => Text(x,"TableName") == table.Name)) failures.Add($"{table.Name}: unexpected check constraint");
}
var expectedIndexes = migration.UpOperations.OfType<CreateIndexOperation>().ToArray();
foreach (var ix in expectedIndexes)
{
    var found = indexes.Where(x => Text(x,"TableName") == ix.Table && Text(x,"name") == ix.Name).OrderBy(x => x.GetProperty("key_ordinal").GetInt32()).ToArray();
    if (!found.Select(x => Text(x,"ColumnName")).SequenceEqual(ix.Columns) || found.Any(x => x.GetProperty("is_unique").GetBoolean() != ix.IsUnique || x.GetProperty("is_disabled").GetBoolean() || x.GetProperty("is_descending_key").GetBoolean() || x.GetProperty("is_included_column").GetBoolean() || Normal(Text(x,"filter_definition")) != Normal(ix.Filter ?? ""))) failures.Add($"{ix.Name}: index mismatch");
}
var extra = indexes.Where(x => expectedTables.Any(t => t.Name == Text(x,"TableName")) && !x.GetProperty("is_primary_key").GetBoolean()).Select(x => Text(x,"name")).Distinct().Except(expectedIndexes.Select(x => x.Name));
failures.AddRange(extra.Select(x => "Unexpected index: " + x));
var result = new { Migration = "20260824113235_AddServiceModule", Gate = failures.Count == 0 ? "PASS" : "BLOCKED", Tables = expectedTables.Length, Columns = expectedTables.Sum(x => x.Columns.Count), Indexes = expectedIndexes.Length, Failures = failures };
File.WriteAllText(args[1], JsonSerializer.Serialize(result,new JsonSerializerOptions { WriteIndented=true }));
Console.WriteLine(JsonSerializer.Serialize(result));
return failures.Count == 0 ? 0 : 2;
