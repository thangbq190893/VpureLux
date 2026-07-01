#!/usr/bin/env bash
set -eu

ARCHIVE=/tmp/vpurelux-web-publish.tar.gz
MIG_ARCHIVE=/tmp/vpurelux-db-migrator-publish.tar.gz
RELEASE="/opt/vpurelux/releases/web-$(date +%Y%m%d-%H%M%S)"
MIG_RELEASE="/opt/vpurelux/releases/dbmigrator-$(date +%Y%m%d-%H%M%S)"

mkdir -p "$RELEASE" "$MIG_RELEASE"
tar -xzf "$ARCHIVE" -C "$RELEASE"
tar -xzf "$MIG_ARCHIVE" -C "$MIG_RELEASE"
test -f "$RELEASE/VPureLux.Web.dll"
test -f "$MIG_RELEASE/VPureLux.DbMigrator.dll"

systemctl stop vpurelux-web || true

if [ -e /opt/vpurelux/app ] && [ ! -L /opt/vpurelux/app ]; then
  mv /opt/vpurelux/app "/opt/vpurelux/releases/app-backup-$(date +%Y%m%d-%H%M%S)"
fi

ln -sfn "$RELEASE" /opt/vpurelux/app
chown -R vpurelux:vpurelux "$RELEASE" "$MIG_RELEASE" /var/log/vpurelux
chmod 750 "$RELEASE" "$MIG_RELEASE"
find "$RELEASE" -type d -exec chmod 750 {} +
find "$RELEASE" -type f -exec chmod 640 {} +
find "$MIG_RELEASE" -type d -exec chmod 750 {} +
find "$MIG_RELEASE" -type f -exec chmod 640 {} +

if [ -f /root/vpurelux-bootstrap/run-db-migrator.sh ]; then
  /root/vpurelux-bootstrap/run-db-migrator.sh "$MIG_RELEASE"
elif [ -f /root/vpurelux-bootstrap/credentials.txt ]; then
  # shellcheck disable=SC1091
  . /root/vpurelux-bootstrap/credentials.txt
  export ASPNETCORE_ENVIRONMENT=Production
  export DOTNET_ENVIRONMENT=Production
  export ConnectionStrings__Default="Server=127.0.0.1,1433;Database=${VPURELUX_DB};User Id=${VPURELUX_APP_LOGIN};Password=${VPURELUX_APP_PASSWORD};TrustServerCertificate=True;"
  export Redis__Configuration="127.0.0.1:6379,password=${REDIS_PASSWORD}"
  cd "$MIG_RELEASE"
  dotnet VPureLux.DbMigrator.dll
else
  echo "WARNING: /root/vpurelux-bootstrap/credentials.txt not found; skipping DbMigrator."
fi

systemctl daemon-reload
systemctl start vpurelux-web
sleep 15
systemctl is-active vpurelux-web
curl -sS --max-time 15 http://127.0.0.1/health-status || true
