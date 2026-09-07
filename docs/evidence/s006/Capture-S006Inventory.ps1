param(
    [ValidatePattern('^(VPL|VPL_SERVICE_REHEARSAL_[0-9_]+|VPL_SERVICE_EMPTY_[0-9_]+)$')]
    [string]$Database = 'VPL',
    [Parameter(Mandatory)][string]$OutputPath,
    [string]$OriginalBaseline
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../../..").Path
$output = [IO.Path]::GetFullPath((Join-Path $root $OutputPath))
if (Test-Path -LiteralPath $output) { throw 'Evidence is immutable; choose a new output path.' }
$config = Get-Content "$root/src/VPureLux.Web/appsettings.json" -Raw | ConvertFrom-Json
$builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder($config.ConnectionStrings.Default)
if ($builder.InitialCatalog -cne 'VPL') { throw 'Configured source must be VPL before any connection.' }
$builder['Initial Catalog'] = $Database
$builder['Connect Timeout'] = 15
$builder['Application Name'] = 'S006_READ_ONLY_INVENTORY'
$connection = New-Object System.Data.SqlClient.SqlConnection($builder.ConnectionString)
function Read-Table([string]$sql) {
    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    $command.CommandTimeout = 90
    $table = New-Object System.Data.DataTable
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($command)
    try { [void]$adapter.Fill($table); return ,$table } finally { $adapter.Dispose(); $command.Dispose() }
}
function Records($table) {
    foreach ($row in $table.Rows) {
        $record = [ordered]@{}
        foreach ($column in $table.Columns) { $record[$column.ColumnName] = $(if ($row.IsNull($column)) { $null } else { $row[$column] }) }
        [pscustomobject]$record
    }
}
function Hash([string]$value) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($value)))).Replace('-', '') }
    finally { $sha.Dispose() }
}
try {
    $connection.Open()
    $target = Read-Table 'SELECT @@SERVERNAME AS ServerName, DB_NAME() AS DatabaseName'
    if ($target.Rows[0].DatabaseName -cne $Database) { throw 'STOP: database identity mismatch.' }
    $snapshot = [ordered]@{ Target = @(Records $target)[0]; CapturedUtc = [DateTime]::UtcNow.ToString('O'); SqlMode = 'SELECT only'; Tables = @() }
    $snapshot.History = @(Records (Read-Table 'SELECT MigrationId,ProductVersion FROM dbo.__EFMigrationsHistory ORDER BY MigrationId'))
    $snapshot.Columns = @(Records (Read-Table "SELECT t.name AS TableName,c.name AS ColumnName,ty.name AS TypeName,c.max_length,c.precision,c.scale,c.is_nullable,c.is_identity,c.is_computed,dc.definition AS DefaultDefinition FROM sys.tables t JOIN sys.columns c ON t.object_id=c.object_id JOIN sys.types ty ON c.user_type_id=ty.user_type_id LEFT JOIN sys.default_constraints dc ON dc.object_id=c.default_object_id WHERE SCHEMA_NAME(t.schema_id)='dbo' AND t.name LIKE 'App%' ORDER BY t.name,c.column_id"))
    $snapshot.Indexes = @(Records (Read-Table "SELECT t.name AS TableName,i.name,i.type_desc,i.is_unique,i.is_primary_key,i.is_disabled,i.filter_definition,c.name AS ColumnName,ic.key_ordinal,ic.is_descending_key,ic.is_included_column FROM sys.tables t JOIN sys.indexes i ON i.object_id=t.object_id JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE t.name LIKE 'App%' ORDER BY t.name,i.name,ic.index_column_id"))
    $snapshot.ForeignKeys = @(Records (Read-Table "SELECT OBJECT_NAME(f.parent_object_id) AS TableName,f.name,COL_NAME(fc.parent_object_id,fc.parent_column_id) AS ColumnName,OBJECT_NAME(f.referenced_object_id) AS PrincipalTable,COL_NAME(fc.referenced_object_id,fc.referenced_column_id) AS PrincipalColumn,f.delete_referential_action_desc,f.is_disabled,f.is_not_trusted FROM sys.foreign_keys f JOIN sys.foreign_key_columns fc ON f.object_id=fc.constraint_object_id WHERE OBJECT_NAME(f.parent_object_id) LIKE 'App%' ORDER BY TableName,f.name,fc.constraint_column_id"))
    $snapshot.Checks = @(Records (Read-Table "SELECT OBJECT_NAME(parent_object_id) AS TableName,name,definition,is_disabled,is_not_trusted FROM sys.check_constraints WHERE OBJECT_NAME(parent_object_id) LIKE 'App%' ORDER BY TableName,name"))
    $original = if ($OriginalBaseline) { Get-Content (Join-Path $root $OriginalBaseline) -Raw | ConvertFrom-Json } else { $null }
    foreach ($table in (Read-Table "SELECT name FROM sys.tables WHERE SCHEMA_NAME(schema_id)='dbo' AND name LIKE 'App%' ORDER BY name").Rows) {
        $name = [string]$table.name
        if ($name -notmatch '^App[A-Za-z0-9]+$') { throw 'Unexpected table identifier.' }
        $old = if ($original) { $original.Tables | Where-Object Name -EQ $name } else { $null }
        $columns = if ($old) { @($old.Columns) } else { @($snapshot.Columns | Where-Object TableName -EQ $name | ForEach-Object ColumnName) }
        if ($columns | Where-Object { $_ -notmatch '^[A-Za-z0-9_]+$' }) { throw 'Unexpected column identifier.' }
        $keys = @($snapshot.Indexes | Where-Object { $_.TableName -eq $name -and $_.is_primary_key } | Sort-Object key_ordinal | ForEach-Object ColumnName)
        if (!$keys.Count) { throw "No primary key for $name" }
        $keySql = ($keys | ForEach-Object { "a.[$_]" }) -join ','
        $allSql = ($columns | ForEach-Object { "a.[$_]" }) -join ','
        $facts = @($columns | Where-Object { $_ -notin @('RowVersion','ConcurrencyStamp','LastModificationTime','LastModifierId') })
        $factsSql = ($facts | ForEach-Object { "a.[$_]" }) -join ','
        $rows = @(Records (Read-Table "SELECT (SELECT $keySql FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) AS RowKey,CONVERT(varchar(64),HASHBYTES('SHA2_256',(SELECT $allSql FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES)),2) AS FullHash,CONVERT(varchar(64),HASHBYTES('SHA2_256',(SELECT $factsSql FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES)),2) AS FactHash FROM dbo.[$name] a ORDER BY $keySql"))
        $entry = [ordered]@{ Name=$name; Columns=$columns; Keys=$keys; Count=$rows.Count; FullHash=(Hash (($rows | ForEach-Object { $_.RowKey + ':' + $_.FullHash }) -join "`n")); FactHash=(Hash (($rows | ForEach-Object { $_.RowKey + ':' + $_.FactHash }) -join "`n")); Rows=$rows }
        if ('Status' -in $columns) { $entry.StatusCounts = @(Records (Read-Table "SELECT Status,COUNT_BIG(*) AS [RowCount] FROM dbo.[$name] GROUP BY Status ORDER BY Status")) }
        $snapshot.Tables += [pscustomobject]$entry
    }
    if ($snapshot.Tables.Name -contains 'AppServiceOrderLines') {
        $snapshot.LegacyMaterial = @(Records (Read-Table 'SELECT o.Id AS OrderId,o.OrderNo,o.CompletedAt,o.InventoryTransactionId,l.Id AS LineId,l.[LineNo],l.ComponentId,l.ActualQuantity,l.CostAmountSnapshot FROM dbo.AppServiceOrders o JOIN dbo.AppServiceOrderLines l ON l.ServiceOrderId=o.Id WHERE o.Status=4 AND l.LineType=1 ORDER BY o.Id,l.[LineNo]'))
    }
    [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($output))
    $snapshot | ConvertTo-Json -Depth 15 | Set-Content -LiteralPath $output -Encoding utf8
    $snapshot.Target
    $snapshot.Tables | Select-Object Name,Count,FullHash
    Get-FileHash -LiteralPath $output -Algorithm SHA256
} finally { $connection.Dispose() }
