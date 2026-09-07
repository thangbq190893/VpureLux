param([Parameter(Mandatory)][ValidatePattern('^20260907_[0-9]{6}$')][string]$RunId)
$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../../..").Path
$out=Join-Path $root "artifacts/s006/backup-$RunId.json"
if(Test-Path $out){throw 'Do not overwrite backup evidence.'}
$config=Get-Content "$root/src/VPureLux.Web/appsettings.json" -Raw | ConvertFrom-Json
$b=New-Object System.Data.SqlClient.SqlConnectionStringBuilder($config.ConnectionStrings.Default)
if($b.InitialCatalog -cne 'VPL'){throw 'STOP: source is not VPL'}
$b['Application Name']='S006_VPL_BACKUP_REHEARSAL'
$c=New-Object System.Data.SqlClient.SqlConnection($b.ConnectionString)
function Query([string]$sql){
    $q=$c.CreateCommand();$q.CommandText=$sql;$q.CommandTimeout=300
    $a=New-Object System.Data.SqlClient.SqlDataAdapter($q);$t=New-Object System.Data.DataTable
    try{[void]$a.Fill($t);return ,$t}finally{$a.Dispose();$q.Dispose()}
}
function Proof { $p=Query 'SELECT @@SERVERNAME AS ServerName,DB_NAME() AS DatabaseName';if($p.Rows[0].DatabaseName -cne 'VPL'){throw 'STOP: catalog mismatch'}; $p.Rows[0] }
function Execute([string]$sql){[void](Proof);$q=$c.CreateCommand();$q.CommandText=$sql;$q.CommandTimeout=300;try{[void]$q.ExecuteNonQuery()}finally{$q.Dispose()}}
try{
    $c.Open();$proof=Proof
    $path="/var/opt/mssql/data/VPL-pre-service-s006-$RunId.bak"
    Execute "BACKUP DATABASE [VPL] TO DISK=N'$path' WITH COPY_ONLY,CHECKSUM,NOFORMAT,NOINIT,NAME=N'S006 VPL $RunId';"
    Execute "RESTORE VERIFYONLY FROM DISK=N'$path' WITH CHECKSUM;"
    $header=Query "RESTORE HEADERONLY FROM DISK=N'$path'"
    if($header.Rows.Count -ne 1 -or $header.Rows[0].DatabaseName -cne 'VPL' -or !$header.Rows[0].HasBackupChecksums){throw 'Unexpected backup header; stop before restore.'}
    $evidence=[ordered]@{RunId=$RunId;ServerName=$proof.ServerName;DatabaseName=$proof.DatabaseName;Path=$path;BackupSize=[long]$header.Rows[0].BackupSize;CompressedSize=[long]$header.Rows[0].CompressedBackupSize;Start=$header.Rows[0].BackupStartDate;Finish=$header.Rows[0].BackupFinishDate;CopyOnly=[bool]$header.Rows[0].IsCopyOnly;Checksum=[bool]$header.Rows[0].HasBackupChecksums;VerifyOnly='PASS'}
    $evidence|ConvertTo-Json|Set-Content $out -Encoding utf8
    [pscustomobject]$evidence
}finally{$c.Dispose()}
