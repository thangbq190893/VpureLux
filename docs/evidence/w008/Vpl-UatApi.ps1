param([string]$BaseUrl = 'http://localhost:5198', [switch]$AllowFixtureWrites)
$ErrorActionPreference = 'Stop'
if ($BaseUrl -cne 'http://localhost:5198') { throw 'This harness only targets the isolated W008 local runtime.' }
if (!$env:VPL_UAT_PASSWORD) { throw 'Set VPL_UAT_PASSWORD for the existing local UAT account before login.' }
$script:UatSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$login = Invoke-WebRequest "$BaseUrl/Account/Login" -WebSession $script:UatSession
$match = [regex]::Match($login.Content, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"')
if (!$match.Success) { throw 'Login antiforgery token missing.' }
$loginBody = @{ 'LoginInput.UserNameOrEmailAddress' = 'admin'; 'LoginInput.Password' = $env:VPL_UAT_PASSWORD; '__RequestVerificationToken' = $match.Groups[1].Value; 'Action' = 'Login' }
$result = Invoke-WebRequest "$BaseUrl/Account/Login" -Method Post -Body $loginBody -WebSession $script:UatSession
if ($result.BaseResponse.RequestMessage.RequestUri.AbsolutePath -like '/Account/Login*') { throw 'UAT login failed.' }
$script:UatBaseUrl = $BaseUrl
$script:UatAllowFixtureWrites = $AllowFixtureWrites.IsPresent
function Invoke-UatApi([string]$Method, [string]$Path, $Body = $null) {
    if (!$Path.StartsWith('/api/')) { throw 'Only local API routes are permitted.' }
    if ($Method -notin 'GET', 'HEAD' -and !$script:UatAllowFixtureWrites) { throw 'Read-only by default; fixture writes require explicit opt-in after the safety gate.' }
    $headers = @{}
    $token = $script:UatSession.Cookies.GetCookies([uri]$script:UatBaseUrl)['XSRF-TOKEN']
    if ($token) { $headers['RequestVerificationToken'] = [uri]::UnescapeDataString($token.Value) }
    $parameters = @{ Uri = "$script:UatBaseUrl$Path"; Method = $Method; WebSession = $script:UatSession; Headers = $headers; ContentType = 'application/json'; SkipHttpErrorCheck = $true }
    if ($null -ne $Body) { $parameters.Body = $Body | ConvertTo-Json -Depth 20 -Compress }
    $response = Invoke-WebRequest @parameters
    if ([int]$response.StatusCode -ge 400) { throw "API $Method $Path returned $($response.StatusCode): $($response.Content)" }
    if ($response.Content) { return $response.Content | ConvertFrom-Json }
}
