$ErrorActionPreference='Stop'
$repo=(Resolve-Path "$PSScriptRoot/../../..").Path
$source='C:\SourceCode\VPureLux-service-v1-b0bf197'
$out="$repo/artifacts/service-v1-rollout"
$web="$out/publish/web"
$scripts=@(Get-ChildItem "$source/src/VPureLux.Web/Pages/Service" -Filter '*.js')+@(Get-Item "$source/src/VPureLux.Web/Pages/Reports/BusinessRevenue.js")
foreach($script in $scripts){
    $relative=[IO.Path]::GetRelativePath("$source/src/VPureLux.Web",$script.FullName)
    $target=Join-Path $web $relative
    if((Get-FileHash $target).Hash -cne (Get-FileHash $script.FullName).Hash){throw "Script mismatch: $relative"}
    node --check $target
    if($LASTEXITCODE -ne 0){throw "Script parsing failed: $relative"}
}
$fonts=@(Get-ChildItem "$web/wwwroot/libs/@fortawesome/fontawesome-free/webfonts" -File)
if($fonts.Count -lt 4 -or !(Test-Path "$web/wwwroot/libs/abp/core/abp.js")){throw 'Asset gate failed'}
foreach($css in Get-ChildItem "$web/wwwroot/libs/@fortawesome/fontawesome-free/css" -Filter '*.css'){
    foreach($match in [regex]::Matches((Get-Content $css.FullName -Raw),'url\(["'']?(\.\./webfonts/[^)"'']+)["'']?\)')){
        if(!(Test-Path (Join-Path $css.DirectoryName $match.Groups[1].Value.Split('?')[0]))){throw 'Font reference missing'}
    }
}
if(!(Get-Content "$web/Pages/Reports/BusinessRevenue.js" -Raw).Contains('const summary = () =>')){throw 'Accepted minifier fix missing'}
$archives=@(foreach($name in 'web','dbmigrator'){
    $archive="$out/$name-service-v1-b0bf197.tar.gz"
    if(Test-Path $archive){throw 'Artifact already exists'}
    tar -czf $archive --exclude='appsettings*.json' --exclude='openiddict.pfx' --exclude='Logs' --exclude='*.log' -C "$out/publish/$name" .
    if($LASTEXITCODE -ne 0){throw 'Archive failed'}
    $entries=@(tar -tzf $archive)
    if($LASTEXITCODE -ne 0 -or ($entries|Where-Object {$_ -match 'appsettings.*json|openiddict.pfx|UatApi|uat-auth|fixtures.json|/Runtime/'})){throw 'Private or UAT artifact detected'}
    [pscustomobject]@{Name=$name;Path=$archive;Sha256=(Get-FileHash $archive).Hash;Bytes=(Get-Item $archive).Length;Entries=$entries.Count}
})
[pscustomobject]@{Source=(git -C $source rev-parse HEAD);Scripts=$scripts.Count;Fonts=$fonts.Count;ConfigAndPfxExcluded=$true;Archives=$archives}|ConvertTo-Json -Depth 5|Set-Content "$out/artifact-verification.json"
$archives|Format-Table -AutoSize
