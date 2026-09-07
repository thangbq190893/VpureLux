set -eu
active=/opt/vpurelux/releases/web-20260903-180516-sales-v1-a4717aa
web=/opt/vpurelux/releases/web-20260907-152000-service-v1-b0bf197
db=/opt/vpurelux/releases/dbmigrator-20260907-152000-service-v1-b0bf197
test "$(readlink -f /opt/vpurelux/app)" = "$active"
test ! -e "$web"
test ! -e "$db"
echo '3810c6d6b97e5872408850b33d3b8103de91b9418be316462aea8ba05c9bbb02  /tmp/web-service-v1-b0bf197.tar.gz' | sha256sum -c -
echo 'f7a5a41e4767a837295ec2d7035a2c8fa2f134899d23a3116d914735cc11694e  /tmp/dbmigrator-service-v1-b0bf197.tar.gz' | sha256sum -c -
mkdir "$web" "$db"
tar -xzf /tmp/web-service-v1-b0bf197.tar.gz -C "$web"
tar -xzf /tmp/dbmigrator-service-v1-b0bf197.tar.gz -C "$db"
for name in appsettings.json appsettings.Production.json appsettings.secrets.json; do
  if test -f "$active/$name"; then cp -p -- "$active/$name" "$web/$name"; cp -p -- "$active/$name" "$db/$name"; cmp "$active/$name" "$web/$name"; fi
done
cp -p -- "$active/openiddict.pfx" "$web/openiddict.pfx"
cmp "$active/openiddict.pfx" "$web/openiddict.pfx"
mkdir -p "$web/Logs" "$db/Logs"
chown -R vpurelux:vpurelux "$web" "$db"
chmod 600 "$web/openiddict.pfx" "$web/appsettings.secrets.json" "$db/appsettings.secrets.json"
test -s "$web/VPureLux.Web.dll"
test -s "$db/VPureLux.DbMigrator.dll"
sha256sum "$web/VPureLux.Web.dll" "$db/VPureLux.DbMigrator.dll" "$web/openiddict.pfx"
echo 'CANDIDATE_PREPARED_CONFIG_CERT_BYTE_MATCH'
df -B1 /
