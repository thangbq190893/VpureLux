param([switch]$AllowFixtureWrites)
if(!$AllowFixtureWrites){throw 'Explicit fixture opt-in required'}
. "$PSScriptRoot/Run-Uat.ps1" -Phase Helpers -AllowFixtureWrites
if($s.RacesDone){throw 'Races already complete; retain original evidence'}
if(!$s.Races){$s.Races=@{}}
if(!$s.RaceSummaries){$s.RaceSummaries=@{}}
function Completion($o,[string]$Key,[int]$Qty=1){return @{ConcurrencyStamp=$o.concurrencyStamp;IdempotencyKey="${p}_$Key";CompletedAt='2026-09-07T07:00:00+07:00';Lines=@($o.lines|ForEach-Object{@{LineId=$_.id;ActualQuantity=$Qty}})}}
function RecordRace([string]$Label,$Outcomes){
    $s.Races[$Label]=$Outcomes;Save
    Assert-S006 (@($Outcomes|Where-Object Status -ge 500).Count -eq 0) "$Label no HTTP 500"
}
$labor=@(@{LineType=2;CatalogItemId=$s.Known.id;Quantity=5;UnitPrice=1000000})
foreach($label in @('twoPayments','paymentCancel','paymentComplete','twoRefunds','twoCompletes','completeCancel')){
    if($s.Races[$label]){continue}
    $o=CreateOrder $label $labor;$o=StartOrder $label
    switch($label){
        twoPayments{
            $r=Invoke-S006Race "/api/app/service-payment/$($o.id)/payment" (PayBody 4000000 'racepayA') "/api/app/service-payment/$($o.id)/payment" (PayBody 4000000 'racepayB')
            RecordRace $label $r;$sum=Invoke-S006 GET "/api/app/service-payment/$($o.id)/summary"
            Assert-S006 ($sum.grossPosted -eq 4000000 -and @($r|Where-Object Status -eq 200).Count -eq 1) 'Two 4m payments with 5m cap: exactly one accepted'
        }
        paymentCancel{
            $r=Invoke-S006Race "/api/app/service-payment/$($o.id)/payment" (PayBody 4000000 'racecancel') "/api/app/service-order/$($o.id)/cancel" @{ConcurrencyStamp=$o.concurrencyStamp;Reason="$p race cancel"}
            RecordRace $label $r;$sum=Invoke-S006 GET "/api/app/service-payment/$($o.id)/summary"
            Assert-S006 ($sum.status -eq 5 -and $sum.refundDue -eq $sum.grossPosted -and $sum.grossPosted -in 0,4000000) 'Payment/cancel ordering preserves cash and refund obligation'
        }
        paymentComplete{
            $r=Invoke-S006Race "/api/app/service-payment/$($o.id)/payment" (PayBody 4000000 'racecomplete') "/api/app/service-order/$($o.id)/complete" (Completion $o 'racecomplete' 3)
            RecordRace $label $r;$sum=Invoke-S006 GET "/api/app/service-payment/$($o.id)/summary"
            Assert-S006 ($sum.status -eq 4 -and $sum.revenue -eq 3000000 -and $sum.grossPosted -in 0,4000000 -and $sum.refundDue -eq [Math]::Max($sum.grossPosted-3000000,0)) 'Payment/complete uses authoritative obligation and no lost advance'
        }
        twoRefunds{
            $null=Invoke-S006 POST "/api/app/service-payment/$($o.id)/payment" (PayBody 5000000 'racerefundseed')
            $null=Invoke-S006 POST "/api/app/service-order/$($o.id)/cancel" @{ConcurrencyStamp=$o.concurrencyStamp;Reason="$p race refunds"}
            $r=Invoke-S006Race "/api/app/service-payment/$($o.id)/refund" (RefundBody 4000000 'raceRefundA') "/api/app/service-payment/$($o.id)/refund" (RefundBody 4000000 'raceRefundB')
            RecordRace $label $r;$sum=Invoke-S006 GET "/api/app/service-payment/$($o.id)/summary"
            Assert-S006 ($sum.grossRefunded -eq 4000000 -and $sum.refundDue -eq 1000000) 'Two refunds cannot exceed remaining credit'
        }
        twoCompletes{
            $body=Completion $o 'doublecomplete'
            $r=Invoke-S006Race "/api/app/service-order/$($o.id)/complete" $body "/api/app/service-order/$($o.id)/complete" $body
            RecordRace $label $r
            Assert-S006 (@($r|Where-Object Status -eq 200).Count -eq 2) 'Same completion key concurrent replay succeeds without duplication'
        }
        completeCancel{
            $r=Invoke-S006Race "/api/app/service-order/$($o.id)/complete" (Completion $o 'completecancel') "/api/app/service-order/$($o.id)/cancel" @{ConcurrencyStamp=$o.concurrencyStamp;Reason="$p complete/cancel"}
            RecordRace $label $r
            Assert-S006 (@($r|Where-Object Status -eq 200).Count -eq 1) 'Complete/cancel: only one terminal transition wins'
        }
    }
    $s.RaceSummaries[$label]=Invoke-S006 GET "/api/app/service-payment/$($o.id)/summary";Save
}
$s.RacesDone=$true;Save
