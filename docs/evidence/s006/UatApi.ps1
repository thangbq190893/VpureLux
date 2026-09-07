param([switch]$AllowFixtureWrites)
$ErrorActionPreference='Stop'
$script:S006Root=(Resolve-Path "$PSScriptRoot/../../..").Path
$script:UatBaseUrl='http://localhost:5196'
$script:UatAllowWrites=$AllowFixtureWrites.IsPresent
$script:Auth=Get-Content "$script:S006Root/artifacts/s006/uat-auth.json" -Raw | ConvertFrom-Json
$script:UatSession=[Microsoft.PowerShell.Commands.WebRequestSession]::new()
$login=Invoke-WebRequest "$script:UatBaseUrl/Account/Login" -WebSession $script:UatSession
$token=[regex]::Match($login.Content,'name="__RequestVerificationToken"[^>]*value="([^"]+)"')
if(!$token.Success){throw 'Missing login antiforgery token'}
$r=Invoke-WebRequest "$script:UatBaseUrl/Account/Login" -Method Post -WebSession $script:UatSession -Body @{'LoginInput.UserNameOrEmailAddress'=$script:Auth.UserName;'LoginInput.Password'=$script:Auth.Password;'__RequestVerificationToken'=$token.Groups[1].Value;Action='Login'}
if($r.BaseResponse.RequestMessage.RequestUri.AbsolutePath -like '/Account/Login*'){throw 'S006 fixture login failed'}
function Invoke-S006([string]$Method,[string]$Path,$Body=$null,[switch]$Raw,[switch]$NoToken){
    if(!$Path.StartsWith('/api/')){throw 'API path required'}
    if($Method -notin 'GET','HEAD' -and !$script:UatAllowWrites){throw 'Explicit fixture-write switch required'}
    $h=@{Accept='application/json';'X-Requested-With'='XMLHttpRequest'}
    # PowerShell retains explicitly supplied headers in WebRequestSession between requests.
    [void]$script:UatSession.Headers.Remove('RequestVerificationToken')
    $t=$script:UatSession.Cookies.GetCookies([uri]$script:UatBaseUrl)['XSRF-TOKEN']
    if($t -and !$NoToken){$h.RequestVerificationToken=[uri]::UnescapeDataString($t.Value)}
    $args=@{Uri="$script:UatBaseUrl$Path";Method=$Method;WebSession=$script:UatSession;Headers=$h;ContentType='application/json';SkipHttpErrorCheck=$true;TimeoutSec=90}
    if($null -ne $Body){$args.Body=$Body|ConvertTo-Json -Depth 30 -Compress}
    $r=Invoke-WebRequest @args
    if($Raw){return @{Status=[int]$r.StatusCode;Content=$r.Content;FinalUrl=$r.BaseResponse.RequestMessage.RequestUri.AbsoluteUri}}
    if([int]$r.StatusCode -ge 400){throw "$Method $Path : $($r.StatusCode) $($r.Content)"}
    if($r.Content){return $r.Content|ConvertFrom-Json -AsHashtable}
}
function Set-S006Service([bool]$Enabled){
    if(!$script:UatAllowWrites){throw 'Explicit fixture-write switch required'}
    $null=Invoke-WebRequest "$script:UatBaseUrl/s006-control/service/$($Enabled.ToString().ToLowerInvariant())" -Method Post -Headers @{'X-S006-Control'=$script:Auth.Control}
}
function Assert-S006($Condition,[string]$Message){if(!$Condition){throw "FAIL: $Message"};Write-Host "PASS: $Message"}
function Invoke-S006Race([string]$LeftPath,$LeftBody,[string]$RightPath,$RightBody){
    $handler=[Net.Http.HttpClientHandler]::new();$handler.CookieContainer=$script:UatSession.Cookies
    $client=[Net.Http.HttpClient]::new($handler)
    $token=$script:UatSession.Cookies.GetCookies([uri]$script:UatBaseUrl)['XSRF-TOKEN']
    $client.DefaultRequestHeaders.Add('RequestVerificationToken',[uri]::UnescapeDataString($token.Value))
    $a=[Net.Http.StringContent]::new(($LeftBody|ConvertTo-Json -Depth 30 -Compress),[Text.Encoding]::UTF8,'application/json')
    $b=[Net.Http.StringContent]::new(($RightBody|ConvertTo-Json -Depth 30 -Compress),[Text.Encoding]::UTF8,'application/json')
    try{
        $first=$client.PostAsync("$script:UatBaseUrl$LeftPath",$a);$second=$client.PostAsync("$script:UatBaseUrl$RightPath",$b)
        [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($first,$second))
        return ,@(@($first,$second)|ForEach-Object{@{Status=[int]$_.Result.StatusCode;Content=$_.Result.Content.ReadAsStringAsync().GetAwaiter().GetResult()}})
    }finally{$a.Dispose();$b.Dispose();$client.Dispose()}
}
