set -eu
active=/opt/vpurelux/releases/web-20260907-152000-service-v1-b0bf197
rollback=/opt/vpurelux/releases/web-20260903-180516-sales-v1-a4717aa
test "$(readlink -f /opt/vpurelux/app)" = "$active"
test -s "$active/VPureLux.Web.dll"
test -s "$rollback/VPureLux.Web.dll"
test -s /var/opt/mssql/data/VPureLux-pre-service-v1-20260907-152000.bak
test -s /var/opt/mssql/data/VPureLux-pre-service-v1-20260907-151324.bak
for path in /tmp/web-service-v1-b0bf197.tar.gz /tmp/dbmigrator-service-v1-b0bf197.tar.gz; do
  if test -f "$path"; then stat -c 'DELETE %n %s bytes' "$path"; rm -- "$path"; fi
done
df -B1 /
df -i /
echo TEMPORARY_UPLOADS_REMOVED
