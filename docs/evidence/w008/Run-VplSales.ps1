param([ValidateSet('Seed', 'Revise')][string]$Phase, [switch]$AllowFixtureWrites)
if (!$AllowFixtureWrites) { throw 'Explicit fixture-write opt-in required.' }
. "$PSScriptRoot/Vpl-UatApi.ps1" -AllowFixtureWrites
$root = (Resolve-Path "$PSScriptRoot/../../..").Path
$statePath = "$root/artifacts/w008-isolated-r2/fixtures.json"
$s = Get-Content $statePath -Raw | ConvertFrom-Json -AsHashtable
if (!$s.Group -or $s.Customer.customerGroupId -ne $s.Group.id) { throw 'Only the fully isolated R2 group/customer may be used.' }
$p = $s.Prefix
function Save-State { $s | ConvertTo-Json -Depth 30 | Set-Content $statePath -Encoding utf8 }
function Assert-Uat($condition, [string]$description) { if (!$condition) { throw "FAIL: $description" }; Write-Output "PASS: $description" }
function Get-Assets([string]$OrderNo) { (Invoke-UatApi GET "/api/app/warranty/asset-list?SearchText=$OrderNo&MaxResultCount=100").items }
function Revision([string]$orderId, [decimal]$quantity, [decimal]$price, [string]$label, [string]$productId = $s.Product.id) {
    $r = Invoke-UatApi POST "/api/sales/orders/$orderId/revisions" @{ Reason = "$p $label" }
    $s["Revision_$label"] = $r; Save-State
    $lineId = $r.lines[0].id
    $r = Invoke-UatApi PUT "/api/sales/revisions/$($r.id)" @{ CustomerId = $s.Customer.id; Lines = @(@{ RevisionLineId = $lineId; ProductId = $productId; Quantity = $quantity; ActualSellingPrice = $price }) }
    $s["Revision_$label"] = $r; Save-State
    if ($label -in 'decrease','replace') {
        Invoke-UatApi POST "/api/sales/revisions/$($r.id)/returned-goods" @{ RevisionLineIds = @($lineId); Reason = "$p synthetic warehouse return" } | Out-Null
    }
    $input = @{ IdempotencyKey = "${p}_apply_$label" }
    Invoke-UatApi POST "/api/sales/revisions/$($r.id)/apply" $input | Out-Null
    Invoke-UatApi POST "/api/sales/revisions/$($r.id)/apply" $input | Out-Null
    return $r
}
if ($Phase -eq 'Seed') {
    if ($s.SalesSeedComplete) { throw 'Seed already complete.' }
    $cfg = Get-Content "$root/src/VPureLux.Web/appsettings.json" -Raw | ConvertFrom-Json
    $c = New-Object System.Data.SqlClient.SqlConnection($cfg.ConnectionStrings.Default)
    try {
        $c.Open(); if ($c.Database -cne 'VPL') { throw 'VPL only.' }
        $q = $c.CreateCommand(); $q.CommandText = 'SELECT Id FROM AppStockItems WHERE CatalogItemId=@id'
        [void]$q.Parameters.AddWithValue('@id', [Guid]$s.Components[0].id)
        $stockId = $q.ExecuteScalar().ToString()
    } finally { $c.Dispose() }
    $s.StockId = $stockId
    $s.Receipt = Invoke-UatApi POST '/api/inventory/transactions/receipts' @{ WarehouseId=$s.Warehouse.id; IdempotencyKey="${p}_receipt"; LotNo="${p}_LOT"; ReceivedAt=[DateTime]::Today.ToString('yyyy-MM-dd'); Reason="$p fixture stock"; Lines=@(@{ StockItemId=$stockId; Quantity=100; UnitCost=10000 }) }
    Save-State
    $s.Orders = @{}
    foreach ($label in @('revision','install','cancelrace','adjustrace')) {
        $o = Invoke-UatApi POST '/api/sales/orders' @{ CustomerId=$s.Customer.id; WarehouseId=$s.Warehouse.id; Lines=@(@{ ProductId=$s.Product.id; Quantity=1; ActualSellingPrice=100000; OverrideReason="$p synthetic price" }) }
        $s.Orders[$label] = $o; Save-State
        Invoke-UatApi POST "/api/sales/orders/$($o.id)/confirm" @{IdempotencyKey="${p}_confirm_$label"} | Out-Null
        $s.Orders[$label] = Invoke-UatApi GET "/api/sales/orders/$($o.id)"; Save-State
        Assert-Uat (@(Get-Assets $s.Orders[$label].orderNo).Count -eq 0) "${label}: confirmation remains decoupled while intake disabled"
    }
    $s.SalesSeedComplete=$true; Save-State; exit
}
if (!$s.SalesSeedComplete) { throw 'Seed first, then enable controlled intake and verify pending assets.' }
if ($s.SalesRevisionComplete) { throw 'Revision UAT already completed.' }
$order = $s.Orders.revision
$before = @(Get-Assets $order.orderNo)
Assert-Uat ($before.Count -eq 1) 'Initial machine intake creates one pending unit'
$originalId = $before[0].id
$null = Revision $order.id 3 100000 'increase'
$increased = @(Get-Assets $order.orderNo)
Assert-Uat ($increased.Count -eq 3) 'Increase and apply replay create exactly three assets'
$originalBefore = Invoke-UatApi GET "/api/app/warranty/$originalId/asset-details"
$null = Revision $order.id 3 110000 'price'
$originalAfter = Invoke-UatApi GET "/api/app/warranty/$originalId/asset-details"
Assert-Uat (($originalBefore|ConvertTo-Json -Depth 20 -Compress) -ceq ($originalAfter|ConvertTo-Json -Depth 20 -Compress)) 'Price-only preserves original machine details and positions'
$null = Revision $order.id 1 110000 'decrease'
$afterDecrease = @(Get-Assets $order.orderNo)
$s.AssetsAfterDecrease=$afterDecrease; Save-State
Assert-Uat ($afterDecrease.Count -eq 3) 'Decrease retains all historical assets (no delete)'
$s.Cancellation = Invoke-UatApi POST "/api/sales/orders/$($order.id)/cancel-confirmed" @{ReasonGroup='Customer'; Reason="$p cancellation before installation"}
Save-State
$s.SalesRevisionComplete=$true; Save-State
Write-Output 'Revision/cancellation API sequence complete; verify statuses and later intake retry read-only.'
