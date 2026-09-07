param([Parameter(Mandatory)][ValidateSet('Baseline','Disabled','Enabled')][string]$Phase)
$ErrorActionPreference='Stop'
$repo=(Resolve-Path "$PSScriptRoot/../../..").Path
$out="$repo/artifacts/service-v1-rollout"
$base='http://180.93.99.150'
$session=[Microsoft.PowerShell.Commands.WebRequestSession]::new()
foreach($cookie in (Get-Content "$out/private-auth-cookies.json" -Raw|ConvertFrom-Json)){
    $session.Cookies.Add([Net.Cookie]::new($cookie.Name,$cookie.Value,$cookie.Path,$cookie.Domain))
}
$routes=@('/health-status','/Account/Login','/Sales','/Sales/Details/957d0603-593d-84ab-3f47-3a238c249fec','/Sales/Returns','/Sales/Refunds','/Reports/SalesRevenue','/Reports/SalesProfit','/Warranty','/Warranty/Policies','/Warranty/Assets','/Warranty/PendingInstallations','/libs/abp/core/abp.js','/libs/@fortawesome/fontawesome-free/css/all.css','/libs/@fortawesome/fontawesome-free/webfonts/fa-solid-900.woff2','/.well-known/jwks')
if($Phase -eq 'Enabled'){
    $routes+=@('/Service','/Service/Works','/Service?handler=List&skipCount=0&maxResultCount=10','/Service/Works?handler=List&skipCount=0&maxResultCount=10','/Reports/ServiceRevenue','/Reports/BusinessRevenue','/Reports/ServiceRevenue?handler=Summary&fromDateText=2026-01-01&toDateText=2026-12-31','/Reports/BusinessRevenue?handler=Summary&fromDateText=2026-01-01&toDateText=2026-12-31','/Reports/ServiceRevenue?handler=List&fromDateText=2026-01-01&toDateText=2026-12-31&skipCount=0&maxResultCount=10','/Reports/BusinessRevenue?handler=List&fromDateText=2026-01-01&toDateText=2026-12-31&skipCount=0&maxResultCount=10')
}
$results=@();$assets=[Collections.Generic.HashSet[string]]::new();$i=0
foreach($route in $routes){
    $r=Invoke-WebRequest ($base+$route) -WebSession $session -TimeoutSec 45 -SkipHttpErrorCheck
    $final=$r.BaseResponse.RequestMessage.RequestUri
    $expected=([uri]($base+$route)).AbsolutePath
    $pass=([int]$r.StatusCode -eq 200 -and $final.AbsolutePath.TrimEnd('/') -ceq $expected.TrimEnd('/'))
    $results+=[pscustomobject]@{Route=$route;Status=[int]$r.StatusCode;FinalPath=$final.AbsolutePath;Pass=$pass;Bytes=$r.RawContentLength}
    if($r.Content -is [string]){
        $r.Content|Set-Content "$out/http-$Phase-$i.txt"
        foreach($m in [regex]::Matches($r.Content,'(?:src|href)="([^"#]+\.(?:js|css)(?:\?[^" ]*)?)"')){
            $uri=[uri]::new([uri]$base,[Net.WebUtility]::HtmlDecode($m.Groups[1].Value))
            if($uri.Host -eq ([uri]$base).Host){[void]$assets.Add($uri.PathAndQuery)}
        }
    }
    $i++
}
foreach($asset in $assets){
    $r=Invoke-WebRequest ($base+$asset) -WebSession $session -TimeoutSec 45 -SkipHttpErrorCheck
    $results+=[pscustomobject]@{Route=$asset;Status=[int]$r.StatusCode;FinalPath=$r.BaseResponse.RequestMessage.RequestUri.AbsolutePath;Pass=([int]$r.StatusCode -eq 200 -and $r.RawContentLength -gt 0);Bytes=$r.RawContentLength}
}
$results|ConvertTo-Json -Depth 4|Set-Content "$out/http-$Phase-results.json"
$failed=@($results|Where-Object {!$_.Pass})
[pscustomobject]@{Phase=$Phase;Requests=$results.Count;Passed=$results.Count-$failed.Count;Failed=$failed.Count}
$failed|Format-Table -AutoSize
if($failed.Count){throw 'Read-only smoke gate failed'}
