$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../../..").Path
Set-Location $root
$env:DOTNET_GCHeapHardLimit='0x60000000'
$groups=@(
    @{Name='domain';Project='Domain';Filter='FullyQualifiedName~Service|FullyQualifiedName~Inventory|FullyQualifiedName~Warranty|FullyQualifiedName~CustomerCare|FullyQualifiedName~Sales'},
    @{Name='application';Project='Application';Filter='FullyQualifiedName~Service|FullyQualifiedName~Reports'},
    @{Name='ef';Project='EntityFrameworkCore';Filter='FullyQualifiedName~Service|FullyQualifiedName~Reports|FullyQualifiedName~Sales|FullyQualifiedName~Inventory|FullyQualifiedName~Warranty|FullyQualifiedName~CustomerCare'},
    @{Name='web-work';Project='Web';Filter='FullyQualifiedName~ServiceWork'},
    @{Name='web-order';Project='Web';Filter='FullyQualifiedName~ServiceOrderWebTests'},
    @{Name='web-money';Project='Web';Filter='FullyQualifiedName~ServiceMoney'},
    @{Name='web-business-report';Project='Web';Filter='FullyQualifiedName~BusinessReport'},
    @{Name='web-warranty';Project='Web';Filter='FullyQualifiedName~Warranty'},
    @{Name='web-sales-report';Project='Web';Filter='FullyQualifiedName~ReportsPagesTests'}
)
$result=@()
foreach($group in $groups){
    $project="test/VPureLux.$($group.Project).Tests/VPureLux.$($group.Project).Tests.csproj"
    dotnet test $project -c Release --no-build --filter $group.Filter -m:1 --blame-hang-timeout 90s --logger "trx;LogFileName=$($group.Name).trx" --results-directory artifacts/s006/regression *> "artifacts/s006/regression-$($group.Name).log"
    $code=$LASTEXITCODE
    $result+=@{Group=$group.Name;ExitCode=$code;Filter=$group.Filter}
    $result|ConvertTo-Json|Set-Content artifacts/s006/regression-status.json
    Write-Host "$($group.Name): exit $code"
}
if(@($result|Where-Object ExitCode -ne 0).Count){exit 1}
