param([Parameter(Mandatory)][string]$Before,[Parameter(Mandatory)][string]$After,[Parameter(Mandatory)][string]$Output)
$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../../..").Path
$b=Get-Content (Join-Path $root $Before) -Raw|ConvertFrom-Json
$a=Get-Content (Join-Path $root $After) -Raw|ConvertFrom-Json
if(Test-Path (Join-Path $root $Output)){throw 'Do not overwrite evidence'}
$results=@(foreach($old in $b.Tables){
    $current=$a.Tables|Where-Object Name -EQ $old.Name
    $rows=@{};foreach($r in $current.Rows){$rows[$r.RowKey]=$r}
    $full=@($old.Rows|Where-Object {$rows[$_.RowKey].FullHash -cne $_.FullHash})
    $facts=@($old.Rows|Where-Object {$rows[$_.RowKey].FactHash -cne $_.FactHash})
    [pscustomobject]@{Name=$old.Name;Before=$old.Count;After=$current.Count;FullHashBefore=$old.FullHash;FullHashAfter=$current.FullHash;ChangedOrMissingRows=$full.Count;ChangedOrMissingFacts=$facts.Count}
})
$failures=@($results|Where-Object {$_.ChangedOrMissingRows -gt 0 -or $_.ChangedOrMissingFacts -gt 0})
$schemaChanges=@(foreach($column in $b.Columns){
    $current=$a.Columns|Where-Object {$_.TableName -eq $column.TableName -and $_.ColumnName -eq $column.ColumnName}
    if(($column|ConvertTo-Json -Compress) -cne ($current|ConvertTo-Json -Compress)){"$($column.TableName).$($column.ColumnName)"}
})
$r=[ordered]@{Database=$a.Target.DatabaseName;Gate=$(if($failures.Count -eq 0 -and $schemaChanges.Count -eq 0){'PASS'}else{'BLOCKED'});LegacyTables=$results.Count;ChangedTables=$failures.Count;OriginalColumnChanges=$schemaChanges;Tables=$results}
$r|ConvertTo-Json -Depth 10|Set-Content (Join-Path $root $Output) -Encoding utf8
[pscustomobject]@{Database=$r.Database;Gate=$r.Gate;LegacyTables=$r.LegacyTables;ChangedTables=$r.ChangedTables;OriginalColumnChanges=$schemaChanges.Count}
if($r.Gate -ne 'PASS'){throw 'Legacy mismatch. Do not repair.'}
