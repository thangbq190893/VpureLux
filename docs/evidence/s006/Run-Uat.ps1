param([ValidateSet('Seed','Money','Core','Helpers')][string]$Phase,[switch]$AllowFixtureWrites)
if(!$AllowFixtureWrites){throw 'Explicit S006 fixture opt-in required'}
. "$PSScriptRoot/UatApi.ps1" -AllowFixtureWrites
$path="$script:S006Root/artifacts/s006/fixtures.json"
$s=if(Test-Path $path){Get-Content $path -Raw|ConvertFrom-Json -AsHashtable}else{@{Prefix='UATSVC_20260907';Orders=@{};Evidence=@{}}}
$p=$s.Prefix
if($p -cne 'UATSVC_20260907'){throw 'Wrong fixture namespace'}
function Save { $s|ConvertTo-Json -Depth 60|Set-Content $path -Encoding utf8 }
function SqlRead([string]$Query,$Ids=@{}){
    $cfg=Get-Content "$script:S006Root/src/VPureLux.Web/appsettings.json" -Raw|ConvertFrom-Json
    $b=[System.Data.SqlClient.SqlConnectionStringBuilder]::new($cfg.ConnectionStrings.Default)
    if($b.InitialCatalog -cne 'VPL'){throw 'STOP: VPL configuration required'}
    $c=[System.Data.SqlClient.SqlConnection]::new($b.ConnectionString)
    try{$c.Open();$q=$c.CreateCommand();$q.CommandText='SELECT DB_NAME()';if($q.ExecuteScalar() -cne 'VPL'){throw 'STOP target'}
        $q.CommandText=$Query;foreach($k in $Ids.Keys){[void]$q.Parameters.AddWithValue("@$k",$Ids[$k])}
        $t=[Data.DataTable]::new();$t.Load($q.ExecuteReader());return ,$t
    }finally{$c.Dispose()}
}
$proof=SqlRead 'SELECT @@SERVERNAME AS ServerName, DB_NAME() AS DatabaseName'
Write-Host "Target: $($proof.Rows[0].ServerName) / $($proof.Rows[0].DatabaseName)"
function CreateOrder([string]$Label,$Lines){
    if(!$s.Orders[$Label]){
        $s.Orders[$Label]=Invoke-S006 POST '/api/app/service-order' @{CustomerAssetId=$s.Asset.assetId;WarehouseId=$s.Warehouse.id;Note="$p $Label";Lines=@($Lines)};Save
    };return $s.Orders[$Label]
}
function StartOrder([string]$Label){
    $o=Invoke-S006 GET "/api/app/service-order/$($s.Orders[$Label].id)"
    if($o.status -eq 1){$o=Invoke-S006 POST "/api/app/service-order/$($o.id)/confirm" @{ConcurrencyStamp=$o.concurrencyStamp}}
    if($o.status -eq 2){$o=Invoke-S006 POST "/api/app/service-order/$($o.id)/start" @{ConcurrencyStamp=$o.concurrencyStamp}}
    $s.Orders[$Label]=$o;Save;return $o
}
function PayBody([decimal]$Amount,[string]$Key){return @{Amount=$Amount;PaymentDate='2026-09-07T10:00:00+07:00';PaymentMethod=1;IdempotencyKey="${p}_$Key";ReferenceNo="${p}_$Key";Note="$p factual receipt"}}
function RefundBody([decimal]$Amount,[string]$Key){return @{Amount=$Amount;RefundDate='2026-09-07T11:00:00+07:00';Method=1;IdempotencyKey="${p}_$Key";ReferenceNo="${p}_$Key";Reason="$p actual refund"}}
Set-S006Service $true
if($Phase -eq 'Seed'){
    if($s.SeedDone){throw 'Seed already complete'}
    Set-S006Service $false
    $denied=Invoke-S006 POST '/api/app/service-work' @{Code="${p}_NEVER";Name="$p disabled";Unit='Lan'} -Raw
    Assert-S006 ($denied.Status -eq 403 -and $denied.Content.Contains('SERVICE_009')) 'Service disabled blocks authenticated permitted mutation'
    $s.Evidence.FeatureDisabled=$denied;Save;Set-S006Service $true
    if(!$s.Group){$s.Group=Invoke-S006 POST '/api/customer-groups' @{Code="${p}_G";Name="$p isolated group"};Save}
    if(!$s.Customer){$s.Customer=Invoke-S006 POST '/api/customers' @{Code="${p}_C";Name="$p customer";CustomerGroupId=$s.Group.id;Notes="$p fixture"};Save}
    if(!$s.Warehouse){$s.Warehouse=Invoke-S006 POST '/api/inventory/warehouses' @{Code="${p}_W";Name="$p warehouse";IsDefault=$false};Save}
    if(!$s.Components){$s.Components=@{}}
    for($i=1;$i -le 9;$i++){
        if(!$s.Components["$i"]){$s.Components["$i"]=Invoke-S006 POST '/api/app/component' @{Code="${p}_CORE$i";Name="$p Core $i";Unit='Cai';ReplacementPolicy=@{IsEnabled=$true;CycleMonths=3;WarningDaysBeforeDue=7}};Save}
    }
    foreach($label in @('Unknown','Zero','Known')){
        if(!$s[$label]){$cost=switch($label){Unknown{$null};Zero{0};Known{20000}}
            $s[$label]=Invoke-S006 POST '/api/app/service-work' @{Code="${p}_$label";Name="$p labor $label";Unit='Lan';DefaultPrice=100000;StandardCost=$cost;Status=1};Save}
    }
    if(!$s.Asset){
        $positions=@(1..9|ForEach-Object{@{PositionCode="CORE-$_";PositionName="Core $_";Quantity=1;ComponentId=$s.Components["$_"].id;ReplacementBaselineDate=$(if($_ -le 6){'2026-08-01'}else{$null})}})
        $s.Asset=Invoke-S006 POST '/api/app/warranty/external-asset' @{CustomerId=$s.Customer.id;Model="$p external machine";SerialNo="${p}_MACHINE";IdempotencyKey="${p}_asset";Positions=$positions};Save
    }
    $s.AssetDetails=Invoke-S006 GET "/api/app/warranty/$($s.Asset.assetId)/asset-details";Save
    $s.Stock=@{};foreach($i in 1..9){$rows=SqlRead 'SELECT Id FROM AppStockItems WHERE CatalogItemId=@id' @{id=[Guid]$s.Components["$i"].id};Assert-S006 ($rows.Rows.Count -eq 1) "Core $i has one stock identity";$s.Stock["$i"]=$rows.Rows[0].Id.ToString()};Save
    if(!$s.Receipts){$s.Receipts=@{}}
    foreach($lot in 1..2){if(!$s.Receipts["$lot"]){$qty=if($lot -eq 1){1}else{20};$cost=if($lot -eq 1){10000}else{20000}
        $s.Receipts["$lot"]=Invoke-S006 POST '/api/inventory/transactions/receipts' @{WarehouseId=$s.Warehouse.id;IdempotencyKey="${p}_receipt$lot";LotNo="${p}_LOT$lot";ReceivedAt="2026-09-0$lot";Lines=@(1..9|ForEach-Object{@{StockItemId=$s.Stock["$_"];Quantity=$qty;UnitCost=$cost}})};Save}}
    $s.Evidence.RemindersBefore=Invoke-S006 GET "/api/app/warranty/reminder-list?CustomerAssetId=$($s.Asset.assetId)&MaxResultCount=20";Save
    $s.SeedDone=$true;Save;Write-Host 'SEED PASS';exit
}
if(!$s.SeedDone){throw 'Seed must complete first'}
if($Phase -eq 'Money'){
    if($s.MoneyDone){throw 'Money phase already complete'}
    $o=CreateOrder 'advance' @(@{LineType=2;CatalogItemId=$s.Known.id;Quantity=10;UnitPrice=1000000})
    $o=StartOrder 'advance'
    $body=PayBody 8000000 'advance8'
    $payment=Invoke-S006 POST "/api/app/service-payment/$($o.id)/payment" $body
    $replay=Invoke-S006 POST "/api/app/service-payment/$($o.id)/payment" $body
    Assert-S006 ($payment.id -eq $replay.id) 'Payment exact replay returns same row'
    $sum=Invoke-S006 GET "/api/app/service-payment/$($o.id)/summary"
    Assert-S006 ($sum.advancePaid -eq 8000000 -and $sum.revenue -eq 0 -and $sum.plannedRemaining -eq 2000000) 'Before completion: advance 8m, planned remaining 2m, revenue zero'
    $s.Evidence.Advance=$sum;Save
    $bad=PayBody 8000001 'advance8';$r=Invoke-S006 POST "/api/app/service-payment/$($o.id)/payment" $bad -Raw
    Assert-S006 ($r.Status -ge 400 -and $r.Status -lt 500) 'Same payment key different payload conflicts'
    $over=Invoke-S006 POST "/api/app/service-payment/$($o.id)/payment" (PayBody 3000000 'overpay') -Raw
    Assert-S006 ($over.Status -ge 400 -and $over.Status -lt 500) 'Advance cannot exceed planned obligation'
    $o=Invoke-S006 GET "/api/app/service-order/$($o.id)"
    $complete=@{ConcurrencyStamp=$o.concurrencyStamp;IdempotencyKey="${p}_advancecomplete";CompletedAt='2026-09-07T00:30:00+07:00';Lines=@(@{LineId=$o.lines[0].id;ActualQuantity=6})}
    $s.Evidence.AdvanceCompletion=Invoke-S006 POST "/api/app/service-order/$($o.id)/complete" $complete;Save
    $sum=Invoke-S006 GET "/api/app/service-payment/$($o.id)/summary"
    Assert-S006 ($sum.revenue -eq 6000000 -and $sum.refundDue -eq 2000000 -and $sum.grossPosted -eq 8000000) 'Actual 6m leaves original 8m posted and 2m refund obligation'
    $s.Evidence.BeforeRefund=$sum;Save
    foreach($i in 1..2){$r=Invoke-S006 POST "/api/app/service-payment/$($o.id)/refund" (RefundBody 1000000 "refund$i");$rr=Invoke-S006 POST "/api/app/service-payment/$($o.id)/refund" (RefundBody 1000000 "refund$i");Assert-S006 ($r.id -eq $rr.id) "Refund $i exact replay"}
    $sum=Invoke-S006 GET "/api/app/service-payment/$($o.id)/summary"
    Assert-S006 ($sum.netPaid -eq 6000000 -and $sum.refundDue -eq 0 -and $sum.revenue -eq 6000000) 'Partial refunds settle credit without changing revenue'
    $page=Invoke-S006 GET "/api/app/service-payment/refund-list?ServiceOrderId=$($o.id)&MaxResultCount=1&SkipCount=1"
    Assert-S006 ($page.totalCount -eq 2 -and $page.items.Count -eq 1) 'Refund history page 2'
    $s.Evidence.AfterRefund=$sum;Save
    $o=CreateOrder 'cancelmoney' @(@{LineType=2;CatalogItemId=$s.Known.id;Quantity=5;UnitPrice=1000000});$o=StartOrder 'cancelmoney'
    $s.Evidence.CancelPayment=Invoke-S006 POST "/api/app/service-payment/$($o.id)/payment" (PayBody 5000000 'cancel5');Save
    $o=Invoke-S006 GET "/api/app/service-order/$($o.id)"
    $null=Invoke-S006 POST "/api/app/service-order/$($o.id)/cancel" @{ConcurrencyStamp=$o.concurrencyStamp;Reason="$p customer cancelled"}
    $sum=Invoke-S006 GET "/api/app/service-payment/$($o.id)/summary"
    Assert-S006 ($sum.status -eq 5 -and $sum.grossPosted -eq 5000000 -and $sum.refundDue -eq 5000000 -and $sum.revenue -eq 0) 'Cancellation preserves factual advance; no automatic refund or void'
    $s.Evidence.CancelMoney=$sum;Save
    $null=Invoke-S006 POST "/api/app/service-payment/$($o.id)/refund" (RefundBody 5000000 'cancelrefund')
    $o=CreateOrder 'void' @(@{LineType=2;CatalogItemId=$s.Zero.id;Quantity=10;UnitPrice=100000})
    $b=PayBody 500000 'voidreceipt';$pay=Invoke-S006 POST "/api/app/service-payment/$($o.id)/payment" $b
    $v=@{PaymentId=$pay.id;ConcurrencyStamp=$pay.concurrencyStamp;Reason="$p wrong receipt";IdempotencyKey="${p}_void"}
    $void=Invoke-S006 POST "/api/app/service-payment/$($o.id)/void-payment" $v
    $replay=Invoke-S006 POST "/api/app/service-payment/$($o.id)/payment" $b
    Assert-S006 ($void.status -eq 2 -and $replay.status -eq 2 -and $void.voidReason -eq $v.Reason) 'Void has explicit reason; original payment replay never reposts money'
    $s.Evidence.Void=$void;$s.MoneyDone=$true;Save;Write-Host 'MONEY PASS';exit
}
if($Phase -eq 'Core'){
    if($s.CoreDone){throw 'Core already complete'}
    $details=Invoke-S006 GET "/api/app/warranty/$($s.Asset.assetId)/asset-details"
    $lines=@(1..3|ForEach-Object{$i=$_;$pos=$details.positions|Where-Object positionCode -eq "CORE-$i";@{LineType=1;CatalogItemId=$s.Components["$i"].id;CustomerAssetComponentId=$pos.id;Quantity=2;UnitPrice=100000}})
    $lines+=@{LineType=2;CatalogItemId=$s.Unknown.id;Quantity=1;UnitPrice=50000}
    $o=CreateOrder 'core123' $lines;$o=StartOrder 'core123'
    $s.Evidence.CoreBefore=Invoke-S006 GET "/api/app/warranty/$($s.Asset.assetId)/asset-details";Save
    $body=@{ConcurrencyStamp=$o.concurrencyStamp;IdempotencyKey="${p}_core123";CompletedAt='2026-09-07T01:30:00+07:00';Lines=@($o.lines|ForEach-Object{@{LineId=$_.id;ActualQuantity=$_.plannedQuantity}})}
    $s.Evidence.CoreComplete=Invoke-S006 POST "/api/app/service-order/$($o.id)/complete" $body;Save
    $replay=Invoke-S006 POST "/api/app/service-order/$($o.id)/complete" $body
    Assert-S006 ($replay.inventoryTransactionId -eq $s.Evidence.CoreComplete.inventoryTransactionId) 'Core completion replay does not create another stock issue'
    $s.Evidence.CoreAfter=Invoke-S006 GET "/api/app/warranty/$($s.Asset.assetId)/asset-details";Save
    Assert-S006 ($s.Evidence.CoreComplete.revenue -eq 650000 -and $null -eq $s.Evidence.CoreComplete.actualProfit) 'Material plus unknown labor cost leaves profit unknown'
    $s.CoreDone=$true;Save;Write-Host 'CORE PASS'
}
