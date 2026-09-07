param(
    [Parameter(Mandatory)][ValidatePattern('^(VPL|VPL_SERVICE_REHEARSAL_20260907_[0-9]{6}|VPL_SERVICE_EMPTY_20260907_[0-9]{6})$')][string]$Database,
    [Parameter(Mandatory)][string]$ScriptPath,
    [Parameter(Mandatory)][ValidatePattern('^[A-Fa-f0-9]{64}$')][string]$ExpectedSha256,
    [Parameter(Mandatory)][string]$BeforeInventory,
    [string]$VplGateEvidence
)
$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../../..").Path
$path=Join-Path $root $ScriptPath
if((Get-FileHash $path -Algorithm SHA256).Hash -cne $ExpectedSha256.ToUpperInvariant()){throw 'Reviewed script hash mismatch'}
$sql=Get-Content $path -Raw
if($sql -match '(?im)^\s*USE\s' -or $sql -match '\[VPureLux\]'){throw 'Unexpected catalog in script'}
$before=Get-Content (Join-Path $root $BeforeInventory) -Raw|ConvertFrom-Json
if($before.Target.DatabaseName -cne $Database){throw 'Baseline catalog mismatch'}
if($Database -ceq 'VPL'){
    if(!$VplGateEvidence){throw 'VPL requires successful rehearsal evidence'}
    $gate=Get-Content (Join-Path $root $VplGateEvidence) -Raw|ConvertFrom-Json
    if($gate.Gate -cne 'PASS' -or $gate.Database -notmatch '^VPL_SERVICE_REHEARSAL_20260907_[0-9]{6}$'){throw 'Rehearsal gate not passed'}
}
$config=Get-Content "$root/src/VPureLux.Web/appsettings.json" -Raw|ConvertFrom-Json
$b=New-Object System.Data.SqlClient.SqlConnectionStringBuilder($config.ConnectionStrings.Default)
if($b.InitialCatalog -cne 'VPL'){throw 'STOP: configured source must be VPL'}
$b['Initial Catalog']=$Database;$b['Application Name']='S006_REVIEWED_SCHEMA_ONLY'
$c=New-Object System.Data.SqlClient.SqlConnection($b.ConnectionString)
function Query([string]$text){$q=$c.CreateCommand();$q.CommandText=$text;$q.CommandTimeout=60;$a=New-Object System.Data.SqlClient.SqlDataAdapter($q);$t=New-Object System.Data.DataTable;try{[void]$a.Fill($t);return ,$t}finally{$a.Dispose();$q.Dispose()}}
try{
    $c.Open();$target=Query 'SELECT @@SERVERNAME AS ServerName,DB_NAME() AS DatabaseName'
    if($target.Rows[0].DatabaseName -cne $Database){throw 'STOP: catalog mismatch'}
    $history=@((Query 'SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId').Rows|ForEach-Object MigrationId)
    if(($history -join '|') -cne ($before.History.MigrationId -join '|')){throw 'History changed since inventory'}
    if($Database -notlike 'VPL_SERVICE_EMPTY_*' -and ($sql -match 'CREATE TABLE \[AppService(?:Works|Orders|OrderLines|Payments)\]' -or $sql -match '20260824113235_AddServiceModule')){throw 'Historical Service migration must be skipped'}
    if((Query 'SELECT DB_NAME() AS Name').Rows[0].Name -cne $Database){throw 'Catalog changed before mutation'}
    # EF executes each migration command separately; a raw GO batch can bind a filtered index before its new column exists.
    $previous=$env:ConnectionStrings__Default
    $env:ConnectionStrings__Default=$b.ConnectionString
    try{
        dotnet ef database update 20260907021302_AddServicePaymentSettlement --project "$root/src/VPureLux.EntityFrameworkCore" --configuration Release --no-build
        if($LASTEXITCODE -ne 0){throw 'EF migration failed; inspect target without automatic repair.'}
    }finally{$env:ConnectionStrings__Default=$previous}
    [pscustomobject]@{Database=$Database;ReviewedScriptSha256=$ExpectedSha256;Executor='EF migration commands, no seed';Result='PASS'}
}finally{$c.Dispose()}
