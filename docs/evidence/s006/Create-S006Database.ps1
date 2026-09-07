param(
    [Parameter(Mandatory)][ValidateSet('Rehearsal','Empty')][string]$Mode,
    [Parameter(Mandatory)][ValidatePattern('^20260907_[0-9]{6}$')][string]$RunId
)
$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../../..").Path
$config=Get-Content "$root/src/VPureLux.Web/appsettings.json" -Raw|ConvertFrom-Json
$b=New-Object System.Data.SqlClient.SqlConnectionStringBuilder($config.ConnectionStrings.Default)
if($b.InitialCatalog -cne 'VPL'){throw 'STOP: configured source must be VPL'}
$target=if($Mode -eq 'Rehearsal'){"VPL_SERVICE_REHEARSAL_$RunId"}else{"VPL_SERVICE_EMPTY_$RunId"}
$b['Application Name']='S006_ISOLATED_DATABASE_CREATE'
$c=New-Object System.Data.SqlClient.SqlConnection($b.ConnectionString)
function Query([string]$sql){$q=$c.CreateCommand();$q.CommandText=$sql;$q.CommandTimeout=300;$a=New-Object System.Data.SqlClient.SqlDataAdapter($q);$t=New-Object System.Data.DataTable;try{[void]$a.Fill($t);return ,$t}finally{$a.Dispose();$q.Dispose()}}
try{
    $c.Open();$proof=Query 'SELECT @@SERVERNAME AS ServerName,DB_NAME() AS DatabaseName'
    if($proof.Rows[0].DatabaseName -cne 'VPL'){throw 'STOP: source catalog mismatch'}
    if((Query "SELECT DB_ID(N'$target') AS Id").Rows[0].Id -isnot [DBNull]){throw 'Database already exists; never overwrite.'}
    if($Mode -eq 'Rehearsal'){
        $backup=Get-Content "$root/artifacts/s006/backup-$RunId.json" -Raw|ConvertFrom-Json
        if($backup.DatabaseName -cne 'VPL' -or $backup.VerifyOnly -cne 'PASS' -or $backup.Path -cne "/var/opt/mssql/data/VPL-pre-service-s006-$RunId.bak"){throw 'Backup evidence mismatch'}
        $files=Query "RESTORE FILELISTONLY FROM DISK=N'$($backup.Path)'"
        $moves=@($files.Rows|ForEach-Object {
            if($_.Type -notin @('D','L')){throw 'Unsupported backup file type'}
            $extension=if($_.Type -eq 'L'){'ldf'}else{'mdf'}
            "MOVE N'$(([string]$_.LogicalName).Replace("'","''"))' TO N'/var/opt/mssql/data/$($target)_$($_.FileId).$extension'"
        }) -join ','
        $sql="RESTORE DATABASE [$target] FROM DISK=N'$($backup.Path)' WITH CHECKSUM,RECOVERY,$moves;"
    }else{$sql="CREATE DATABASE [$target];"}
    if((Query 'SELECT DB_NAME() AS Name').Rows[0].Name -cne 'VPL'){throw 'Source changed before write'}
    $q=$c.CreateCommand();$q.CommandText=$sql;$q.CommandTimeout=300
    try{[void]$q.ExecuteNonQuery()}finally{$q.Dispose()}
}finally{$c.Dispose()}
$b['Initial Catalog']=$target
$c=New-Object System.Data.SqlClient.SqlConnection($b.ConnectionString)
try{
    $c.Open();$proof=Query 'SELECT @@SERVERNAME AS ServerName,DB_NAME() AS DatabaseName'
    if($proof.Rows[0].DatabaseName -cne $target){throw 'Created database identity mismatch'}
    $result=[pscustomobject]@{Mode=$Mode;ServerName=$proof.Rows[0].ServerName;DatabaseName=$proof.Rows[0].DatabaseName;CreatedUtc=[DateTime]::UtcNow.ToString('O')}
    $result|ConvertTo-Json|Set-Content "$root/artifacts/s006/database-$Mode-$RunId.json"
    $result
}finally{$c.Dispose()}
