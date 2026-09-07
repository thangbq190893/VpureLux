param([switch]$AllowFixtureWrites)
if(!$AllowFixtureWrites){throw 'Explicit fixture opt-in required'}
. "$PSScriptRoot/Run-Uat.ps1" -Phase Helpers -AllowFixtureWrites
if($s.SecurityDone){throw 'Already complete'}
function Permission([string]$Name,[bool]$Allowed){
    $null=Invoke-WebRequest "$script:UatBaseUrl/s006-control/permission" -Method Post -ContentType 'application/json' -Body (@{Name=$Name;Allowed=$Allowed}|ConvertTo-Json) -Headers @{'X-S006-Control'=$script:Auth.Control}
}
$o=CreateOrder 'security' @(@{LineType=2;CatalogItemId=$s.Known.id;Quantity=10;UnitPrice=100000})
$orderPath="/api/app/service-order/$($o.id)";$moneyPath="/api/app/service-payment/$($o.id)"
$create=@{CustomerAssetId=$s.Asset.assetId;WarehouseId=$s.Warehouse.id;Note="$p security antiforgery";Lines=@(@{LineType=2;CatalogItemId=$s.Known.id;Quantity=1;UnitPrice=100000})}
$work=@{Code="${p}_SECURITY";Name="$p security";Unit='Lan';DefaultPrice=100000;Status=1}
$complete=@{ConcurrencyStamp=$o.concurrencyStamp;IdempotencyKey="${p}_seccomplete";CompletedAt='2026-09-07T07:00:00+07:00';Lines=@(@{LineId=$o.lines[0].id;ActualQuantity=1})}
$edit=@{OrderDate=$o.orderDate;ConcurrencyStamp=$o.concurrencyStamp;Lines=$create.Lines}
$void=@{PaymentId=[Guid]::NewGuid();ConcurrencyStamp=$o.concurrencyStamp;IdempotencyKey="${p}_secvoid";Reason="$p void"}
$cases=@(
    @{Permission='Service.View';Method='GET';Path='/api/app/service-order?MaxResultCount=1'},
    @{Permission='Service.Create';Method='POST';Path='/api/app/service-order';Body=$create},
    @{Permission='Service.Edit';Method='PUT';Path=$orderPath;Body=$edit},
    @{Permission='Service.Confirm';Method='POST';Path="$orderPath/confirm";Body=@{ConcurrencyStamp=$o.concurrencyStamp}},
    @{Permission='Service.Cancel';Method='POST';Path="$orderPath/cancel";Body=@{ConcurrencyStamp=$o.concurrencyStamp;Reason="$p cancel"}},
    @{Permission='Service.Complete';Method='POST';Path="$orderPath/complete";Body=$complete},
    @{Permission='Service.ManagePayments';Method='POST';Path="$moneyPath/payment";Body=(PayBody 100000 'secpaid')},
    @{Permission='Service.ManageWorks';Method='POST';Path='/api/app/service-work';Body=$work},
    @{Permission='Reports.Service.View';Method='GET';Path='/api/app/business-revenue/service-list?MaxResultCount=1'},
    @{Permission='Reports.Consolidated.View';Method='GET';Path='/api/app/business-revenue/consolidated-list?MaxResultCount=1'},
    @{Permission='Reports.Sales.View';Method='GET';Path='/api/app/business-revenue/consolidated-list?Source=1&MaxResultCount=1'}
)
$e=@()
foreach($case in $cases){
    $name="VPureLux.$($case.Permission)"
    try{Permission $name $false;$r=Invoke-S006 $case.Method $case.Path $case.Body -Raw;Assert-S006 ($r.Status -eq 403) "$name server deny";$e+=@{Permission=$name;Status=$r.Status}}
    finally{Permission $name $true}
}
$anti=@($cases|Where-Object{$_.Method -eq 'POST' -and $_.Permission -notlike 'Reports.*'})
$anti+=@{Method='POST';Path="$moneyPath/void-payment";Body=$void},@{Method='POST';Path="$moneyPath/refund";Body=(RefundBody 100000 'secrefund')}
foreach($case in $anti){$r=Invoke-S006 POST $case.Path $case.Body -Raw -NoToken;$s.Evidence.LastAntiforgery=$r;Save;Assert-S006 ($r.Status -eq 400 -or $r.FinalUrl -like '*/Error?httpStatusCode=400') "Antiforgery missing rejected: $($case.Path)";$e+=@{Antiforgery=$case.Path;Status=$r.Status;FinalUrl=$r.FinalUrl}}
$s.Evidence.Security=$e;Save
$report='/api/app/business-revenue/service-list?SearchText=UATSVC_20260907&FromDate=2026-01-01&ToDate=2026-12-31&MaxResultCount=2'
try{
    Permission 'VPureLux.Service.ViewCost' $false;Permission 'VPureLux.Service.ViewProfit' $false
    $r=Invoke-S006 GET $report
    foreach($row in $r.items){Assert-S006 ($null -eq $row.materialCost -and $null -eq $row.profit -and $null -eq $row.totalKnownCost) 'Restricted report costs/profit absent'}
}finally{Permission 'VPureLux.Service.ViewCost' $true;Permission 'VPureLux.Service.ViewProfit' $true}
foreach($endpoint in @('/api/app/service-work','/api/app/service-order','/api/app/business-revenue/service-list')){
    $r=Invoke-S006 GET "${endpoint}?SearchText=$p&MaxResultCount=1&SkipCount=1&FromDate=2026-01-01&ToDate=2026-12-31"
    Assert-S006 ($r.totalCount -gt 1 -and $r.items.Count -eq 1) "Server paging page 2: $endpoint"
}
$s.SecurityDone=$true;Save
