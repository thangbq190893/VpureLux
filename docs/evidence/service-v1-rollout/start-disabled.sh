set -eu
active=/opt/vpurelux/releases/web-20260903-180516-sales-v1-a4717aa
web=/opt/vpurelux/releases/web-20260907-152000-service-v1-b0bf197
test "$(readlink -f /opt/vpurelux/app)" = "$active"
test "$(systemctl show vpurelux-web -p MainPID --value)" = 0
test -s "$web/VPureLux.Web.dll"
cp -p /etc/vpurelux/vpurelux.env /etc/vpurelux/vpurelux.env.pre-service-v1-20260907
python3 - <<'PY'
from pathlib import Path
p=Path('/etc/vpurelux/vpurelux.env')
lines=p.read_text().splitlines(keepends=True)
key='Service__IsEnabled='
assert sum(x.startswith(key) for x in lines)<=1
lines=[x for x in lines if not x.startswith(key)]
text=''.join(lines)
if text and not text.endswith('\n'):text+='\n'
p.write_text(text+key+'false\n')
PY
ln -s "$web" /opt/vpurelux/app.service-v1-next
mv -Tf /opt/vpurelux/app.service-v1-next /opt/vpurelux/app
systemctl start vpurelux-web
echo 'NEW_RELEASE_STARTED_SERVICE_FALSE'
date -Is
readlink -f /opt/vpurelux/app
