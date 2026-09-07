set -eu
df -B1 /
df -i /
readlink -f /opt/vpurelux/app
find /opt/vpurelux/releases -mindepth 1 -maxdepth 1 -type d -printf '%T@ %p\n' | sort -n
find /opt/vpurelux/backups /opt/vpurelux/shared /root /tmp -maxdepth 2 -type f -printf '%s %TY-%Tm-%Td %p\n' | sort -nr | head -50
python3 - <<'PY'
import os,json,subprocess,hashlib
pid=subprocess.check_output(['systemctl','show','vpurelux-web','-p','MainPID','--value'],text=True).strip()
env=dict(x.split('=',1) for x in open('/proc/'+pid+'/environ','rb').read().decode().split('\0') if '=' in x)
current=os.path.realpath('/opt/vpurelux/app')
print('ENV_KEYS',json.dumps(sorted(env)))
for k in sorted(env):
    if k.startswith(('Service__','CustomerCare__')) or k in ('ASPNETCORE_ENVIRONMENT','ASPNETCORE_URLS'):
        print(k,env[k])
files={}
merged={}
def merge(a,b):
    for k,v in b.items():
        if isinstance(v,dict): merge(a.setdefault(k,{}),v)
        else:a[k]=v
for name in ['appsettings.json','appsettings.Production.json','appsettings.secrets.json']:
    path=os.path.join(current,name)
    if os.path.isfile(path):
        data=open(path,'rb').read();files[name]=hashlib.sha256(data).hexdigest()
        merge(merged,json.loads(data.decode('utf-8-sig')))
print('CONFIG_HASHES',json.dumps(files))
print('CONNECTION_ENV_PRESENT','ConnectionStrings__Default' in env)
cs=env.get('ConnectionStrings__Default',merged.get('ConnectionStrings',{}).get('Default',''))
parts=dict(x.split('=',1) for x in cs.split(';') if '=' in x)
print('CONNECTION_TARGET',json.dumps({k:v for k,v in parts.items() if k.lower() in ('data source','server','initial catalog','database')}))
print('CONFIG_SERVICE',json.dumps(merged.get('Service',{})))
print('CONFIG_CUSTOMERCARE',json.dumps(merged.get('CustomerCare',{})))
cert=os.path.join(current,'openiddict.pfx')
pw=env.get('AuthServer__CertificatePassPhrase',merged.get('AuthServer',{}).get('CertificatePassPhrase',''))
result=subprocess.run(['openssl','pkcs12','-in',cert,'-clcerts','-nokeys','-passin','stdin'],input=pw+'\n',text=True,capture_output=True)
if result.returncode: raise RuntimeError('Certificate inspection failed; password not printed')
certinfo=subprocess.run(['openssl','x509','-noout','-subject','-issuer','-dates','-fingerprint','-sha256'],input=result.stdout,text=True,capture_output=True,check=True)
print(certinfo.stdout)
print('PFX_SHA256',hashlib.sha256(open(cert,'rb').read()).hexdigest())
PY
