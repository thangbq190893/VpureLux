param([switch]$AllowFixtureWrites)
if(!$AllowFixtureWrites){throw 'Explicit fixture opt-in required'}
. "$PSScriptRoot/Run-Uat.ps1" -Phase Helpers -AllowFixtureWrites
if(!$s.TimeCases){$s.TimeCases=@{}}
$pos=$s.AssetDetails.positions|Where-Object positionCode -eq 'CORE-8'
foreach($time in @('00:00','00:30','01:30','06:59','07:00','23:59')){
    if($s.TimeCases[$time]){continue}
    $label='time'+$time.Replace(':','')
    $o=CreateOrder $label @(@{LineType=1;CatalogItemId=$s.Components['8'].id;CustomerAssetComponentId=$pos.id;Quantity=1;UnitPrice=100000});$o=StartOrder $label
    $at="2026-09-06T${time}:00+07:00"
    $body=@{ConcurrencyStamp=$o.concurrencyStamp;IdempotencyKey="${p}_$label";CompletedAt=$at;Lines=@(@{LineId=$o.lines[0].id;ActualQuantity=1})}
    $result=Invoke-S006 POST "/api/app/service-order/$($o.id)/complete" $body
    $stored=SqlRead 'SELECT CompletedAt,CompletionCommandHash FROM AppServiceOrders WHERE Id=@id' @{id=[Guid]$o.id}
    Assert-S006 ($stored.Rows[0].CompletedAt -eq ([datetimeoffset]$at).UtcDateTime) "$time SQL persists UTC instant"
    $detail=Invoke-S006 GET "/api/app/service-order/$($o.id)"
    Assert-S006 (([datetime]$detail.completedAt).Ticks -eq ([datetimeoffset]$at).UtcDateTime.Ticks -and !$detail.isLegacyCompletion) "$time detail API preserves existing UTC-valued field and identifies new facts"
    $page=Invoke-WebRequest "$script:UatBaseUrl/Service/Details/$($o.id)" -WebSession $script:UatSession
    Assert-S006 ($page.Content.Contains("06/09/2026 $time")) "$time rendered detail preserves Vietnam business time"
    $report=Invoke-S006 GET "/api/app/business-revenue/service-list?SearchText=$($o.orderNo)&FromDate=2026-09-06&ToDate=2026-09-06&MaxResultCount=10"
    $excluded=Invoke-S006 GET "/api/app/business-revenue/service-list?SearchText=$($o.orderNo)&FromDate=2026-09-05&ToDate=2026-09-05&MaxResultCount=10"
    Assert-S006 ($report.totalCount -eq 1 -and $excluded.totalCount -eq 0) "$time report business-date boundary includes only intended day"
    $asset=Invoke-S006 GET "/api/app/warranty/$($s.Asset.assetId)/asset-details"
    $position=$asset.positions|Where-Object positionCode -eq 'CORE-8'
    Assert-S006 (([datetime]$position.replacementBaselineDate).Date -eq [datetime]'2026-09-06') "$time reminder baseline uses business date"
    $s.TimeCases[$time]=@{OrderId=$o.id;Stored=$stored.Rows[0].CompletedAt;Detail=$detail.completedAt;Report=$report.items[0];Baseline=$position.replacementBaselineDate};Save
}
if(!$s.Evidence.PolicyPass){
    $before=Invoke-S006 GET "/api/app/warranty/reminder-list?SearchText=$($s.Asset.assetNo)&MaxResultCount=1000"
    $oldDetail=Invoke-S006 GET "/api/app/service-order/$($s.Orders.core123.id)"
    $null=Invoke-S006 POST "/api/app/warranty/set-policy/$($s.Components['1'].id)" @{IsEnabled=$true;CycleMonths=6;WarningDaysBeforeDue=21}
    $null=Invoke-S006 POST "/api/app/warranty/set-policy/$($s.Components['2'].id)" @{IsEnabled=$false;CycleMonths=3;WarningDaysBeforeDue=7}
    $after=Invoke-S006 GET "/api/app/warranty/reminder-list?SearchText=$($s.Asset.assetNo)&MaxResultCount=1000"
    Assert-S006 (($before|ConvertTo-Json -Depth 20 -Compress) -ceq ($after|ConvertTo-Json -Depth 20 -Compress)) 'Policy change alone never rewrites existing cycles'
    $work=Invoke-S006 GET "/api/app/service-work/$($s.Unknown.id)"
    $null=Invoke-S006 PUT "/api/app/service-work/$($work.id)" @{Code=$work.code;Name="$p changed catalog";Unit='Lan';DefaultPrice=999000;StandardCost=90000;Status=1;ConcurrencyStamp=$work.concurrencyStamp}
    $newDetail=Invoke-S006 GET "/api/app/service-order/$($s.Orders.core123.id)"
    Assert-S006 (($oldDetail|ConvertTo-Json -Depth 20 -Compress) -ceq ($newDetail|ConvertTo-Json -Depth 20 -Compress)) 'Catalog changes leave completed charge and unknown labor snapshot intact'
    $lines=@(1..2|ForEach-Object{$i=$_;$pos=$s.AssetDetails.positions|Where-Object positionCode -eq "CORE-$i";@{LineType=1;CatalogItemId=$s.Components["$i"].id;CustomerAssetComponentId=$pos.id;Quantity=1;UnitPrice=100000}})
    $o=CreateOrder 'policy' $lines;$o=StartOrder 'policy'
    $null=Invoke-S006 POST "/api/app/service-order/$($o.id)/complete" @{ConcurrencyStamp=$o.concurrencyStamp;IdempotencyKey="${p}_policy";CompletedAt='2026-09-07T07:00:00+07:00';Lines=@($o.lines|ForEach-Object{@{LineId=$_.id;ActualQuantity=1}})}
    $r=SqlRead @'
SELECT p.PositionCode,r.CycleMonthsSnapshot,r.WarningDaysBeforeDueSnapshot,r.DueDate
FROM AppAssetReplacementReminders r JOIN AppCustomerAssetComponents p ON p.Id=r.CustomerAssetComponentId
WHERE r.CustomerAssetId=@id AND r.Status=1 AND p.PositionCode IN ('CORE-1','CORE-2')
'@ @{id=[Guid]$s.Asset.assetId}
    Assert-S006 ($r.Rows.Count -eq 1 -and $r.Rows[0].PositionCode -eq 'CORE-1' -and $r.Rows[0].CycleMonthsSnapshot -eq 6 -and $r.Rows[0].WarningDaysBeforeDueSnapshot -eq 21) 'Successor uses current enabled policy; disabled policy has no successor'
    $s.Evidence.PolicyPass=$true;Save
}
