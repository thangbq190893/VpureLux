# VPureLux Server Deployment Runbook

This runbook is the single handoff document for preparing a new Ubuntu server and deploying VPureLux Web from a published artifact.

Target use case: VPureLux dev/UAT server, no domain yet, access by public IP over HTTP.

Important: do not commit real passwords into this repository. When handing this runbook to an operator or an AI agent, provide the "Secret and environment block" below in the chat/session or as a temporary server-local file, then delete it after the deployment.

## Secret And Environment Block

Provide these values once. With this block, no follow-up questions are needed for a dev/UAT deployment.

```bash
export VPS_HOST="180.93.99.150"
export VPS_SSH_USER="root"
export VPS_ROOT_PASSWORD="<root-password>"

export VPURELUX_REPO="https://github.com/thangbq190893/VpureLux"
export VPURELUX_BRANCH="main"

# Local Windows artifact paths used by the current workflow.
export LOCAL_WEB_PUBLISH_DIR='C:\SourceCode\VpureLux\src\VPureLux.Web\bin\Release\net10.0\publish'
export LOCAL_DBMIGRATOR_PROJECT='C:\SourceCode\VpureLux\src\VPureLux.DbMigrator\VPureLux.DbMigrator.csproj'

# SQL edition for dev/UAT. Use Developer for unrestricted dev/test, Express for small dev/demo.
# Do not use Developer for real production.
export VPURELUX_SQL_EDITION="Developer"

export VPURELUX_DB="VPureLux"
export SQL_SA_PASSWORD="<strong-sa-password>"
export VPURELUX_APP_LOGIN="vpurelux_app"
export VPURELUX_APP_PASSWORD="<strong-app-sql-password>"

export REDIS_PASSWORD="<strong-redis-password>"

# IP allowed to connect directly to SQL/Redis in dev. Use the office/current public IP.
# Leave empty to not expose SQL/Redis externally.
export DEV_CLIENT_IP="<client-public-ip>"

export OPENIDDCT_PFX_PASSWORD="b6b8be5a-c72c-4ee6-9377-06ce8d0541a1"
```

## Current Known Server Shape

The existing dev server was configured with:

- OS: Ubuntu 22.04.5 LTS
- CPU: 4 vCPU
- RAM: about 9.5 GiB
- Swap: 4 GiB
- Disk: about 59 GiB root volume
- .NET: SDK 10.0.109, runtime 10.0.9
- Node: v22.23.1
- Yarn: 1.22.22
- SQL Server: SQL Server 2022 Express 16.0.4255.1 initially, planned to switch to Developer later for dev/test
- Redis: Redis 6 from Ubuntu apt
- Web app: `/opt/vpurelux/app` symlink to `/opt/vpurelux/releases/web-<timestamp>`
- Nginx: port 80 reverse proxy to `http://127.0.0.1:5000`
- Health endpoint: `http://<server-ip>/health-status`

## Non-Negotiable Assumptions

- This runbook is for dev/UAT unless explicitly changed.
- No production deployment with SQL Server Developer.
- No domain/certbot unless a domain is provided and DNS points to the server.
- Do not run full test suite on the server.
- Use published artifacts for deployment. Do not rebuild Web on the server unless explicitly requested.
- ABP commercial package credentials are required only when building/restoring from source, not when copying already-published artifacts.

## 1. SSH And Bootstrap Logging

From the operator machine:

```powershell
ssh root@180.93.99.150
```

On the server:

```bash
mkdir -p /root/vpurelux-bootstrap
exec > >(tee -a /root/vpurelux-bootstrap/install.log) 2>&1
set -euo pipefail
date -Is
```

## 2. Validate Server

```bash
lsb_release -a || cat /etc/os-release
uname -a
free -h
df -h
nproc
```

Stop if:

- OS is not Ubuntu 22.04.
- RAM is below 9 GiB.
- Free disk is below 35 GiB.

## 3. Base Packages, Timezone, Swap

```bash
export DEBIAN_FRONTEND=noninteractive
apt-get update
apt-get upgrade -y
apt-get install -y \
  curl wget git unzip zip jq htop iotop ncdu net-tools ca-certificates gnupg \
  software-properties-common apt-transport-https lsb-release ufw fail2ban \
  logrotate unattended-upgrades chrony nginx certbot python3-certbot-nginx \
  openssl tar

timedatectl set-timezone Asia/Ho_Chi_Minh

if ! swapon --show | grep -q .; then
  fallocate -l 4G /swapfile
  chmod 600 /swapfile
  mkswap /swapfile
  swapon /swapfile
  grep -q '^/swapfile ' /etc/fstab || echo '/swapfile none swap sw 0 0' >> /etc/fstab
fi

printf 'vm.swappiness=10\n' >/etc/sysctl.d/99-vpurelux.conf
sysctl --system || true
```

Create the deployment user:

```bash
id deploy >/dev/null 2>&1 || adduser --disabled-password --gecos "" deploy
usermod -aG sudo deploy
id deploy
```

Do not disable root login until `deploy` SSH key login is tested.

## 4. Install .NET 10

Microsoft Ubuntu 22.04 feed may not contain `dotnet-sdk-10.0`. Use Ubuntu .NET backports.

```bash
apt-get update
apt-get install -y software-properties-common
add-apt-repository -y ppa:dotnet/backports
apt-get update
apt-cache policy dotnet-sdk-10.0
apt-cache search dotnet-sdk | sort
apt-get install -y dotnet-sdk-10.0

dotnet --info
dotnet --list-sdks
dotnet --list-runtimes
```

Expected:

- SDK 10.x
- `Microsoft.NETCore.App 10.x`
- `Microsoft.AspNetCore.App 10.x`

## 5. Install Node.js 22 LTS And Yarn

```bash
curl -fsSL https://deb.nodesource.com/setup_22.x -o /tmp/nodesource_setup.sh
bash /tmp/nodesource_setup.sh
apt-get install -y nodejs
corepack enable || true
yarn -v || npm install -g yarn --force

node -v
npm -v
yarn -v || true
```

## 6. Install SQL Server 2022

Add Microsoft repositories:

```bash
if [ ! -f /etc/apt/sources.list.d/microsoft-prod.list ]; then
  wget -q https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O /tmp/packages-microsoft-prod.deb
  dpkg -i /tmp/packages-microsoft-prod.deb
fi

curl -fsSL https://packages.microsoft.com/config/ubuntu/22.04/mssql-server-2022.list \
  -o /etc/apt/sources.list.d/mssql-server-2022.list

apt-get update
apt-cache policy mssql-server
apt-get install -y mssql-server
```

Run setup. For dev/UAT, choose Developer or Express based on the secret block. Developer is recommended for dev/UAT if the team needs no Express limits.

```bash
read -rsp 'Enter SQL SA password: ' MSSQL_SA_PASSWORD
echo
export MSSQL_SA_PASSWORD

if [ "${VPURELUX_SQL_EDITION:-Developer}" = "Developer" ]; then
  ACCEPT_EULA=Y MSSQL_PID=Developer /opt/mssql/bin/mssql-conf -n setup
elif [ "${VPURELUX_SQL_EDITION:-Developer}" = "Express" ]; then
  ACCEPT_EULA=Y MSSQL_PID=Express /opt/mssql/bin/mssql-conf -n setup
else
  echo "Unsupported SQL edition for this runbook: ${VPURELUX_SQL_EDITION}"
  exit 1
fi

unset MSSQL_SA_PASSWORD
```

Limit SQL Server memory on a 10 GiB VPS:

```bash
/opt/mssql/bin/mssql-conf set memory.memorylimitmb 5120
systemctl enable mssql-server
systemctl restart mssql-server
systemctl is-active mssql-server
```

For dev direct access, allow SQL to listen externally but rely on UFW source restriction:

```bash
/opt/mssql/bin/mssql-conf set network.ipaddress 0.0.0.0
systemctl restart mssql-server
```

Verify:

```bash
ss -ltnp | grep ':1433' || true
```

## 7. Install mssql-tools18

```bash
apt-get update
ACCEPT_EULA=Y apt-get install -y mssql-tools18 unixodbc-dev
printf 'export PATH="$PATH:/opt/mssql-tools18/bin"\n' > /etc/profile.d/mssql-tools18.sh
chmod +x /etc/profile.d/mssql-tools18.sh
export PATH="$PATH:/opt/mssql-tools18/bin"
sqlcmd "-?" | head -n 20
```

## 8. Create Database And App Login

Create a server-local credentials file. Do not store this in git.

```bash
mkdir -p /root/vpurelux-bootstrap
umask 077
cat >/root/vpurelux-bootstrap/credentials.txt <<EOF
VPURELUX_DB=${VPURELUX_DB}
VPURELUX_APP_LOGIN=${VPURELUX_APP_LOGIN}
VPURELUX_APP_PASSWORD=${VPURELUX_APP_PASSWORD}
VPURELUX_REPO=${VPURELUX_REPO}
VPURELUX_BRANCH=${VPURELUX_BRANCH}
REDIS_PASSWORD=${REDIS_PASSWORD}
EOF
chmod 600 /root/vpurelux-bootstrap/credentials.txt
```

Create DB/login:

```bash
read -rsp 'Enter SQL SA password: ' SA_PASSWORD
echo
source /root/vpurelux-bootstrap/credentials.txt

SQLCMDPASSWORD="$SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b \
  -Q "IF DB_ID(N'${VPURELUX_DB}') IS NULL CREATE DATABASE [${VPURELUX_DB}];"

SQL_LOGIN_QUERY="DECLARE @pwd nvarchar(256) = N'${VPURELUX_APP_PASSWORD}';
IF SUSER_ID(N'${VPURELUX_APP_LOGIN}') IS NULL
BEGIN
  DECLARE @sql nvarchar(max) = N'CREATE LOGIN [${VPURELUX_APP_LOGIN}] WITH PASSWORD = ' + QUOTENAME(@pwd, '''') + N', CHECK_POLICY = ON;';
  EXEC(@sql);
END;"

SQLCMDPASSWORD="$SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -Q "$SQL_LOGIN_QUERY"

SQLCMDPASSWORD="$SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -d "$VPURELUX_DB" -C -b \
  -Q "IF USER_ID(N'${VPURELUX_APP_LOGIN}') IS NULL CREATE USER [${VPURELUX_APP_LOGIN}] FOR LOGIN [${VPURELUX_APP_LOGIN}];
      IF IS_ROLEMEMBER(N'db_owner', N'${VPURELUX_APP_LOGIN}') = 0 ALTER ROLE [db_owner] ADD MEMBER [${VPURELUX_APP_LOGIN}];
      SELECT DB_NAME() AS CurrentDatabase;"

unset SA_PASSWORD
```

`db_owner` is acceptable for dev migration. For production, separate migration and runtime permissions.

## 9. Install And Configure Redis

```bash
apt-get install -y redis-server
cp /etc/redis/redis.conf /etc/redis/redis.conf.vpurelux.bak.$(date +%Y%m%d%H%M%S)

# For dev direct access. For stricter internal-only mode, use: bind 127.0.0.1 ::1
sed -i -E 's/^#? *bind .*/bind 0.0.0.0 ::/' /etc/redis/redis.conf
sed -i -E 's/^#? *protected-mode .*/protected-mode yes/' /etc/redis/redis.conf
sed -i -E 's/^#? *supervised .*/supervised systemd/' /etc/redis/redis.conf

if grep -q '^maxmemory ' /etc/redis/redis.conf; then
  sed -i -E 's/^maxmemory .*/maxmemory 768mb/' /etc/redis/redis.conf
else
  printf '\nmaxmemory 768mb\n' >> /etc/redis/redis.conf
fi

if grep -q '^maxmemory-policy ' /etc/redis/redis.conf; then
  sed -i -E 's/^maxmemory-policy .*/maxmemory-policy allkeys-lru/' /etc/redis/redis.conf
else
  printf 'maxmemory-policy allkeys-lru\n' >> /etc/redis/redis.conf
fi

if grep -q '^requirepass ' /etc/redis/redis.conf; then
  sed -i -E "s|^requirepass .*|requirepass ${REDIS_PASSWORD}|" /etc/redis/redis.conf
else
  printf '\nrequirepass %s\n' "${REDIS_PASSWORD}" >> /etc/redis/redis.conf
fi

systemctl enable redis-server
systemctl restart redis-server
REDISCLI_AUTH="${REDIS_PASSWORD}" redis-cli -h 127.0.0.1 ping
```

## 10. Configure UFW

Default web-only:

```bash
ufw allow OpenSSH
ufw allow 80/tcp
ufw allow 443/tcp
```

Dev direct SQL/Redis from a single client IP:

```bash
if [ -n "${DEV_CLIENT_IP:-}" ]; then
  ufw allow from "$DEV_CLIENT_IP" to any port 1433 proto tcp comment 'VPureLux dev SQL Server'
  ufw allow from "$DEV_CLIENT_IP" to any port 6379 proto tcp comment 'VPureLux dev Redis'
fi

ufw --force enable
ufw status verbose
```

Do not open `1433` or `6379` to `Anywhere` unless the server is disposable.

## 11. Create VPureLux Directories And System User

```bash
mkdir -p /opt/vpurelux/app
mkdir -p /opt/vpurelux/releases
mkdir -p /opt/vpurelux/shared
mkdir -p /etc/vpurelux
mkdir -p /var/log/vpurelux
mkdir -p /var/backups/vpurelux/sql

useradd --system --home /opt/vpurelux --shell /usr/sbin/nologin vpurelux || true

chown -R vpurelux:vpurelux /opt/vpurelux /var/log/vpurelux
chmod 750 /etc/vpurelux
chmod 750 /var/backups/vpurelux
chmod 750 /var/backups/vpurelux/sql
```

## 12. App Environment File

```bash
source /root/vpurelux-bootstrap/credentials.txt

cat >/etc/vpurelux/vpurelux.env <<EOF
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:5000
DOTNET_PRINT_TELEMETRY_MESSAGE=false
ConnectionStrings__Default=Server=127.0.0.1,1433;Database=${VPURELUX_DB};User Id=${VPURELUX_APP_LOGIN};Password=${VPURELUX_APP_PASSWORD};TrustServerCertificate=True;
Redis__Configuration=127.0.0.1:6379,password=${REDIS_PASSWORD}
EOF

chown root:vpurelux /etc/vpurelux/vpurelux.env
chmod 640 /etc/vpurelux/vpurelux.env
```

## 13. Systemd Service

```bash
cat >/etc/systemd/system/vpurelux-web.service <<'EOF'
[Unit]
Description=VPureLux Web
After=network.target mssql-server.service redis-server.service
Wants=mssql-server.service redis-server.service

[Service]
WorkingDirectory=/opt/vpurelux/app
ExecStart=/usr/bin/dotnet /opt/vpurelux/app/VPureLux.Web.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=vpurelux-web
User=vpurelux
EnvironmentFile=/etc/vpurelux/vpurelux.env
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
NoNewPrivileges=true

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable vpurelux-web
```

Do not start until the artifact is deployed.

## 14. Nginx Reverse Proxy

```bash
cat >/etc/nginx/sites-available/vpurelux <<'EOF'
server {
    listen 80;
    server_name _;

    client_max_body_size 50M;

    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
EOF

ln -sf /etc/nginx/sites-available/vpurelux /etc/nginx/sites-enabled/vpurelux
rm -f /etc/nginx/sites-enabled/default
nginx -t
systemctl enable nginx
systemctl reload nginx
```

Do not run certbot until a domain is provided and DNS points to the VPS.

## 15. Backup Script

```bash
cat >/opt/vpurelux/shared/backup-sql.sh <<'EOF'
#!/usr/bin/env bash
set -euo pipefail

DB_NAME="${DB_NAME:-VPureLux}"
BACKUP_DIR="${BACKUP_DIR:-/var/backups/vpurelux/sql}"
SQL_SERVER="${SQL_SERVER:-localhost}"
SQL_USER="${SQL_USER:-sa}"
CREDENTIAL_FILE="${CREDENTIAL_FILE:-/etc/vpurelux/backup-sql.env}"
KEEP_COUNT="${KEEP_COUNT:-3}"

if [[ -f "$CREDENTIAL_FILE" ]]; then
  source "$CREDENTIAL_FILE"
fi

if [[ -z "${SQL_PASSWORD:-}" ]]; then
  echo "SQL_PASSWORD must be supplied via environment or $CREDENTIAL_FILE" >&2
  exit 1
fi

mkdir -p "$BACKUP_DIR"
chmod 750 "$BACKUP_DIR"

timestamp="$(date +%Y%m%d-%H%M%S)"
bak_file="$BACKUP_DIR/${DB_NAME}-${timestamp}.bak"
gz_file="$bak_file.gz"

/opt/mssql-tools18/bin/sqlcmd -S "$SQL_SERVER" -U "$SQL_USER" -P "$SQL_PASSWORD" -C \
  -Q "BACKUP DATABASE [$DB_NAME] TO DISK = N'$bak_file' WITH INIT, COMPRESSION, CHECKSUM;"

gzip -f "$bak_file"
ls -1t "$BACKUP_DIR"/${DB_NAME}-*.bak.gz 2>/dev/null | tail -n +$((KEEP_COUNT + 1)) | xargs -r rm -f

echo "Backup completed: $gz_file"
EOF

chmod 750 /opt/vpurelux/shared/backup-sql.sh
chown vpurelux:vpurelux /opt/vpurelux/shared/backup-sql.sh
bash -n /opt/vpurelux/shared/backup-sql.sh
```

Do not enable cron until backup credentials and retention are confirmed.

## 16. Logrotate

```bash
cat >/etc/logrotate.d/vpurelux <<'EOF'
/var/log/vpurelux/*.log {
    daily
    rotate 14
    compress
    missingok
    notifempty
    copytruncate
}
EOF
```

## 17. Deploy Web Artifact From Windows Publish Folder

On Windows operator machine:

```powershell
$publish='C:\SourceCode\VpureLux\src\VPureLux.Web\bin\Release\net10.0\publish'
$archive=Join-Path $env:TEMP 'vpurelux-web-publish.tar.gz'
if (Test-Path $archive) { Remove-Item -LiteralPath $archive -Force }
tar -czf $archive -C $publish .
scp $archive root@180.93.99.150:/tmp/vpurelux-web-publish.tar.gz
```

On server:

```bash
ARCHIVE=/tmp/vpurelux-web-publish.tar.gz
RELEASE="/opt/vpurelux/releases/web-$(date +%Y%m%d-%H%M%S)"

mkdir -p "$RELEASE"
tar -xzf "$ARCHIVE" -C "$RELEASE"
test -f "$RELEASE/VPureLux.Web.dll"

systemctl stop vpurelux-web || true

if [ -e /opt/vpurelux/app ] && [ ! -L /opt/vpurelux/app ]; then
  BACKUP="/opt/vpurelux/releases/app-backup-$(date +%Y%m%d-%H%M%S)"
  mv /opt/vpurelux/app "$BACKUP"
elif [ -L /opt/vpurelux/app ]; then
  echo "Previous app symlink: $(readlink -f /opt/vpurelux/app)"
fi

ln -sfn "$RELEASE" /opt/vpurelux/app
chown -R vpurelux:vpurelux "$RELEASE" /var/log/vpurelux
chmod 750 "$RELEASE"
find "$RELEASE" -type d -exec chmod 750 {} +
find "$RELEASE" -type f -exec chmod 640 {} +
chmod 750 /opt/vpurelux /opt/vpurelux/releases /opt/vpurelux/shared
```

## 18. Publish And Deploy DbMigrator

On Windows operator machine:

```powershell
$out=Join-Path $env:TEMP 'vpurelux-db-migrator-publish'
if (Test-Path $out) { Remove-Item -LiteralPath $out -Recurse -Force }
dotnet publish 'C:\SourceCode\VpureLux\src\VPureLux.DbMigrator\VPureLux.DbMigrator.csproj' -c Release -o $out --nologo

$archive=Join-Path $env:TEMP 'vpurelux-db-migrator-publish.tar.gz'
if (Test-Path $archive) { Remove-Item -LiteralPath $archive -Force }
tar -czf $archive -C $out .
scp $archive root@180.93.99.150:/tmp/vpurelux-db-migrator-publish.tar.gz
```

On server:

```bash
MIG_ARCHIVE=/tmp/vpurelux-db-migrator-publish.tar.gz
MIG_RELEASE="/opt/vpurelux/releases/dbmigrator-$(date +%Y%m%d-%H%M%S)"

mkdir -p "$MIG_RELEASE"
tar -xzf "$MIG_ARCHIVE" -C "$MIG_RELEASE"
test -f "$MIG_RELEASE/VPureLux.DbMigrator.dll"
chown -R vpurelux:vpurelux "$MIG_RELEASE"
chmod 750 "$MIG_RELEASE"
find "$MIG_RELEASE" -type d -exec chmod 750 {} +
find "$MIG_RELEASE" -type f -exec chmod 640 {} +
```

Run migration:

```bash
systemctl stop vpurelux-web || true

cat >/root/vpurelux-bootstrap/run-db-migrator.sh <<'SCRIPT'
#!/usr/bin/env bash
set -euo pipefail
source /root/vpurelux-bootstrap/credentials.txt
export ASPNETCORE_ENVIRONMENT=Production
export DOTNET_ENVIRONMENT=Production
export ConnectionStrings__Default="Server=127.0.0.1,1433;Database=${VPURELUX_DB};User Id=${VPURELUX_APP_LOGIN};Password=${VPURELUX_APP_PASSWORD};TrustServerCertificate=True;"
export Redis__Configuration="127.0.0.1:6379,password=${REDIS_PASSWORD}"
cd "$1"
dotnet VPureLux.DbMigrator.dll
SCRIPT

chmod 700 /root/vpurelux-bootstrap/run-db-migrator.sh
/root/vpurelux-bootstrap/run-db-migrator.sh "$MIG_RELEASE"
```

Expected output:

```text
Successfully completed host database migrations.
Successfully completed all database migrations.
```

## 19. Start Web And Verify

```bash
systemctl daemon-reload
systemctl start vpurelux-web
sleep 20

systemctl is-active vpurelux-web
systemctl status vpurelux-web --no-pager -l | sed -n '1,80p'
journalctl -u vpurelux-web --no-pager -n 120
ss -ltnp | grep -E 'dotnet|:5000' || true

curl -I --max-time 15 http://127.0.0.1:5000/ || true
curl -I --max-time 15 http://127.0.0.1/ || true
curl -sS --max-time 15 http://127.0.0.1/health-status || true
```

Healthy expected:

```json
{"status":"Healthy"}
```

Access by IP:

```text
http://180.93.99.150/
```

## 20. Switch SQL Express To Developer Later

For dev/UAT only. Do not use Developer for production.

Backup first:

```bash
source /root/vpurelux-bootstrap/credentials.txt
read -rsp 'Enter SQL SA password: ' SQL_PASSWORD
echo
DB_NAME="$VPURELUX_DB" SQL_PASSWORD="$SQL_PASSWORD" /opt/vpurelux/shared/backup-sql.sh
unset SQL_PASSWORD
```

Change edition:

```bash
/opt/mssql/bin/mssql-conf set-edition
```

Choose `Developer`, accept license terms, then:

```bash
systemctl restart mssql-server
systemctl is-active mssql-server
```

Verify:

```bash
source /root/vpurelux-bootstrap/credentials.txt
SQLCMDPASSWORD="$VPURELUX_APP_PASSWORD" /opt/mssql-tools18/bin/sqlcmd \
  -S 127.0.0.1 -U "$VPURELUX_APP_LOGIN" -d "$VPURELUX_DB" -C \
  -Q "SELECT SERVERPROPERTY('Edition') AS Edition, SERVERPROPERTY('ProductVersion') AS ProductVersion;"
```

## 21. Operational Health Check

```bash
printf '\n-- OS / uptime / load --\n'
lsb_release -a || cat /etc/os-release
uptime
nproc

printf '\n-- CPU --\n'
top -bn1 | head -n 5

printf '\n-- Memory --\n'
free -h

printf '\n-- Disk --\n'
df -hT
du -sh /opt/vpurelux /var/opt/mssql /var/log /var/backups/vpurelux 2>/dev/null || true

printf '\n-- Services --\n'
for s in vpurelux-web nginx mssql-server redis-server; do
  printf '%-18s ' "$s"
  systemctl is-active "$s" || true
done

printf '\n-- Top processes --\n'
ps -eo pid,ppid,user,comm,%cpu,%mem,rss --sort=-rss | head -n 15

printf '\n-- Listeners --\n'
ss -ltnp | grep -E ':(80|443|1433|6379|5000) ' || true

printf '\n-- Web health --\n'
curl -sS --max-time 10 http://127.0.0.1/health-status || true

printf '\n-- Reboot and updates --\n'
[ -f /var/run/reboot-required ] && cat /var/run/reboot-required || echo 'no reboot-required file'
apt list --upgradable 2>/dev/null | tail -n +2 | wc -l
```

Healthy dev baseline from the existing server:

- CPU idle about 95 percent during light use.
- RAM about 1.7 GiB used, 7.2 GiB available.
- Swap 0 used.
- Disk about 22 percent used.
- `dotnet` about 1 GiB RSS.
- `sqlservr` about 1 GiB RSS.

## 22. Troubleshooting

### Nginx returns 502

Check Kestrel:

```bash
systemctl is-active vpurelux-web
ss -ltnp | grep ':5000' || true
journalctl -u vpurelux-web --no-pager -n 200
```

If logs show invalid object names such as `AbpFeatureGroups`, `AbpBackgroundJobs`, or `AbpRoles`, run DbMigrator.

### Web returns 500 after first deploy

Usually the DB is not migrated. Run DbMigrator and restart web.

### SQL is active but port 1433 not visible immediately

SQL Server may still be recovering. Wait 30-60 seconds, then:

```bash
ss -ltnp | grep ':1433' || true
tail -n 80 /var/opt/mssql/log/errorlog
```

Expected log:

```text
SQL Server is now ready for client connections.
Server is listening on [ 0.0.0.0 <ipv4> 1433]
```

### Redis exposed but client cannot connect

Check UFW source IP and password:

```bash
ufw status verbose
grep '^requirepass ' /etc/redis/redis.conf
ss -ltnp | grep ':6379' || true
```

## 23. Security Hardening Later

These are intentionally deferred for the current dev phase, but should be done before any real production use:

- Change root password.
- Change SQL SA password.
- Rotate `vpurelux_app` SQL password.
- Rotate Redis password.
- Create SSH key for `deploy`, test login, then disable root SSH and password login.
- Restrict SQL/Redis to VPN or private network only.
- Add TLS domain and certbot.
- Move secrets to a secret manager or server-only env files with limited access.
- Separate migration DB user from runtime DB user.
- Review SQL Server edition licensing before production.

