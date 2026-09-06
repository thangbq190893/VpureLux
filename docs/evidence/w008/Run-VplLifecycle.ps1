param([switch]$AllowFixtureWrites)
if (!$AllowFixtureWrites) { throw 'Explicit fixture-write opt-in required.' }
. "$PSScriptRoot/Vpl-UatApi.ps1" -AllowFixtureWrites
$root = (Resolve-Path "$PSScriptRoot/../../..").Path
$statePath = "$root/artifacts/w008-isolated-r2/fixtures.json"
$s = Get-Content $statePath -Raw | ConvertFrom-Json -AsHashtable
if (!$s.Group -or !$s.SalesSeedComplete) { throw 'Requires fully isolated R2 fixtures.' }
if ($s.LifecycleComplete) { throw 'Already complete; do not repeat with new keys.' }
$p = $s.Prefix
function Save-State { $s | ConvertTo-Json -Depth 30 | Set-Content $statePath -Encoding utf8 }
function Assert-Uat($condition, [string]$description) { if (!$condition) { throw "FAIL: $description" }; Write-Host "PASS: $description" }
function Installation([string]$label) {
    $pending = Invoke-UatApi GET "/api/app/warranty/pending-installation-list?SearchText=$($s.Orders[$label].orderNo)&MaxResultCount=10"
    Assert-Uat ($pending.totalCount -eq 1) "$label has exactly one pending machine"
    $id = $pending.items[0].id
    $editor = Invoke-UatApi GET "/api/app/warranty/$id/installation-editor"
    $input = @{InstalledAt=[DateTime]::Today.ToString('yyyy-MM-dd'); SerialNo="${p}_$label"; InstallationAddress="$p synthetic address"; IdempotencyKey="${p}_install_$label"; Positions=$editor.positions}
    return @{Id=$id; Input=$input}
}
# Use independent HTTP requests, sharing only authenticated cookies, to exercise SQL/Redis coordination.
function Race([string]$leftPath, $leftBody, [string]$rightPath, $rightBody) {
    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.CookieContainer = $script:UatSession.Cookies
    $client = [System.Net.Http.HttpClient]::new($handler)
    $token = $script:UatSession.Cookies.GetCookies([uri]$script:UatBaseUrl)['XSRF-TOKEN']
    if ($token) { $client.DefaultRequestHeaders.Add('RequestVerificationToken', [uri]::UnescapeDataString($token.Value)) }
    $left = [System.Net.Http.StringContent]::new(($leftBody|ConvertTo-Json -Depth 20 -Compress),[Text.Encoding]::UTF8,'application/json')
    $right = [System.Net.Http.StringContent]::new(($rightBody|ConvertTo-Json -Depth 20 -Compress),[Text.Encoding]::UTF8,'application/json')
    try {
        $first = $client.PostAsync("$script:UatBaseUrl$leftPath",$left)
        $second = $client.PostAsync("$script:UatBaseUrl$rightPath",$right)
        [System.Threading.Tasks.Task]::WaitAll([System.Threading.Tasks.Task[]]@($first,$second))
        $results = @($first,$second) | ForEach-Object { @{Status=[int]$_.Result.StatusCode; Content=$_.Result.Content.ReadAsStringAsync().GetAwaiter().GetResult()} }
        return ,@($results)
    } finally { $left.Dispose(); $right.Dispose(); $client.Dispose() }
}
$install = Installation 'install'
$s.InstalledAsset = $install
Save-State
$result = Invoke-UatApi POST "/api/app/warranty/$($install.Id)/confirm-installation" $install.Input
$replay = Invoke-UatApi POST "/api/app/warranty/$($install.Id)/confirm-installation" $install.Input
Assert-Uat ($result.createdReminderCount -eq 1 -and $replay.isReplay -and $replay.createdReminderCount -eq 1) 'Installation creates one schedule; replay creates no duplicate'
$s.InstallationResult=$result; Save-State
foreach ($label in @('cancelrace','adjustrace')) {
    $raceInstall=Installation $label
    $rightPath=if($label -eq 'cancelrace'){"/api/sales/orders/$($s.Orders[$label].id)/cancel-confirmed"}else{"/api/sales/orders/$($s.Orders[$label].id)/revisions"}
    $outcomes=Race "/api/app/warranty/$($raceInstall.Id)/confirm-installation" $raceInstall.Input $rightPath @{ReasonGroup='Customer';Reason="$p $label"}
    $s["Race_$label"]=@{AssetId=$raceInstall.Id; Outcomes=$outcomes}; Save-State
    Assert-Uat (@($outcomes|Where-Object{$_.Status -ge 200 -and $_.Status -lt 300}).Count -eq 1) "$label concurrent HTTP requests have exactly one successful outcome"
    Assert-Uat (@($outcomes|Where-Object{$_.Status -ge 500}).Count -eq 0) "$label has no HTTP 500"
}
$beforePolicy = Invoke-UatApi GET "/api/app/warranty/reminder-list?SearchText=$p&MaxResultCount=100"
Invoke-UatApi POST "/api/app/warranty/set-policy/$($s.Components[0].id)" @{IsEnabled=$true;CycleMonths=6;WarningDaysBeforeDue=21} | Out-Null
$afterPolicy = Invoke-UatApi GET "/api/app/warranty/reminder-list?SearchText=$p&MaxResultCount=100"
Assert-Uat (($beforePolicy|ConvertTo-Json -Depth 15 -Compress) -ceq ($afterPolicy|ConvertTo-Json -Depth 15 -Compress)) 'Policy edit leaves existing due dates and snapshots unchanged'
$extReminders = @($afterPolicy.items|Where-Object customerAssetId -eq $s.External.assetId)
Assert-Uat ($extReminders.Count -eq 3) 'Three external baseline reminders before lifecycle actions'
$historyBefore=Invoke-UatApi GET "/api/app/warranty/asset-history?CustomerAssetId=$($s.External.assetId)&MaxResultCount=100"
$actions=@(
    @{Id=$extReminders[0].id;Action='complete-reminder';Input=@{CompletedAt=[DateTime]::Today.ToString('yyyy-MM-dd');Note="$p complete";IdempotencyKey="${p}_complete"}},
    @{Id=$extReminders[1].id;Action='reschedule-reminder';Input=@{DueDate=[DateTime]::Today.AddDays(20).ToString('yyyy-MM-dd');Note="$p reschedule";IdempotencyKey="${p}_reschedule"}},
    @{Id=$extReminders[2].id;Action='skip-reminder';Input=@{Note="$p skip";IdempotencyKey="${p}_skip"}}
)
foreach($action in $actions){
    Invoke-UatApi POST "/api/app/warranty/$($action.Id)/$($action.Action)" $action.Input | Out-Null
    Invoke-UatApi POST "/api/app/warranty/$($action.Id)/$($action.Action)" $action.Input | Out-Null
}
$s.LifecycleActions=$actions; Save-State
$historyAfter=Invoke-UatApi GET "/api/app/warranty/asset-history?CustomerAssetId=$($s.External.assetId)&MaxResultCount=100"
Assert-Uat ($historyAfter.totalCount -eq $historyBefore.totalCount+3) 'Complete/reschedule/skip append exactly three events despite retries'
foreach($old in $historyBefore.items){$matching=$historyAfter.items|Where-Object id -eq $old.id;Assert-Uat (($old|ConvertTo-Json -Compress) -ceq ($matching|ConvertTo-Json -Compress)) 'Historical event remains unchanged'}
$suspend=@{Reason="$p suspend";IdempotencyKey="${p}_suspend"}
Invoke-UatApi POST "/api/app/warranty/$($s.External.assetId)/suspend-asset" $suspend | Out-Null
Invoke-UatApi POST "/api/app/warranty/$($s.External.assetId)/suspend-asset" $suspend | Out-Null
$reminders=Invoke-UatApi GET "/api/app/warranty/reminder-list?SearchText=$($s.External.assetNo)&Status=1&MaxResultCount=100"
Assert-Uat ($reminders.totalCount -eq 0) 'Suspended asset has no pending reminder'
$s.DuplicateExternal=Invoke-UatApi POST '/api/app/warranty/external-asset' @{CustomerId=$s.Customer.id;Model="$p duplicate serial warning";SerialNo="${p}_EXT";IdempotencyKey="${p}_duplicate_serial";Positions=@(@{PositionCode='CORE-1';PositionName='Unmapped';Quantity=1})}
Assert-Uat ($s.DuplicateExternal.duplicateSerialAssetNos -contains $s.External.assetNo) 'Duplicate serial warns with existing asset number'
$s.LifecycleComplete=$true;Save-State
