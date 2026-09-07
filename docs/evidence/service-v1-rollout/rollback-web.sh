set -eu
rollback=/opt/vpurelux/releases/web-20260903-180516-sales-v1-a4717aa
test -s "$rollback/VPureLux.Web.dll"
systemctl stop vpurelux-web
if test -f /etc/vpurelux/vpurelux.env.pre-service-v1-20260907; then
  cp -p /etc/vpurelux/vpurelux.env.pre-service-v1-20260907 /etc/vpurelux/vpurelux.env
fi
ln -s "$rollback" /opt/vpurelux/app.service-v1-rollback
mv -Tf /opt/vpurelux/app.service-v1-rollback /opt/vpurelux/app
systemctl start vpurelux-web
echo 'ROLLBACK_STARTED_VERIFY_HEALTH_SEPARATELY'
readlink -f /opt/vpurelux/app
