param([Parameter(Mandatory)][ValidateSet('VPureLux_SERVICE_REHEARSAL_20260907_152000','VPureLux')][string]$Database)
$ErrorActionPreference='Stop'
$repo=(Resolve-Path "$PSScriptRoot/../../..").Path
$source='C:\SourceCode\VPureLux-service-v1-b0bf197'
$head=git -C $source rev-parse HEAD
$expected=git -C $repo rev-parse b0bf197
if($head -cne $expected){throw 'Wrong migration source'}
if(git -C $source diff --name-only HEAD -- src){throw 'Dirty migration source'}
$before=Get-Content "$repo/artifacts/service-v1-rollout/prod-initial.json" -Raw|ConvertFrom-Json
if($Database -ceq 'VPureLux'){
    $gate=Get-Content "$repo/artifacts/service-v1-rollout/clone-reconciliation.json" -Raw|ConvertFrom-Json
    if($gate.Gate -cne 'PASS'){throw 'Rehearsal gate required'}
    $stop=Get-Content "$repo/artifacts/service-v1-rollout/pre-migration-stop.json" -Raw|ConvertFrom-Json
    if($stop.ExitStatus -ne 0 -or $stop.Output -notcontains 'WEB_STOPPED_VERIFIED'){throw 'Web stop proof required'}
}
$cfg=Get-Content "$repo/src/VPureLux.Web/appsettings.json" -Raw|ConvertFrom-Json
$b=[System.Data.SqlClient.SqlConnectionStringBuilder]::new($cfg.ConnectionStrings.Default)
$b['Initial Catalog']=$Database
$c=[System.Data.SqlClient.SqlConnection]::new($b.ConnectionString)
try{
    $c.Open();$q=$c.CreateCommand();$q.CommandText='SELECT DB_NAME()';if($q.ExecuteScalar() -cne $Database){throw 'Catalog mismatch'}
    $q.CommandText='SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId'
    $r=$q.ExecuteReader();$history=@();while($r.Read()){$history+=$r.GetString(0)};$r.Close()
    if(($history -join '|') -cne ($before.History.MigrationId -join '|')){throw 'STOP: migration history mismatch'}
}finally{$c.Dispose()}
$prior=$env:ConnectionStrings__Default
$env:ConnectionStrings__Default=$b.ConnectionString
Push-Location "$source/src/VPureLux.EntityFrameworkCore"
try{
    dotnet ef database update 20260907021302_AddServicePaymentSettlement --configuration Release --no-build 2>&1 | Tee-Object "$repo/artifacts/service-v1-rollout/migration-$Database.log"
    if($LASTEXITCODE -ne 0){throw 'Migration failure: STOP, no repair'}
}finally{Pop-Location;$env:ConnectionStrings__Default=$prior}
