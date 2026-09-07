set -eu
printf 'TIME '; date -Is
printf 'DISK '; df -B1 / | tail -1
printf 'INODES '; df -i / | tail -1
printf 'ACTIVE_RELEASE '; readlink -f /opt/vpurelux/app
printf 'SERVICE '; systemctl is-active vpurelux-web
systemctl show vpurelux-web -p MainPID -p ActiveEnterTimestamp -p NRestarts -p User --no-pager
python3 - <<'PY'
import subprocess, json, os
pid=subprocess.check_output(['systemctl','show','vpurelux-web','-p','MainPID','--value'], text=True).strip()
assert pid and pid != '0', 'No active runtime PID'
env=dict(x.split('=',1) for x in open('/proc/'+pid+'/environ','rb').read().decode().split('\0') if '=' in x)
print('RUNTIME_SERVICE_ENABLED',env.get('Service__IsEnabled','<absent>'))
print('RUNTIME_ENVIRONMENT',env.get('ASPNETCORE_ENVIRONMENT','<absent>'))
print('RUNTIME_CONNECTION_CATALOG',next((p.split('=',1)[1] for p in env.get('ConnectionStrings__Default','').split(';') if p.lower().startswith(('database=','initial catalog='))),'<absent>'))
print('CURRENT_PFX_EXISTS',os.path.isfile('/opt/vpurelux/app/openiddict.pfx'))
PY
curl -fsS --max-time 30 http://127.0.0.1:5000/health-status
curl -fsS --max-time 30 http://127.0.0.1:5000/.well-known/jwks | python3 -c "import sys,json; print('SIGNING_KEY_IDS', [x.get('kid') for x in json.load(sys.stdin).get('keys',[])])"
python3 - <<'PY'
import subprocess,re
logs=subprocess.check_output(['journalctl','-u','vpurelux-web','--since','2026-09-07 15:17:40','--no-pager','-o','cat'],text=True)
hits=[l for l in logs.splitlines() if re.search(r'\[(ERR|FTL)\]|Unhandled|SqlException|OpenIddict.*(error|fail)|Request finished .* - 500\b|status code 500\b',l,re.I)]
print('MATERIAL_LOG_HITS',len(hits))
for hit in hits[:50]:print(hit[:700])
PY
