$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../../..").Path
$dir=Join-Path $root 'artifacts/s006'
$web=Join-Path $dir 'publish/web';$db=Join-Path $dir 'publish/dbmigrator'
foreach($file in @("$web/VPureLux.Web.dll","$db/VPureLux.DbMigrator.dll","$web/openiddict.pfx","$web/wwwroot/libs/abp/core/abp.js")){
    if(!(Test-Path -LiteralPath $file)){throw "Required publish file missing: $file"}
}
$scripts=@(Get-ChildItem "$root/src/VPureLux.Web/Pages/Service" -Filter '*.js')+@(Get-Item "$root/src/VPureLux.Web/Pages/Reports/BusinessRevenue.js")
$checked=@()
foreach($source in $scripts){
    $relative=[IO.Path]::GetRelativePath("$root/src/VPureLux.Web",$source.FullName)
    $destination=Join-Path $web $relative
    if(!(Test-Path $destination) -or (Get-FileHash $source.FullName).Hash -ne (Get-FileHash $destination).Hash){throw "Script publish mismatch: $relative"}
    node --check $destination
    if($LASTEXITCODE -ne 0){throw "Invalid JavaScript: $relative"}
    $checked+=$relative.Replace('\','/')
}
$fontRoot="$web/wwwroot/libs/@fortawesome/fontawesome-free"
$fonts=@(Get-ChildItem "$fontRoot/webfonts" -File)
$missing=@()
foreach($css in Get-ChildItem "$fontRoot/css" -Filter '*.css'){
    foreach($match in [regex]::Matches((Get-Content $css.FullName -Raw),'url\(["'']?(\.\./webfonts/[^)"'']+)["'']?\)')){
        $path=Join-Path $css.DirectoryName ($match.Groups[1].Value.Split('?')[0])
        if(!(Test-Path $path)){$missing+=$path}
    }
}
if(!$fonts.Count -or $missing.Count){throw 'Missing referenced font assets'}
if(!(Get-Content "$web/Pages/Reports/BusinessRevenue.js" -Raw).Contains('const summary = () =>')){throw 'Minifier correction missing'}
$archives=@()
foreach($item in @(@{Name='web';Path=$web},@{Name='dbmigrator';Path=$db})){
    $archive=Join-Path $dir "$($item.Name)-b0bf197-s006.tar.gz"
    if(Test-Path $archive){throw 'Refuse to overwrite archive'}
    tar -czf $archive -C $item.Path .
    if($LASTEXITCODE -ne 0){throw 'Archive failed'}
    $archives+=@{Name=$item.Name;Path=[IO.Path]::GetRelativePath($root,$archive).Replace('\','/');Sha256=(Get-FileHash $archive -Algorithm SHA256).Hash;Bytes=(Get-Item $archive).Length;Files=@(Get-ChildItem $item.Path -Recurse -File).Count}
}
$result=@{Source='b0bf197';ApplicationFix='c2663ac';Scripts=$checked;Fonts=$fonts.Count;MissingFonts=$missing.Count;Certificate='Local openiddict.pfx present in publish; private artifact, not source-controlled; no certificate/password content recorded';Minifier='be4a5ad closure retained; actual NUglify verified by BusinessReportUiTests';Archives=$archives;Started=$false;Uploaded=$false;ProductionAccess=$false}
$result|ConvertTo-Json -Depth 8|Set-Content "$dir/publish-verification.json" -Encoding utf8
$archives|Format-Table -AutoSize
