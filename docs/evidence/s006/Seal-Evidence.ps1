$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../../..").Path
$dir=Join-Path $root 'artifacts/s006'
$s=Get-Content "$dir/fixtures.json" -Raw|ConvertFrom-Json -AsHashtable
$reconciliation=Get-Content "$dir/vpl-final-reconciliation.json" -Raw|ConvertFrom-Json
if($reconciliation.Gate -ne 'PASS'){throw 'Cannot seal failed legacy reconciliation'}
$e=[ordered]@{
    Baseline='0a7eb72';ApplicationSource='c2663ac';Database='VPL';SqlEndpoint='180.93.99.150'
    ProductionAccess=$false;Prefix=$s.Prefix
    CustomerId=$s.Customer.id;GroupId=$s.Group.id;WarehouseId=$s.Warehouse.id;Asset=$s.Asset
    Orders=@($s.Orders.GetEnumerator()|Sort-Object Name|ForEach-Object{@{Label=$_.Key;Id=$_.Value.id;OrderNo=$_.Value.orderNo}})
    Components=@($s.Components.GetEnumerator()|Sort-Object Name|ForEach-Object{@{Core=$_.Key;Id=$_.Value.id;Code=$_.Value.code}})
    OtherAssets=@($s.EdgeAsset,$s.Evidence.TransitionAsset,$s.OtherAsset)
    ProbeOrderIds=$s.Evidence.ProbeOrderIds
    ProbeWorkIds=@($s.Evidence.ProbeWorks|ForEach-Object id)
    TimeCases=$s.TimeCases
    CoreCompletion=$s.Evidence.CoreComplete;MultiLot=$s.Evidence.MultiLot
    Money=@{BeforeRefund=$s.Evidence.BeforeRefund;AfterRefund=$s.Evidence.AfterRefund;Cancel=$s.Evidence.CancelMoney;Browser=$s.Evidence.FinalChecks.BrowserMoney;Customer=$s.Evidence.FinalChecks.CustomerMoney}
    Legacy=$reconciliation
    Reports=$s.Evidence.FinalChecks.Reports
    Inventory=$s.Evidence.FinalChecks.Inventory
    Orphans=$s.Evidence.FinalChecks.Orphans
    Flags=@($s.GetEnumerator()|Where-Object Key -like '*Done'|ForEach-Object{@{Phase=$_.Key;Done=$_.Value}})
    Rollback=@{Shortage=$s.Evidence.shortagePass;SecondLineShortage=$s.Evidence.secondlineshortagePass;AfterStockAndCareSave=$s.Evidence.poststockfailurePass}
    SnapshotImmutability=$s.Evidence.FinalSnapshotPass
    Publish=Get-Content "$dir/publish-verification.json" -Raw|ConvertFrom-Json
    FinalOrderCashFacts=Get-Content "$dir/final-order-cash-facts.json" -Raw|ConvertFrom-Json
    Artifacts=@()
}
$raceGroups=@{};foreach($k in $s.Races.Keys){$raceGroups[$k]=$s.Races[$k]}
$raceGroups.Stock=$s.Evidence.LastStockRace;$raceGroups.Position=$s.Evidence.PositionRace;$raceGroups.Warranty=$s.Evidence.RaceWarrantyComplete
$e.Races=@(foreach($k in $raceGroups.Keys){@{Case=$k;Results=@($raceGroups[$k]|ForEach-Object{$body=try{$_.Content|ConvertFrom-Json}catch{$null};@{Status=$_.Status;BusinessCode=$body.error.code;Id=$body.id;ServiceOrderId=$body.serviceOrderId}})}})
foreach($file in Get-ChildItem $dir -File -Recurse|Where-Object{$_.Extension -in '.json','.trx','.sql','.sqlplan','.png','.log','.txt' -and $_.Name -notmatch 'auth|options' -and $_.FullName -notmatch '\\(tools|publish|Runtime)\\'}){
    $e.Artifacts+=@{Path=[IO.Path]::GetRelativePath($root,$file.FullName).Replace('\','/');Sha256=(Get-FileHash $file.FullName -Algorithm SHA256).Hash;Bytes=$file.Length}
}
$e|ConvertTo-Json -Depth 35|Set-Content "$PSScriptRoot/verification-evidence.json" -Encoding utf8
Write-Host "Evidence sealed: $($e.Orders.Count) known fixture orders, $($e.Artifacts.Count) local evidence hashes; auth/cookies excluded"
