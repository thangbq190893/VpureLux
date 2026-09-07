set -eu
python3 - <<'PY'
import subprocess,re,hashlib,json,os
old='/opt/vpurelux/releases/web-20260903-180516-sales-v1-a4717aa'
new='/opt/vpurelux/releases/web-20260907-152000-service-v1-b0bf197'
for name in ('appsettings.json','appsettings.secrets.json','openiddict.pfx'):
    a=hashlib.sha256(open(old+'/'+name,'rb').read()).hexdigest()
    b=hashlib.sha256(open(new+'/'+name,'rb').read()).hexdigest()
    if a!=b:raise RuntimeError('Production config/certificate drift: '+name)
    print('BYTE_MATCH',name,a)
before=open('/etc/vpurelux/vpurelux.env.pre-service-v1-20260907').read().splitlines()
after=open('/etc/vpurelux/vpurelux.env').read().splitlines()
assert [x for x in before if not x.startswith('Service__IsEnabled=')]==[x for x in after if not x.startswith('Service__IsEnabled=')]
print('UNRELATED_ENV_UNCHANGED')
logs=subprocess.check_output(['journalctl','-u','vpurelux-web','--since','2026-09-07 15:13:24','--no-pager','-o','cat'],text=True)
important=[x for x in logs.splitlines() if re.search(r'\[(ERR|FTL|WRN)\]|fail:|exception|Request finished .* - 500\b|status code 500\b',x,re.I)]
print('ERROR_WARNING_LINE_COUNT',len(important))
for line in important:
    line=re.sub(r'(?i)(password|pwd|token|cookie|authorization|secret)\s*[:=]\s*[^\s;,]+',r'\1=<redacted>',line)
    print(line[:600])
PY
df -B1 /
df -i /
systemctl show vpurelux-web -p ActiveState -p MainPID -p NRestarts --no-pager
