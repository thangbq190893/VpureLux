param([switch]$AllowFixtureWrites)
if(!$AllowFixtureWrites){throw 'Explicit fixture opt-in required'}
. "$PSScriptRoot/Run-Uat.ps1" -Phase Helpers -AllowFixtureWrites
if($s.Evidence.FinalSnapshotPass){Write-Host 'Already verified';exit}
function ReadSnapshot{
    $o=Invoke-S006 GET "/api/app/service-order/$($s.Orders.core123.id)"
    $r=Invoke-S006 GET "/api/app/business-revenue/service-list?SearchText=$($o.orderNo)&FromDate=2026-01-01&ToDate=2026-12-31&MaxResultCount=10"
    $t=SqlRead @'
SELECT
(SELECT * FROM AppAssetMaintenanceEvents WHERE CustomerAssetId=@id ORDER BY Id FOR JSON PATH) AS Events,
(SELECT * FROM AppAssetReplacementReminders WHERE CustomerAssetId=@id ORDER BY Id FOR JSON PATH) AS Reminders
'@ @{id=[Guid]$s.Asset.assetId}
    return @{Order=$o;Report=$r;Events=$t.Rows[0].Events;Reminders=$t.Rows[0].Reminders}
}
$before=ReadSnapshot
$w=Invoke-S006 GET "/api/app/service-work/$($s.Unknown.id)"
$null=Invoke-S006 PUT "/api/app/service-work/$($w.id)" @{Code=$w.code;Name=$w.name;Unit='Gio';DefaultPrice=888000;StandardCost=80000;Status=1;ConcurrencyStamp=$w.concurrencyStamp}
$c=Invoke-S006 GET "/api/app/component/$($s.Components['3'].id)"
$null=Invoke-S006 PUT "/api/app/component/$($c.id)" @{Name="$p Core 3 current renamed";Unit=$c.unit;Description="$p metadata-only change"}
$after=ReadSnapshot
foreach($key in $before.Keys){Assert-S006 (($before[$key]|ConvertTo-Json -Depth 40 -Compress) -ceq ($after[$key]|ConvertTo-Json -Depth 40 -Compress)) "Historical $key unchanged after current Work unit/price/cost and Component metadata edits"}
$s.Evidence.FinalSnapshot=@{Before=$before;After=$after};$s.Evidence.FinalSnapshotPass=$true;Save
