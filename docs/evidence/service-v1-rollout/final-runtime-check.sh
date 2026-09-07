set -eu
systemctl is-active vpurelux-web
test "$(readlink -f /opt/vpurelux/app)" = /opt/vpurelux/releases/web-20260907-152000-service-v1-b0bf197
curl -fsS --max-time 30 http://127.0.0.1:5000/health-status
python3 - <<'PY'
import subprocess,re
logs=subprocess.check_output(['journalctl','-u','vpurelux-web','--since','2026-09-07 15:48:00','--no-pager','-o','cat'],text=True)
hits=[line for line in logs.splitlines() if re.search(r'\[(ERR|FTL)\]|Unhandled|SqlException|Request finished .* - 500\b|status code 500\b',line,re.I)]
print('POST_SEAL_MATERIAL_LOG_HITS',len(hits))
for hit in hits: print(hit[:700])
if hits: raise SystemExit(2)
PY
df -B1 / | tail -1
df -i / | tail -1
