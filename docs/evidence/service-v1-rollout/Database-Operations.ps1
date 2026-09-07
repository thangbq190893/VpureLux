param(
    [Parameter(Mandatory)][ValidateSet('InventoryDisposable','DropDisposable','Backup','RestoreClone','DropClone')][string]$Action,
    [ValidatePattern('^20260907_[0-9]{6}$')][string]$RunId
)
$ErrorActionPreference='Stop'
$repo=(Resolve-Path "$PSScriptRoot/../../..").Path
$out="$repo/artifacts/service-v1-rollout"
$cfg=Get-Content "$repo/src/VPureLux.Web/appsettings.json" -Raw|ConvertFrom-Json
$b=[System.Data.SqlClient.SqlConnectionStringBuilder]::new($cfg.ConnectionStrings.Default)
$b['Initial Catalog']='master'
$b['Application Name']='SERVICE_V1_AUTHORIZED_BACKUP_REHEARSAL'
$c=[System.Data.SqlClient.SqlConnection]::new($b.ConnectionString)
function Query([string]$sql){
    $q=$c.CreateCommand();$q.CommandText=$sql;$q.CommandTimeout=300
    $a=[System.Data.SqlClient.SqlDataAdapter]::new($q);$t=[System.Data.DataTable]::new()
    try{[void]$a.Fill($t);return ,$t}finally{$a.Dispose();$q.Dispose()}
}
function Execute([string]$sql){$q=$c.CreateCommand();$q.CommandText=$sql;$q.CommandTimeout=300;try{[void]$q.ExecuteNonQuery()}finally{$q.Dispose()}}
function Records($t){foreach($r in $t.Rows){$o=[ordered]@{};foreach($col in $t.Columns){$o[$col.ColumnName]=$(if($r.IsNull($col)){$null}else{$r[$col]})};[pscustomobject]$o}}
try{
    $c.Open()
    $proof=Query 'SELECT @@SERVERNAME AS ServerName,DB_NAME() AS DatabaseName'
    if($proof.Rows[0].ServerName -cne 'VPureLux' -or $proof.Rows[0].DatabaseName -cne 'master'){throw 'Server/catalog proof failed'}
    if($Action -in @('InventoryDisposable','DropDisposable')){
        $names=@('VPL_SERVICE_REHEARSAL_20260907_113345','VPL_SERVICE_EMPTY_20260907_113345','VPL_SALES_REHEARSAL_20260903','VPL_SALES_REHEARSAL_LEGACY_20260903')
        $results=@(foreach($name in $names){
            $state=Query "SELECT d.name,d.state_desc,COALESCE(SUM(CONVERT(bigint,f.size))*8192,0) AS AllocatedBytes FROM sys.databases d LEFT JOIN sys.master_files f ON f.database_id=d.database_id WHERE d.name=N'$name' GROUP BY d.name,d.state_desc"
            $sessions=Query "SELECT COUNT(*) AS ActiveConnections FROM sys.dm_exec_sessions WHERE database_id=DB_ID(N'$name') AND is_user_process=1"
            $facts=@(Records $state)
            if($Action -eq 'DropDisposable' -and $facts.Count){
                if($sessions.Rows[0].ActiveConnections -ne 0){throw "Disposable database in use: $name"}
                Execute "DROP DATABASE [$name]"
            }
            [pscustomobject]@{Database=$name;Before=$facts;ActiveConnections=$sessions.Rows[0].ActiveConnections;Dropped=($Action -eq 'DropDisposable' -and $facts.Count -gt 0)}
        })
        $file="$out/$Action.json";if(Test-Path $file){throw 'Evidence already exists'}
        $results|ConvertTo-Json -Depth 6|Set-Content $file
        $results|Format-Table -AutoSize
        return
    }
    if(!$RunId){throw 'RunId required'}
    $path="/var/opt/mssql/data/VPureLux-pre-service-v1-$($RunId.Replace('_','-')).bak"
    $clone="VPureLux_SERVICE_REHEARSAL_$RunId"
    if($Action -eq 'Backup'){
        if(Test-Path "$out/backup-$RunId.json"){throw 'Never overwrite backup evidence'}
        $exists=Query "SELECT file_exists FROM sys.dm_os_file_exists(N'$path')"
        if($exists.Rows[0].file_exists){throw 'Backup path exists'}
        Execute "BACKUP DATABASE [VPureLux] TO DISK=N'$path' WITH COPY_ONLY,CHECKSUM,NOFORMAT,NOINIT,NAME=N'Service V1 production $RunId'"
        Execute "RESTORE VERIFYONLY FROM DISK=N'$path' WITH CHECKSUM"
        $h=(Query "RESTORE HEADERONLY FROM DISK=N'$path'").Rows[0]
        if($h.DatabaseName -cne 'VPureLux' -or !$h.HasBackupChecksums -or !$h.IsCopyOnly){throw 'Backup facts mismatch'}
        $e=[pscustomobject]@{Database='VPureLux';Path=$path;BackupSize=[long]$h.BackupSize;Start=$h.BackupStartDate;Finish=$h.BackupFinishDate;CopyOnly=[bool]$h.IsCopyOnly;Checksum=[bool]$h.HasBackupChecksums;VerifyOnly='PASS'}
        $e|ConvertTo-Json|Set-Content "$out/backup-$RunId.json";$e
    } elseif($Action -eq 'RestoreClone'){
        $backup=Get-Content "$out/backup-$RunId.json" -Raw|ConvertFrom-Json
        if($backup.Path -cne $path -or $backup.VerifyOnly -cne 'PASS'){throw 'Backup gate failed'}
        if((Query "SELECT DB_ID(N'$clone') AS Id").Rows[0].Id -isnot [DBNull]){throw 'Clone already exists'}
        $files=Query "RESTORE FILELISTONLY FROM DISK=N'$path'"
        $moves=@($files.Rows|ForEach-Object {
            if($_.Type -notin @('D','L')){throw 'Unsupported backup file type'}
            $ext=if($_.Type -eq 'L'){'ldf'}else{'mdf'}
            "MOVE N'$(([string]$_.LogicalName).Replace("'","''"))' TO N'/var/opt/mssql/data/$($clone)_$($_.FileId).$ext'"
        }) -join ','
        Execute "RESTORE DATABASE [$clone] FROM DISK=N'$path' WITH CHECKSUM,RECOVERY,$moves"
        $c.ChangeDatabase($clone)
        if((Query 'SELECT DB_NAME() AS Name').Rows[0].Name -cne $clone){throw 'Clone proof failed'}
        [pscustomobject]@{Database=$clone;SourceBackup=$path;Result='PASS'}|ConvertTo-Json|Set-Content "$out/clone-$RunId.json"
        "Restored and proved $clone"
    } elseif($Action -eq 'DropClone'){
        if(!(Test-Path "$out/final-reconciliation.json")){throw 'Final reconciliation evidence required'}
        $gate=Get-Content "$out/final-reconciliation.json" -Raw|ConvertFrom-Json
        if($gate.Gate -cne 'PASS'){throw 'Final reconciliation failed'}
        if((Query "SELECT COUNT(*) AS N FROM sys.dm_exec_sessions WHERE database_id=DB_ID(N'$clone') AND is_user_process=1").Rows[0].N -ne 0){throw 'Clone in use'}
        Execute "DROP DATABASE [$clone]"
        "Dropped exact clone $clone"
    }
}finally{$c.Dispose()}
