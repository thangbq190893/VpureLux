param([Parameter(Mandatory)][string]$ScriptPath, [Parameter(Mandatory)][string]$OutputPath, [int]$TimeoutSeconds=60)
$ErrorActionPreference='Stop'
$repo=(Resolve-Path "$PSScriptRoot/../../..").Path
$config=Get-Content "$repo/src/VPureLux.Web/appsettings.json" -Raw|ConvertFrom-Json
$builder=[System.Data.SqlClient.SqlConnectionStringBuilder]::new($config.ConnectionStrings.Default)
# This deployment's explicitly supplied SSH credential matches the locally stored credential.
$credential=[pscredential]::new('root',(ConvertTo-SecureString $builder.Password -AsPlainText -Force))
Import-Module Posh-SSH
$session=New-SSHSession -ComputerName 180.93.99.150 -Credential $credential -ConnectionTimeout 15 -AcceptKey
try {
    $script=Get-Content -LiteralPath (Join-Path $repo $ScriptPath) -Raw
    $bytes=[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($script.Replace("`r`n","`n")))
    $result=Invoke-SSHCommand -SessionId $session.SessionId -Command "echo '$bytes' | base64 -d | bash" -TimeOut $TimeoutSeconds
    $out=Join-Path $repo $OutputPath
    if(Test-Path -LiteralPath $out){throw 'Evidence already exists'}
    $result|Select-Object ExitStatus,Output,Error|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $out -Encoding utf8
    $result.Output
    if($result.ExitStatus -ne 0){throw "Remote command failed: $($result.ExitStatus); inspect private evidence"}
} finally {Remove-SSHSession -SessionId $session.SessionId|Out-Null}
