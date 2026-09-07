set -eu
python3 - <<'PY'
import json,os,subprocess,hashlib
pid=subprocess.check_output(['systemctl','show','vpurelux-web','-p','MainPID','--value'],text=True).strip()
env=dict(x.split('=',1) for x in open('/proc/'+pid+'/environ','rb').read().decode().split('\0') if '=' in x)
home=env['HOME']
print('RUNTIME_HOME',home)
store=home+'/.dotnet/corefx/cryptography/x509stores/my'
if os.path.isdir(store):
    for name in sorted(os.listdir(store)):
        path=os.path.join(store,name)
        if os.path.isfile(path):print('CERT_STORE_HASH',name,hashlib.sha256(open(path,'rb').read()).hexdigest())
for route in ['/health-status','/Account/Login','/Sales','/Reports/SalesRevenue','/Reports/SalesProfit','/CustomerCare/Assets','/Warranty','/CustomerCare/PendingInstallations','/.well-known/openid-configuration','/.well-known/jwks']:
    p=subprocess.run(['curl','--max-time','30','-sS','-b','/tmp/sales-v1-auth-cookie.txt','-o','/tmp/service-v1-smoke-body','-w','%{http_code} %{redirect_url}', 'http://180.93.99.150'+route],capture_output=True,text=True)
    print('GET',route,p.stdout,'exit',p.returncode)
    if route.endswith('jwks') and p.stdout.startswith('200'):
        doc=json.load(open('/tmp/service-v1-smoke-body'));print('SIGNING_KEY_IDS',json.dumps([k.get('kid') for k in doc.get('keys',[])]))
PY
