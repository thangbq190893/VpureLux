param([switch]$AllowFixtureWrites)
if(!$AllowFixtureWrites){throw 'Explicit fixture opt-in required'}
. "$PSScriptRoot/Run-Uat.ps1" -Phase Helpers -AllowFixtureWrites
if($s.EdgeDone){throw 'Edges already complete'}
function CompleteInput($o,[string]$Key){return @{ConcurrencyStamp=$o.concurrencyStamp;IdempotencyKey="${p}_$Key";CompletedAt='2026-09-07T07:00:00+07:00';Lines=@($o.lines|ForEach-Object{@{LineId=$_.id;ActualQuantity=$_.plannedQuantity}})}}
if(!$s.EdgeWork){$s.EdgeWork=Invoke-S006 POST '/api/app/service-work' @{Code="${p}_EDGE";Name="$p original edge";Unit='Lan';DefaultPrice=100000;StandardCost=0;Status=1};Save}
if(!$s.Evidence.EditFlow){
    $o=CreateOrder 'editflow' @(@{LineType=2;CatalogItemId=$s.EdgeWork.id;Quantity=1;UnitPrice=100000})
    $w=Invoke-S006 GET "/api/app/service-work/$($s.EdgeWork.id)"
    $null=Invoke-S006 PUT "/api/app/service-work/$($w.id)" @{Code=$w.code;Name="$p changed inactive";Unit='Gio';DefaultPrice=999999;StandardCost=99999;Status=2;ConcurrencyStamp=$w.concurrencyStamp}
    $old=$o.lines[0]
    $body=@{OrderDate=$o.orderDate;Note="$p header only";ConcurrencyStamp=$o.concurrencyStamp;Lines=@(@{Id=$old.id;LineType=2;CatalogItemId=$s.EdgeWork.id;Quantity=1;UnitPrice=100000})}
    $edited=Invoke-S006 PUT "/api/app/service-order/$($o.id)" $body
    Assert-S006 ($edited.lines[0].id -eq $old.id -and $edited.lines[0].unit -eq $old.unit -and $edited.lines[0].itemName -eq $old.itemName -and $edited.lines[0].standardCostSnapshot -eq 0) 'Header edit retains line ID, original unit/name/zero-cost despite inactive changed catalog'
    $conflict=Invoke-S006 PUT "/api/app/service-order/$($o.id)" $body -Raw
    Assert-S006 ($conflict.Status -ge 400 -and $conflict.Status -lt 500) 'Stale edit rejects predictably'
    $denied=Invoke-S006 POST '/api/app/service-order' @{CustomerAssetId=$s.Asset.assetId;WarehouseId=$s.Warehouse.id;Note="$p invalid inactive selection";Lines=@(@{LineType=2;CatalogItemId=$s.EdgeWork.id;Quantity=1;UnitPrice=100000})} -Raw
    Assert-S006 ($denied.Status -ge 400 -and $denied.Status -lt 500) 'Inactive work is visible in history but cannot be newly selected'
    $o=Invoke-S006 POST "/api/app/service-order/$($o.id)/confirm" @{ConcurrencyStamp=$edited.concurrencyStamp}
    $direct=Invoke-S006 POST "/api/app/service-order/$($o.id)/complete" (CompleteInput $o 'direct-confirmed') -Raw
    Assert-S006 ($direct.Status -ge 400 -and $direct.Status -lt 500) 'Confirmed cannot complete without Start'
    $s.Orders.editflow=$o;Save;$o=StartOrder 'editflow'
    $sum=Invoke-S006 GET "/api/app/service-payment/$($o.id)/summary"
    $effects=SqlRead 'SELECT (SELECT COUNT(*) FROM AppInventoryTransactions WHERE ReferenceId=@id) AS Stock,(SELECT COUNT(*) FROM AppAssetMaintenanceEvents WHERE SourceId=@id) AS Care' @{id=[Guid]$o.id}
    Assert-S006 ($effects.Rows[0].Stock -eq 0 -and $effects.Rows[0].Care -eq 0 -and $sum.revenue -eq 0) 'Confirm and Start create neither stock/care effects nor revenue'
    $s.Evidence.EditFlow=@{LineId=$old.id;Conflict=$conflict;DirectComplete=$direct;Summary=$sum};Save
}
if(!$s.NoPolicy){$s.NoPolicy=Invoke-S006 POST '/api/app/component' @{Code="${p}_NO_POLICY";Name="$p no policy material";Unit='Cai'};Save}
if(!$s.NoPolicyStock){$s.NoPolicyStock=(SqlRead 'SELECT Id FROM AppStockItems WHERE CatalogItemId=@id' @{id=[Guid]$s.NoPolicy.id}).Rows[0].Id.ToString();Save}
if(!$s.NoPolicyReceipt){$s.NoPolicyReceipt=Invoke-S006 POST '/api/inventory/transactions/receipts' @{WarehouseId=$s.Warehouse.id;IdempotencyKey="${p}_nopolicyreceipt";LotNo="${p}_NOPOLICY";ReceivedAt='2026-09-01';Lines=@(@{StockItemId=$s.NoPolicyStock;Quantity=5;UnitCost=10000})};Save}
if(!$s.EdgeAsset){$s.EdgeAsset=Invoke-S006 POST '/api/app/warranty/external-asset' @{CustomerId=$s.Customer.id;Model="$p eligibility";IdempotencyKey="${p}_eligibility";Positions=@(
    @{PositionCode='MISSING';PositionName='Missing policy';Quantity=1;ComponentId=$s.NoPolicy.id;ReplacementBaselineDate='2026-08-01'},
    @{PositionCode='INACTIVE';PositionName='Will be inactive';Quantity=1;ComponentId=$s.Components['4'].id;ReplacementBaselineDate='2026-08-01'},
    @{PositionCode='UNMAPPED';PositionName='Will be unmapped';Quantity=1;ComponentId=$s.Components['5'].id;ReplacementBaselineDate='2026-08-01'})};Save}
if(!$s.Evidence.Eligibility){
    $asset=Invoke-S006 GET "/api/app/warranty/$($s.EdgeAsset.assetId)/asset-details"
    if(!$s.Orders.eligibility){$s.Orders.eligibility=Invoke-S006 POST '/api/app/service-order' @{CustomerAssetId=$s.EdgeAsset.assetId;WarehouseId=$s.Warehouse.id;Note="$p eligibility";Lines=@($asset.positions|ForEach-Object{@{LineType=1;CatalogItemId=$_.componentId;CustomerAssetComponentId=$_.id;Quantity=1;UnitPrice=100000}})};Save}
    $o=StartOrder 'eligibility'
    $open=SqlRead @'
SELECT r.Id,p.PositionCode FROM AppAssetReplacementReminders r
JOIN AppCustomerAssetComponents p ON p.Id=r.CustomerAssetComponentId
WHERE r.CustomerAssetId=@id AND r.Status=1 AND p.PositionCode IN ('INACTIVE','UNMAPPED')
'@ @{id=[Guid]$asset.id}
    foreach($r in $open.Rows){$null=Invoke-S006 POST "/api/app/warranty/$($r.Id)/skip-reminder" @{IdempotencyKey="${p}_edge_skip_$($r.PositionCode)";Note="$p operator closes old cycle before mapping edit"}}
    $positions=@($asset.positions|Where-Object positionCode -ne 'INACTIVE'|ForEach-Object{if($_.positionCode -eq 'UNMAPPED'){$_.componentId=$null};$_})
    $null=Invoke-S006 PUT "/api/app/warranty/$($asset.id)/external-asset" @{Model=$asset.model;IdempotencyKey="${p}_eligibility-edit";Positions=$positions}
    $s.Evidence.EligibilityBefore=Invoke-S006 GET "/api/app/warranty/$($asset.id)/asset-details";Save
    $result=Invoke-S006 POST "/api/app/service-order/$($o.id)/complete" (CompleteInput $o 'eligibility')
    $state=SqlRead @'
SELECT p.PositionCode,p.Status,p.ComponentId,p.ReplacementBaselineDate,
(SELECT COUNT(*) FROM AppAssetReplacementReminders r WHERE r.CustomerAssetComponentId=p.Id AND r.Status=1) AS Pending
FROM AppCustomerAssetComponents p WHERE p.CustomerAssetId=@id ORDER BY p.PositionCode
'@ @{id=[Guid]$asset.id}
    foreach($r in $state.Rows){Assert-S006 ($r.Pending -eq 0) "$($r.PositionCode) creates no successor"}
    Assert-S006 (($state.Rows|Where-Object PositionCode -eq INACTIVE).Status -eq 5 -and ($state.Rows|Where-Object PositionCode -eq UNMAPPED).Status -eq 3) 'No automatic position activation or mapping'
    $s.Evidence.Eligibility=@{Result=$result;Positions=@($state.Rows|ForEach-Object{@{Code=$_.PositionCode;Status=$_.Status;Pending=$_.Pending}})};Save
}
if(!$s.Evidence.RepeatedMaterial){
    $o=CreateOrder 'repeatmaterial' @(@{LineType=1;CatalogItemId=$s.Components['5'].id;Quantity=1;UnitPrice=100000},@{LineType=1;CatalogItemId=$s.Components['5'].id;Quantity=1;UnitPrice=100000});$o=StartOrder 'repeatmaterial'
    $result=Invoke-S006 POST "/api/app/service-order/$($o.id)/complete" (CompleteInput $o 'repeatmaterial')
    Assert-S006 ($result.actualCost -eq 40000) 'Repeated material lines skip depleted first lot and persist exact second-lot cost'
    $s.Evidence.RepeatedMaterial=$result;Save
}
if(!$s.Evidence.RaceWarrantyComplete){
    $pos=$s.AssetDetails.positions|Where-Object positionCode -eq 'CORE-6'
    $reminder=(SqlRead 'SELECT Id FROM AppAssetReplacementReminders WHERE CustomerAssetComponentId=@id AND Status=1' @{id=[Guid]$pos.id}).Rows[0].Id
    $o=CreateOrder 'warrantyrace' @(@{LineType=1;CatalogItemId=$s.Components['6'].id;CustomerAssetComponentId=$pos.id;Quantity=1;UnitPrice=100000});$o=StartOrder 'warrantyrace'
    $race=Invoke-S006Race "/api/app/service-order/$($o.id)/complete" (CompleteInput $o 'warrantyrace') "/api/app/warranty/$reminder/complete-reminder" @{CompletedAt='2026-09-07';IdempotencyKey="${p}_warrantyrace";Note="$p bypass"}
    Assert-S006 (@($race|Where-Object Status -eq 200).Count -eq 1 -and @($race|Where-Object Status -ge 500).Count -eq 0) 'Service completion versus direct Warranty completion: only Service wins'
    $s.Evidence.RaceWarrantyComplete=$race;Save
}
if(!$s.Evidence.ReenableNoBackfill){
    $before=Invoke-S006 GET "/api/app/warranty/reminder-list?SearchText=$($s.Asset.assetNo)&MaxResultCount=1000"
    $null=Invoke-S006 POST "/api/app/warranty/set-policy/$($s.Components['2'].id)" @{IsEnabled=$true;CycleMonths=3;WarningDaysBeforeDue=7}
    $after=Invoke-S006 GET "/api/app/warranty/reminder-list?SearchText=$($s.Asset.assetNo)&MaxResultCount=1000"
    Assert-S006 (($before|ConvertTo-Json -Depth 20 -Compress) -ceq ($after|ConvertTo-Json -Depth 20 -Compress)) 'Re-enabling policy does not backfill historical reminders'
    $s.Evidence.ReenableNoBackfill=$true;Save
}
$s.EdgeDone=$true;Save
