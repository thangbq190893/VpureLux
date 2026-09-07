set -eu
systemctl is-active vpurelux-web
for i in 1 2 3; do
  curl -fsS --max-time 20 http://127.0.0.1:5000/health-status
  echo
  sleep 2
done
python3 - <<'PY'
import subprocess,json
pid=subprocess.check_output(['systemctl','show','vpurelux-web','-p','MainPID','--value'],text=True).strip()
env=dict(x.split('=',1) for x in open('/proc/'+pid+'/environ','rb').read().decode().split('\0') if '=' in x)
print('Service__IsEnabled',env.get('Service__IsEnabled','<absent>'))
for k in ('CustomerCare__IsEnabled','CustomerCare__IsSalesIntakeEnabled','CustomerCare__SalesIntakeGoLiveFrom'):print(k,env.get(k))
print('SIGNING_KEY_IDS',[k.get('kid') for k in json.loads(subprocess.check_output(['curl','-fsS','http://127.0.0.1:5000/.well-known/jwks'],text=True)).get('keys',[])])
PY
