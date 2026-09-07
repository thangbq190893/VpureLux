set -eu
test "$(readlink -f /opt/vpurelux/app)" = /opt/vpurelux/releases/web-20260907-152000-service-v1-b0bf197
curl -fsS --max-time 20 http://127.0.0.1:5000/health-status
python3 - <<'PY'
from pathlib import Path
p=Path('/etc/vpurelux/vpurelux.env')
text=p.read_text()
assert text.count('Service__IsEnabled=false\n')==1
assert text.count('Service__IsEnabled=')==1
p.write_text(text.replace('Service__IsEnabled=false\n','Service__IsEnabled=true\n'))
PY
systemctl restart vpurelux-web
echo 'SERVICE_TRUE_RESTARTED'
date -Is
