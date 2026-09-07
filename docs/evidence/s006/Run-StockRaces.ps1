param([switch]$AllowFixtureWrites)
if(!$AllowFixtureWrites){throw 'Explicit fixture opt-in required'}
. "$PSScriptRoot/Run-Uat.ps1" -Phase Helpers -AllowFixtureWrites
function CompleteInput($o,[string]$Key){return @{ConcurrencyStamp=$o.concurrencyStamp;IdempotencyKey="${p}_$Key";CompletedAt='2026-09-07T07:00:00+07:00';Lines=@($o.lines|ForEach-Object{@{LineId=$_.id;ActualQuantity=$_.plannedQuantity}})}}
if(!$s.Evidence.LastStockRace){
    if(!$s.OtherAsset){$s.OtherAsset=Invoke-S006 POST '/api/app/warranty/external-asset' @{CustomerId=$s.Customer.id;Model="$p second machine";IdempotencyKey="${p}_otherasset";Positions=@(@{PositionCode='UNMAPPED';PositionName='Unknown';Quantity=1})};Save}
    $a=CreateOrder 'stockA' @(@{LineType=1;CatalogItemId=$s.Components['9'].id;Quantity=21;UnitPrice=100000});$a=StartOrder 'stockA'
    if(!$s.Orders.stockB){$s.Orders.stockB=Invoke-S006 POST '/api/app/service-order' @{CustomerAssetId=$s.OtherAsset.assetId;WarehouseId=$s.Warehouse.id;Note="$p stockB";Lines=@(@{LineType=1;CatalogItemId=$s.Components['9'].id;Quantity=21;UnitPrice=100000})};Save}
    $b=StartOrder 'stockB'
    $r=Invoke-S006Race "/api/app/service-order/$($a.id)/complete" (CompleteInput $a 'stockA') "/api/app/service-order/$($b.id)/complete" (CompleteInput $b 'stockB')
    $s.Evidence.LastStockRace=$r;Save
    Assert-S006 (@($r|Where-Object Status -eq 200).Count -eq 1 -and @($r|Where-Object Status -ge 500).Count -eq 0) 'Different machines compete for same final stock: one successful completion, no 500'
    $balance=SqlRead 'SELECT QuantityOnHand,InventoryValue FROM AppInventoryBalances WHERE WarehouseId=@w AND StockItemId=@s' @{w=[Guid]$s.Warehouse.id;s=[Guid]$s.Stock['9']}
    Assert-S006 ($balance.Rows[0].QuantityOnHand -eq 0 -and $balance.Rows[0].InventoryValue -eq 0) 'Final stock race cannot over-allocate or leave residual cost'
}
if(!$s.Evidence.PositionRace){
    $pos=$s.AssetDetails.positions|Where-Object positionCode -eq 'CORE-6'
    $line=@(@{LineType=1;CatalogItemId=$s.Components['6'].id;CustomerAssetComponentId=$pos.id;Quantity=1;UnitPrice=100000})
    $a=CreateOrder 'positionA' $line;$a=StartOrder 'positionA';$b=CreateOrder 'positionB' $line;$b=StartOrder 'positionB'
    $r=Invoke-S006Race "/api/app/service-order/$($a.id)/complete" (CompleteInput $a 'positionA') "/api/app/service-order/$($b.id)/complete" (CompleteInput $b 'positionB')
    $s.Evidence.PositionRace=$r;Save
    Assert-S006 (@($r|Where-Object Status -ge 500).Count -eq 0) 'Same position concurrent completions have no 500'
    $pending=SqlRead 'SELECT COUNT(*) AS N FROM AppAssetReplacementReminders WHERE CustomerAssetComponentId=@id AND Status=1' @{id=[Guid]$pos.id}
    Assert-S006 ($pending.Rows[0].N -eq 1) 'One pending successor remains for same position'
    $reminder=SqlRead 'SELECT Id FROM AppAssetReplacementReminders WHERE CustomerAssetComponentId=@id AND Status=1' @{id=[Guid]$pos.id}
    $bypass=Invoke-S006 POST "/api/app/warranty/$($reminder.Rows[0].Id)/complete-reminder" @{CompletedAt='2026-09-07';IdempotencyKey="${p}_bypass";Note="$p bypass attempt"} -Raw
    Assert-S006 ($bypass.Status -ge 400 -and $bypass.Status -lt 500) 'Warranty direct completion cannot bypass Service stock/cash workflow'
    $s.Evidence.WarrantyBypass=$bypass;Save
}
