param(
    [ValidateSet('Init', 'External')][string]$Phase = 'Init',
    [ValidatePattern('^W008_[A-Za-z0-9_]+$')][string]$Prefix = 'W008_20260904',
    [string]$OutputDirectory = 'artifacts/w008-verification',
    [switch]$AllowFixtureWrites
)
if (!$AllowFixtureWrites) { throw 'Fixture writes require explicit opt-in; resolve the documented legacy-fingerprint hold first.' }
. "$PSScriptRoot/Vpl-UatApi.ps1" -AllowFixtureWrites
$root = (Resolve-Path "$PSScriptRoot/../../..").Path
$statePath = "$root/$OutputDirectory/fixtures.json"
if (!(Test-Path "$root/$OutputDirectory/vpl-before.json")) { throw 'Capture the immutable baseline before fixtures.' }
function Save-State { $script:s | ConvertTo-Json -Depth 30 | Set-Content $statePath -Encoding utf8 }
function Assert-Uat($condition, [string]$description) {
    if (!$condition) { throw "FAIL: $description" }
    Write-Output "PASS: $description"
}
if ($Phase -eq 'Init') {
    $script:s = if (Test-Path $statePath) { Get-Content $statePath -Raw | ConvertFrom-Json -AsHashtable } else { @{ Prefix = $Prefix; StartedUtc = [DateTime]::UtcNow.ToString('O'); Components = @() } }
    if ($s.InitComplete) { throw 'Initialization already completed.' }
    if ($s.Customer -and !$s.Group) { throw 'Old fixture references a legacy group; use a new run/prefix, never rewrite the old baseline.' }
    Save-State
    if (!$s.Group) { $s.Group = Invoke-UatApi POST '/api/customer-groups' @{ Code = "$($s.Prefix)_G"; Name = "$($s.Prefix) isolated group" } }
    Save-State
    if (!$s.Customer) { $s.Customer = Invoke-UatApi POST '/api/customers' @{ Code = "$($s.Prefix)_KH"; Name = "$($s.Prefix) synthetic customer"; CustomerGroupId = $s.Group.id; Notes = 'W008 isolated synthetic UAT; not a real customer' } }
    Save-State
    if (!$s.Warehouse) { $s.Warehouse = Invoke-UatApi POST '/api/inventory/warehouses' @{ Code = "$($s.Prefix)_WH"; Name = "$($s.Prefix) isolated warehouse"; IsDefault = $false } }
    Save-State
    foreach ($i in 1..3) {
        if ($s.Components.Count -ge $i) { continue }
        $component = Invoke-UatApi POST '/api/app/component' @{ Code = "$($s.Prefix)_C$i"; Name = "$($s.Prefix) Core $i"; Unit = 'Cai' }
        $s.Components += $component
        Save-State
    }
    $s.Policy = Invoke-UatApi POST "/api/app/warranty/set-policy/$($s.Components[0].id)" @{ IsEnabled = $true; CycleMonths = 3; WarningDaysBeforeDue = 15 }
    if (!$s.Product) { $s.Product = Invoke-UatApi POST '/api/app/product' @{ Code = "$($s.Prefix)_M1"; Name = "$($s.Prefix) machine" } }
    Save-State
    $s.Machine = Invoke-UatApi POST "/api/app/warranty/set-machine-setting/$($s.Product.id)" @{ IsMachine = $true }
    $s.Bom = Invoke-UatApi POST "/api/bom/products/$($s.Product.id)/versions" @{ EffectiveFrom = [DateTime]::Today.ToString('yyyy-MM-dd'); Items = @(@{ ComponentId = $s.Components[0].id; Quantity = 1 }) }
    Save-State
    Invoke-UatApi POST "/api/bom/versions/$($s.Bom.id)/publish"
    $s.InitComplete = $true
    Save-State
    Write-Output "Created isolated fixtures: $($s.Prefix)"
    exit
}
$script:s = Get-Content $statePath -Raw | ConvertFrom-Json -AsHashtable
if (!$s.InitComplete) { throw 'Initialization incomplete; inspect saved state.' }
if ($s.ExternalComplete) { throw 'Already complete; do not repeat with new keys.' }
$positions = @(1..9 | ForEach-Object {
    $p = @{ PositionCode = "CORE-$_"; PositionName = "Core $_"; Quantity = 1 }
    if ($_ -le 5) { $p.ComponentId = $s.Components[0].id }
    if ($_ -eq 6) { $p.ComponentId = $s.Components[1].id }
    if ($_ -in 1,2,3,6,7) {
        $due = [DateTime]::Today.AddDays($(if ($_ -eq 1) { 10 } elseif ($_ -eq 2) { 0 } else { -2 }))
        $p.ReplacementBaselineDate = $due.AddMonths(-3).ToString('yyyy-MM-dd')
    }
    $p
})
$input = @{ CustomerId = $s.Customer.id; Model = "$($s.Prefix) external nine-core"; Brand = 'Synthetic'; SerialNo = "$($s.Prefix)_EXT"; InstallationAddress = 'Synthetic UAT location'; ExternalReference = $s.Prefix; IdempotencyKey = "$($s.Prefix)_external_create"; Positions = $positions }
$s.External = Invoke-UatApi POST '/api/app/warranty/external-asset' $input
Save-State
Assert-Uat ($s.External.positionCount -eq 9 -and $s.External.createdReminderCount -eq 3) 'Nine positions; only mapped enabled confirmed baselines create reminders'
$replay = Invoke-UatApi POST '/api/app/warranty/external-asset' $input
Assert-Uat ($replay.assetId -eq $s.External.assetId) 'External create retry preserves asset identity'
$s.ExternalBefore = Invoke-UatApi GET "/api/app/warranty/$($s.External.assetId)/asset-details"
Save-State
$page = Invoke-UatApi GET "/api/app/warranty/reminder-list?SearchText=$($s.Prefix)&MaxResultCount=100"
$s.RemindersBefore = $page
Save-State
Assert-Uat ($page.totalCount -eq 3) 'No duplicate reminder after retry'
$keep = $s.ExternalBefore.positions | Where-Object { $_.positionCode -in @('CORE-4','CORE-5','CORE-6','CORE-7','CORE-8','CORE-9') } | ConvertTo-Json -Depth 12 -Compress
$edit = @{ Model = $input.Model; Brand = $input.Brand; SerialNo = $input.SerialNo; IdempotencyKey = "$($s.Prefix)_external_partial"; Positions = @($s.ExternalBefore.positions | Where-Object { $_.positionCode -in @('CORE-1','CORE-2','CORE-3') } | ForEach-Object { @{ Id = $_.id; PositionCode = $_.positionCode; PositionName = $_.positionName; ComponentId = $_.componentId; Quantity = $_.quantity; ReplacementBaselineDate = $_.replacementBaselineDate; Note = 'W008 partial edit only' } }) }
$s.ExternalEdit = Invoke-UatApi PUT "/api/app/warranty/$($s.External.assetId)/external-asset" $edit
$after = Invoke-UatApi GET "/api/app/warranty/$($s.External.assetId)/asset-details"
$preserved = $after.positions | Where-Object { $_.positionCode -in @('CORE-4','CORE-5','CORE-6','CORE-7','CORE-8','CORE-9') } | ConvertTo-Json -Depth 12 -Compress
Assert-Uat ($preserved -ceq $keep) 'Partial Core1..3 edit preserves all Core4..9 facts'
$s.ExternalComplete = $true
Save-State
