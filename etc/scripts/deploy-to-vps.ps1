#Requires -Version 5.1
<#
.SYNOPSIS
  Publish VPureLux Web + DbMigrator and deploy to the dev/UAT VPS.

.DESCRIPTION
  Follows docs/VPURELUX_SERVER_DEPLOYMENT_RUNBOOK.md sections 17-19.
  Requires SSH access to the target server (key-based auth recommended).

.PARAMETER SkipPublish
  Skip dotnet publish; reuse existing publish folders/archives.

.PARAMETER Host
  VPS hostname or IP. Default: 180.93.99.150

.PARAMETER SshUser
  SSH user. Default: root

.PARAMETER SshPassword
  Optional root password (uses Posh-SSH when set). Prefer SSH keys instead.

.EXAMPLE
  .\deploy-to-vps.ps1

.EXAMPLE
  $env:VPS_ROOT_PASSWORD = '***'; .\deploy-to-vps.ps1 -SshPassword $env:VPS_ROOT_PASSWORD
#>
[CmdletBinding()]
param(
    [switch] $SkipPublish,
    [string] $HostName = '180.93.99.150',
    [string] $SshUser = 'root',
    [string] $SshPassword = $env:VPS_ROOT_PASSWORD
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$webProject = Join-Path $repoRoot 'src\VPureLux.Web\VPureLux.Web.csproj'
$dbMigratorProject = Join-Path $repoRoot 'src\VPureLux.DbMigrator\VPureLux.DbMigrator.csproj'
$webPublishDir = Join-Path $repoRoot 'src\VPureLux.Web\bin\Release\net10.0\publish'
$dbMigratorPublishDir = Join-Path $env:TEMP 'vpurelux-db-migrator-publish'
$webArchive = Join-Path $env:TEMP 'vpurelux-web-publish.tar.gz'
$dbMigratorArchive = Join-Path $env:TEMP 'vpurelux-db-migrator-publish.tar.gz'
$remoteHost = "${SshUser}@${HostName}"

function Ensure-PoshSsh {
    if (-not (Get-Module -ListAvailable -Name Posh-SSH)) {
        Write-Host 'Installing Posh-SSH module (CurrentUser)...'
        Install-Module -Name Posh-SSH -Scope CurrentUser -Force -AllowClobber
    }
    Import-Module Posh-SSH -ErrorAction Stop
}

function Invoke-RemoteCommand {
    param([string] $Command)
    if ($SshPassword) {
        Ensure-PoshSsh
        $sec = ConvertTo-SecureString $SshPassword -AsPlainText -Force
        $cred = New-Object System.Management.Automation.PSCredential($SshUser, $sec)
        $session = New-SSHSession -ComputerName $HostName -Credential $cred -AcceptKey -ErrorAction Stop
        try {
            $result = Invoke-SSHCommand -SessionId $session.SessionId -Command $Command -TimeOut 600
            if ($result.ExitStatus -ne 0) {
                throw "Remote command failed (exit $($result.ExitStatus)): $($result.Error)"
            }
            if ($result.Output) { $result.Output | ForEach-Object { Write-Host $_ } }
            return $result
        }
        finally {
            Remove-SSHSession -SessionId $session.SessionId | Out-Null
        }
    }

    ssh -o StrictHostKeyChecking=accept-new $remoteHost $Command
    if ($LASTEXITCODE -ne 0) { throw "ssh failed with exit code $LASTEXITCODE" }
}

function Send-RemoteFile {
    param([string] $LocalPath, [string] $RemotePath)
    if ($SshPassword) {
        Ensure-PoshSsh
        $sec = ConvertTo-SecureString $SshPassword -AsPlainText -Force
        $cred = New-Object System.Management.Automation.PSCredential($SshUser, $sec)
        # Posh-SSH expects a remote directory; keep Unix paths intact on Windows.
        $remoteDir = '/tmp/'
        Set-SCPItem -ComputerName $HostName -Credential $cred -Path $LocalPath -Destination $remoteDir -AcceptKey -ErrorAction Stop
        return
    }

    scp -o StrictHostKeyChecking=accept-new $LocalPath "${remoteHost}:$RemotePath"
    if ($LASTEXITCODE -ne 0) { throw "scp failed with exit code $LASTEXITCODE" }
}

if (-not $SkipPublish) {
    Write-Host 'Publishing VPureLux.Web (Release)...'
    dotnet publish $webProject -c Release -o $webPublishDir --nologo

    $pfxSrc = Join-Path $repoRoot 'src\VPureLux.Web\openiddict.pfx'
    if (-not (Test-Path (Join-Path $webPublishDir 'openiddict.pfx')) -and (Test-Path $pfxSrc)) {
        Copy-Item $pfxSrc (Join-Path $webPublishDir 'openiddict.pfx') -Force
    }

    Write-Host 'Publishing VPureLux.DbMigrator (Release)...'
    if (Test-Path $dbMigratorPublishDir) {
        Remove-Item -LiteralPath $dbMigratorPublishDir -Recurse -Force
    }
    dotnet publish $dbMigratorProject -c Release -o $dbMigratorPublishDir --nologo
}

if (-not (Test-Path (Join-Path $webPublishDir 'VPureLux.Web.dll'))) {
    throw "Web publish output missing: $webPublishDir"
}
if (-not (Test-Path (Join-Path $dbMigratorPublishDir 'VPureLux.DbMigrator.dll'))) {
    throw "DbMigrator publish output missing: $dbMigratorPublishDir"
}

Write-Host 'Creating archives...'
foreach ($archive in @($webArchive, $dbMigratorArchive)) {
    if (Test-Path $archive) { Remove-Item -LiteralPath $archive -Force }
}
tar -czf $webArchive -C $webPublishDir .
tar -czf $dbMigratorArchive -C $dbMigratorPublishDir .
Write-Host "  Web:      $webArchive ($([math]::Round((Get-Item $webArchive).Length / 1MB, 1)) MB)"
Write-Host "  Migrator: $dbMigratorArchive ($([math]::Round((Get-Item $dbMigratorArchive).Length / 1MB, 1)) MB)"

Write-Host "Uploading to $remoteHost ..."
Send-RemoteFile -LocalPath $webArchive -RemotePath '/tmp/vpurelux-web-publish.tar.gz'
Send-RemoteFile -LocalPath $dbMigratorArchive -RemotePath '/tmp/vpurelux-db-migrator-publish.tar.gz'

$remoteDeployScriptPath = Join-Path $PSScriptRoot 'remote-deploy-vpurelux.sh'
$remoteDeployScriptUnix = Join-Path $env:TEMP 'remote-deploy-vpurelux.sh'
$unixScript = (Get-Content -LiteralPath $remoteDeployScriptPath -Raw).Replace("`r`n", "`n")
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($remoteDeployScriptUnix, $unixScript, $utf8NoBom)
Send-RemoteFile -LocalPath $remoteDeployScriptUnix -RemotePath '/tmp/remote-deploy-vpurelux.sh'

Write-Host 'Running remote deploy...'
Invoke-RemoteCommand -Command 'chmod +x /tmp/remote-deploy-vpurelux.sh && bash /tmp/remote-deploy-vpurelux.sh'

Write-Host ''
Write-Host "Deploy complete. Open: http://${HostName}/"
Write-Host "Health: http://${HostName}/health-status"
