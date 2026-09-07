$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../../..").Path
Set-Location $root
$env:DOTNET_GCHeapHardLimit='0x60000000'
$names=[Collections.Generic.HashSet[string]]::new()
foreach($file in @('web-order','web-sales-report')){
    [xml]$x=Get-Content "artifacts/s006/regression/$file.trx" -Raw
    foreach($test in $x.TestRun.TestDefinitions.UnitTest){[void]$names.Add("$($test.TestMethod.className).$($test.TestMethod.name)")}
}
[void]$names.Add('VPureLux.Pages.ServiceOrderWebTests.Completion_details_preserve_legacy_wall_time_and_convert_new_UTC_at_day_boundaries')
$results=@();$i=0
foreach($name in ($names|Sort-Object)){
    $i++;$label="web-split-$i"
    dotnet test test/VPureLux.Web.Tests/VPureLux.Web.Tests.csproj -c Release --no-build --filter "FullyQualifiedName=$name" -m:1 --blame-hang-timeout 90s --logger "trx;LogFileName=$label.trx" --results-directory artifacts/s006/web-split *> "artifacts/s006/$label.log"
    $code=$LASTEXITCODE
    [xml]$trx=Get-Content "artifacts/s006/web-split/$label.trx" -Raw
    $count=$trx.TestRun.ResultSummary.Counters
    if([int]$count.total -eq 0){$code=2}
    $results+=@{Name=$name;ExitCode=$code;Total=[int]$count.total;Passed=[int]$count.passed;Failed=[int]$count.failed}
    $results|ConvertTo-Json|Set-Content artifacts/s006/web-split-status.json
    Write-Host "$label $name total=$($count.total) passed=$($count.passed) exit=$code"
}
if(@($results|Where-Object ExitCode -ne 0).Count){exit 1}
