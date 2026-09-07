set -eu
test "$(readlink -f /opt/vpurelux/app)" = /opt/vpurelux/releases/web-20260903-180516-sales-v1-a4717aa
test -s /var/opt/mssql/data/VPureLux-pre-service-v1-20260907-152000.bak
test -s /opt/vpurelux/releases/web-20260907-152000-service-v1-b0bf197/VPureLux.Web.dll
test "$(df --output=avail -B1 / | tail -1)" -gt 10737418240
curl -fsS --max-time 20 http://127.0.0.1:5000/health-status
date -Is
systemctl stop vpurelux-web
test "$(systemctl show vpurelux-web -p MainPID --value)" = 0
test "$(systemctl is-active vpurelux-web || true)" = inactive
echo WEB_STOPPED_VERIFIED
df -B1 /
