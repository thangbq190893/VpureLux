param(
    [Parameter(Mandatory)]
    [ValidatePattern('^(VPureLux|VPureLux_SERVICE_REHEARSAL_[0-9_]+)$')]
    [string]$Database,
    [Parameter(Mandatory)][string]$OutputPath,
    [string]$OriginalBaseline
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path "$PSScriptRoot/../../..").Path
# Reuse the accepted SELECT-only fingerprint algorithm with a rollout-specific catalog guard.
$source = Get-Content "$repo/docs/evidence/s006/Capture-S006Inventory.ps1" -Raw
$source = $source.Replace('^(VPL|VPL_SERVICE_REHEARSAL_[0-9_]+|VPL_SERVICE_EMPTY_[0-9_]+)$', '^(VPureLux|VPureLux_SERVICE_REHEARSAL_[0-9_]+)$')
$source = $source.Replace('$root = (Resolve-Path "$PSScriptRoot/../../..").Path', ('$root = ''' + $repo.Replace("'", "''") + ''''))
$source = $source.Replace('S006_READ_ONLY_INVENTORY', 'SERVICE_V1_ROLLOUT_READ_ONLY')
& ([scriptblock]::Create($source)) -Database $Database -OutputPath $OutputPath -OriginalBaseline $OriginalBaseline
