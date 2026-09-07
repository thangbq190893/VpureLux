param([switch]$AllowFixtureWrites,[switch]$SecondLine)
if(!$AllowFixtureWrites){throw 'Explicit fixture opt-in required'}
. "$PSScriptRoot/Run-Uat.ps1" -Phase Helpers -AllowFixtureWrites
if($s.SafetyDone -and !$SecondLine){throw 'Safety phase already complete'}
function CompleteInput($o,[string]$Key,[int]$Quantity){return @{ConcurrencyStamp=$o.concurrencyStamp;IdempotencyKey="${p}_$Key";CompletedAt='2026-09-07T07:00:00+07:00';Lines=@($o.lines|ForEach-Object{@{LineId=$_.id;ActualQuantity=$Quantity}})}}
function Snapshot($o){
    $q=@'
SELECT
(SELECT * FROM AppServiceOrders WHERE Id=@order FOR JSON PATH) AS Orders,
(SELECT * FROM AppServiceOrderLines WHERE ServiceOrderId=@order ORDER BY Id FOR JSON PATH) AS Lines,
(SELECT * FROM AppInventoryTransactions WHERE ReferenceId=@order ORDER BY Id FOR JSON PATH) AS Transactions,
(SELECT * FROM AppInventoryBalances WHERE WarehouseId=@warehouse ORDER BY StockItemId FOR JSON PATH) AS Balances,
(SELECT * FROM AppInventoryLots WHERE WarehouseId=@warehouse ORDER BY Id FOR JSON PATH) AS Lots,
(SELECT * FROM AppCustomerAssetComponents WHERE CustomerAssetId=@asset ORDER BY Id FOR JSON PATH) AS Positions,
(SELECT * FROM AppAssetMaintenanceEvents WHERE CustomerAssetId=@asset ORDER BY Id FOR JSON PATH) AS Events,
(SELECT * FROM AppAssetReplacementReminders WHERE CustomerAssetId=@asset ORDER BY Id FOR JSON PATH) AS Reminders
'@
    $r=SqlRead $q @{order=[Guid]$o.id;warehouse=[Guid]$s.Warehouse.id;asset=[Guid]$s.Asset.assetId}
    $h=@{};foreach($col in $r.Columns){$h[$col.ColumnName]=$r.Rows[0][$col.ColumnName]};return $h
}
$labels=if($SecondLine){@('secondlineshortage')}else{@('shortage','poststockfailure')}
foreach($label in $labels){
    if($s.Evidence["${label}Pass"]){continue}
    $pos=$s.AssetDetails.positions|Where-Object positionCode -eq 'CORE-4'
    $qty=if($label -eq 'shortage'){1000}else{1}
    $lines=@(@{LineType=1;CatalogItemId=$s.Components['4'].id;CustomerAssetComponentId=$pos.id;Quantity=$qty;UnitPrice=100000})
    if($SecondLine){$lines+=@{LineType=1;CatalogItemId=$s.Components['9'].id;Quantity=1;UnitPrice=100000}}
    $o=CreateOrder $label $lines
    $o=StartOrder $label
    if($label -eq 'poststockfailure'){
        $null=Invoke-WebRequest "$script:UatBaseUrl/s006-control/failure/$($o.id)" -Method Post -Headers @{'X-S006-Control'=$script:Auth.Control}
    }
    $before=Snapshot $o;$s.Evidence["${label}Before"]=$before;Save
    $response=Invoke-S006 POST "/api/app/service-order/$($o.id)/complete" (CompleteInput $o $label $qty) -Raw
    $after=Snapshot $o;$s.Evidence["${label}After"]=$after;$s.Evidence["${label}Response"]=$response;Save
    Assert-S006 ($response.Status -ge 400 -and $response.Status -lt 500) "$label predictable rejection"
    if($label -eq 'poststockfailure'){Assert-S006 ($response.Content.Contains('S006:InjectedPostStockFailure')) 'Failure really occurred after stock and CustomerCare SaveChanges'}
    foreach($key in $before.Keys){Assert-S006 ($before[$key] -ceq $after[$key]) "$label $key exact rollback"}
    $s.Evidence["${label}Pass"]=$true;Save
}
if($SecondLine){Write-Host 'SECOND_LINE_SHORTAGE_ROLLBACK_PASS';exit}
$o=$s.Orders.core123
$alloc=SqlRead @'
SELECT l.Id,l.ActualQuantity,l.ActualCostAmount,COUNT(a.Id) AS AllocationCount,
SUM(a.Quantity) AS AllocatedQuantity,SUM(a.Quantity*a.UnitCost) AS AllocatedCost
FROM AppServiceOrderLines l
JOIN AppInventoryLotAllocations a ON a.InventoryTransactionLineId=l.InventoryTransactionLineId
WHERE l.ServiceOrderId=@id AND l.LineType=1
GROUP BY l.Id,l.ActualQuantity,l.ActualCostAmount ORDER BY l.Id
'@ @{id=[Guid]$o.id}
Assert-S006 ($alloc.Rows.Count -eq 3) 'Three performed material lines have factual allocations'
$rows=@();foreach($r in $alloc.Rows){Assert-S006 ($r.AllocationCount -eq 2 -and $r.AllocatedQuantity -eq 2 -and $r.AllocatedCost -eq 30000 -and $r.ActualCostAmount -eq 30000) 'FIFO consumes first 1@10k then 1@20k, exact persisted cost';$rows+=@{Id=$r.Id;Quantity=$r.AllocatedQuantity;Cost=$r.AllocatedCost;Lots=$r.AllocationCount}}
$s.Evidence.MultiLot=$rows
foreach($i in 4..9){$before=$s.Evidence.CoreBefore.positions|Where-Object positionCode -eq "CORE-$i";$after=$s.Evidence.CoreAfter.positions|Where-Object positionCode -eq "CORE-$i";Assert-S006 (($before|ConvertTo-Json -Compress) -ceq ($after|ConvertTo-Json -Compress)) "Unperformed Core $i remains byte-equivalent in DTO"}
Save
$pos=$s.AssetDetails.positions|Where-Object positionCode -eq 'CORE-7'
$o=CreateOrder 'missingbaseline' @(@{LineType=1;CatalogItemId=$s.Components['7'].id;CustomerAssetComponentId=$pos.id;Quantity=1;UnitPrice=100000});$o=StartOrder 'missingbaseline'
$s.Evidence.MissingBaselineComplete=Invoke-S006 POST "/api/app/service-order/$($o.id)/complete" (CompleteInput $o 'missingbaseline' 1);Save
$detail=Invoke-S006 GET "/api/app/warranty/$($s.Asset.assetId)/asset-details"
$position=$detail.positions|Where-Object positionCode -eq 'CORE-7'
Assert-S006 ($position.replacementBaselineDate -like '2026-09-07*' -or ([datetime]$position.replacementBaselineDate).Date -eq [datetime]'2026-09-07') 'MissingBaseline starts actual cycle after performed replacement'
$s.Evidence.MissingBaselineAfter=$position;$s.SafetyDone=$true;Save
