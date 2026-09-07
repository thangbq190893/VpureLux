param([switch]$AllowFixtureWrites)
if(!$AllowFixtureWrites){throw 'Explicit fixture opt-in required'}
. "$PSScriptRoot/Run-Uat.ps1" -Phase Helpers -AllowFixtureWrites
function Rows($t){@($t.Rows|ForEach-Object{$h=@{};foreach($c in $t.Columns){$h[$c.ColumnName]=$(if($_.IsNull($c)){$null}else{$_[$c.ColumnName]})};$h})}
$e=@{}
foreach($kind in @('asset','material','work')){
    $base="/api/app/service-order/$kind-options?SearchText=$p&MaxResultCount=1"
    $a=Invoke-S006 GET $base;$b=Invoke-S006 GET "$base&SkipCount=1"
    Assert-S006 ($a.totalCount -gt 1 -and $b.items.Count -eq 1 -and $a.items[0].id -ne $b.items[0].id) "$kind lookup true page2"
    $e[$kind]=@{Total=$a.totalCount;First=$a.items[0].id;Second=$b.items[0].id}
}
$range='FromDate=2026-01-01&ToDate=2026-12-31'
$all=Invoke-S006 GET "/api/app/business-revenue/consolidated-summary?$range"
$sales=Invoke-S006 GET "/api/app/business-revenue/consolidated-summary?$range&Source=1"
$service=Invoke-S006 GET "/api/app/business-revenue/service-summary?$range"
Assert-S006 ($all.totalRevenue -eq $sales.totalRevenue+$service.totalRevenue) 'Consolidated = Sales + Completed Service'
$facts=SqlRead 'SELECT SUM(TotalRevenueAmount) AS Revenue, COUNT(*) AS Documents FROM AppServiceOrders WHERE Status=4 AND IsDeleted=0'
Assert-S006 ($facts.Rows[0].Revenue -eq $service.totalRevenue -and $facts.Rows[0].Documents -eq $service.documentCount) 'Service recognition matches persisted completed facts only'
$e.Reports=@{All=$all;Sales=$sales;Service=$service}
$a=Invoke-S006 GET "/api/app/business-revenue/consolidated-list?$range&MaxResultCount=1"
$b=Invoke-S006 GET "/api/app/business-revenue/consolidated-list?$range&MaxResultCount=1&SkipCount=1"
Assert-S006 ($a.items[0].documentId -ne $b.items[0].documentId) 'Consolidated true page2'
$browser=Invoke-S006 GET "/api/app/service-payment/$($s.Orders.browser.id)/summary"
Assert-S006 ($browser.grossPosted -eq 1500000 -and $browser.grossRefunded -eq 500000 -and $browser.netPaid -eq 1000000 -and $browser.revenue -eq 1000000) 'Actual browser receipts/void/refund settle 1m recognized charge'
$e.BrowserMoney=$browser
$e.CustomerMoney=Invoke-S006 GET "/api/app/service-payment/customer-summary/$($s.Customer.id)"
Assert-S006 ($e.CustomerMoney.inconsistentOrderCount -eq 0) 'Customer aggregate has no inconsistent ledger'
$legacy=Invoke-WebRequest "$script:UatBaseUrl/Service/Details/e27239d9-37e3-f148-4773-3a234a99d2b7" -WebSession $script:UatSession
Assert-S006 ($legacy.Content.Contains('25/08/2026 17:24') -and !$legacy.Content.Contains('26/08/2026 00:24')) 'Real legacy detail preserves wall-time after fix'
$e.LegacyDetail='25/08/2026 17:24'
if(!$s.Evidence.FeatureTransition){
    $asset=Invoke-S006 POST '/api/app/warranty/external-asset' @{CustomerId=$s.Customer.id;Model="$p transitional";IdempotencyKey="${p}_transitional";Positions=@(@{PositionCode='CORE';PositionName='Core';Quantity=1;ComponentId=$s.Components['3'].id;ReplacementBaselineDate='2026-08-01'})}
    $s.Evidence.TransitionAsset=$asset;Save
    $r=(SqlRead 'SELECT Id FROM AppAssetReplacementReminders WHERE CustomerAssetId=@id AND Status=1' @{id=[Guid]$asset.assetId}).Rows[0].Id
    try{
        Set-S006Service $false
        $page=Invoke-WebRequest "$script:UatBaseUrl/Warranty" -WebSession $script:UatSession
        Assert-S006 (!$page.Content.Contains('href="/Service"') -and !$page.Content.Contains('href="/Service/Works"')) 'Disabled Service menus hidden'
        $result=Invoke-S006 POST "/api/app/warranty/$r/complete-reminder" @{CompletedAt='2026-09-07';IdempotencyKey="${p}_transitionalcomplete";Note="$p transitional completion"}
        $s.Evidence.FeatureTransition=@{DisabledCompletion=$result;AssetId=$asset.assetId};Save
    }finally{Set-S006Service $true}
    $r=(SqlRead 'SELECT Id FROM AppAssetReplacementReminders WHERE CustomerAssetId=@id AND Status=1' @{id=[Guid]$asset.assetId}).Rows[0].Id
    $null=Invoke-S006 POST "/api/app/warranty/$r/skip-reminder" @{IdempotencyKey="${p}_transitionalskip";Note="$p permitted care action while Service enabled"}
}
$orphans=SqlRead @'
SELECT
(SELECT COUNT(*) FROM AppServiceOrderLines l LEFT JOIN AppServiceOrders o ON o.Id=l.ServiceOrderId WHERE o.Id IS NULL) AS OrphanLines,
(SELECT COUNT(*) FROM AppServicePayments p LEFT JOIN AppServiceOrders o ON o.Id=p.ServiceOrderId WHERE o.Id IS NULL) AS OrphanPayments,
(SELECT COUNT(*) FROM AppServiceRefunds r LEFT JOIN AppServiceOrders o ON o.Id=r.ServiceOrderId WHERE o.Id IS NULL) AS OrphanRefunds,
(SELECT COUNT(*) FROM AppInventoryLotAllocations a LEFT JOIN AppInventoryLots l ON l.Id=a.InventoryLotId WHERE l.Id IS NULL) AS OrphanAllocations,
(SELECT COUNT(*) FROM AppAssetReplacementReminders r LEFT JOIN AppCustomerAssets a ON a.Id=r.CustomerAssetId WHERE a.Id IS NULL) AS OrphanReminders
'@
foreach($c in $orphans.Columns){Assert-S006 ($orphans.Rows[0][$c] -eq 0) $c.ColumnName}
$e.Orphans=Rows $orphans
$e.Inventory=Rows (SqlRead @'
SELECT b.StockItemId,b.QuantityOnHand AS Quantity,b.InventoryValue AS TotalValue,SUM(l.AvailableQuantity) AS LotAvailable
FROM AppInventoryBalances b JOIN AppInventoryLots l ON l.WarehouseId=b.WarehouseId AND l.StockItemId=b.StockItemId
WHERE b.WarehouseId=@id GROUP BY b.StockItemId,b.QuantityOnHand,b.InventoryValue
'@ @{id=[Guid]$s.Warehouse.id})
foreach($r in $e.Inventory){Assert-S006 ($r.Quantity -ge 0 -and $r.Quantity -eq $r.LotAvailable) 'Fixture balance reconciles lots without negative stock'}
$s.Evidence.FinalChecks=$e;$s.FinalChecksDone=$true;Save
Write-Host 'FINAL_CHECKS_PASS'
