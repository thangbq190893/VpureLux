param(
    [ValidateSet('Capture', 'Verify')][string]$Mode = 'Capture',
    [string]$OutputDirectory = 'artifacts/w008-verification'
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../../..").Path
$config = Get-Content "$root/src/VPureLux.Web/appsettings.json" -Raw | ConvertFrom-Json
$builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder($config.ConnectionStrings.Default)
if ($builder.InitialCatalog -cne 'VPL') { throw 'Only VPL is authorized.' }
$connection = New-Object System.Data.SqlClient.SqlConnection($builder.ConnectionString)
function Read-Table([string]$sql) {
    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    $command.CommandTimeout = 60
    $table = New-Object System.Data.DataTable
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($command)
    try { [void]$adapter.Fill($table); return ,$table } finally { $adapter.Dispose(); $command.Dispose() }
}
try {
    $connection.Open()
    if ((Read-Table 'SELECT DB_NAME() AS Name').Rows[0].Name -cne 'VPL') { throw 'Database mismatch.' }
    $tables = Read-Table "SELECT name FROM sys.tables WHERE name LIKE 'App%' ORDER BY name"
    $snapshot = [ordered]@{ Database = 'VPL'; CapturedUtc = [DateTime]::UtcNow.ToString('O'); Tables = @() }
    foreach ($table in $tables.Rows) {
        $name = [string]$table.name
        if ($name -notmatch '^App[A-Za-z0-9]+$') { throw 'Unexpected table name.' }
        $keys = Read-Table "SELECT c.name FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE i.is_primary_key=1 AND i.object_id=OBJECT_ID('dbo.$name') ORDER BY ic.key_ordinal"
        if ($keys.Rows.Count -eq 0) { throw "No primary key: $name" }
        $keyColumns = ($keys.Rows | ForEach-Object { 'a.[' + $_.name + ']' }) -join ','
        $rows = Read-Table "SELECT (SELECT $keyColumns FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) AS RowKey, CONVERT(varchar(64), HASHBYTES('SHA2_256', (SELECT a.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES)), 2) AS RowHash FROM dbo.[$name] a"
        $snapshot.Tables += @{ Name = $name; Count = $rows.Rows.Count; Rows = @($rows.Rows | ForEach-Object { @{ Key = $_.RowKey; Hash = $_.RowHash } }) }
    }
    $output = Join-Path $root $OutputDirectory
    [void](New-Item -ItemType Directory -Force -Path $output)
    if ($Mode -eq 'Capture') {
        $path = Join-Path $output 'vpl-before.json'
        if (Test-Path $path) { throw 'Do not overwrite the original baseline.' }
        $snapshot | ConvertTo-Json -Depth 8 | Set-Content $path -Encoding utf8
        $snapshot.Tables | ForEach-Object { [pscustomobject]@{ Table = $_.Name; BeforeCount = $_.Count } }
    } else {
        $before = Get-Content (Join-Path $output 'vpl-before.json') -Raw | ConvertFrom-Json
        $results = foreach ($old in $before.Tables) {
            $current = $snapshot.Tables | Where-Object Name -EQ $old.Name
            $map = @{}
            foreach ($row in $current.Rows) { $map[$row.Key] = $row.Hash }
            $changed = @($old.Rows | Where-Object { $map[$_.Key] -cne $_.Hash })
            [pscustomobject]@{ Table = $old.Name; Before = $old.Count; After = $current.Count; ChangedOrMissingLegacyRows = $changed.Count; ExpectedSequenceAdvance = ($old.Name -eq 'AppNumberSequences') }
        }
        $snapshot | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $output 'vpl-after.json') -Encoding utf8
        $results | ConvertTo-Json | Set-Content (Join-Path $output 'vpl-reconciliation.json') -Encoding utf8
        $results
        if ($results | Where-Object { $_.ChangedOrMissingLegacyRows -gt 0 -and -not $_.ExpectedSequenceAdvance }) { throw 'Legacy business fingerprint mismatch: investigate; never repair automatically.' }
    }
} finally { $connection.Dispose() }
