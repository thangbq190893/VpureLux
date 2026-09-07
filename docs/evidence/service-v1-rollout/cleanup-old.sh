set -eu
active=/opt/vpurelux/releases/web-20260903-180516-sales-v1-a4717aa
rollback=/opt/vpurelux/releases/web-20260825-111401
test "$(readlink -f /opt/vpurelux/app)" = "$active"
test -s "$active/VPureLux.Web.dll"
test -s "$rollback/VPureLux.Web.dll"
test -s "$rollback/VPureLux.Web.runtimeconfig.json"
test -s "$rollback/openiddict.pfx"
df -B1 /
mkdir -p /opt/vpurelux/shared/retained-release-data-20260907
while IFS= read -r path; do
  test -n "$path" || continue
  case "$path" in /opt/vpurelux/releases/web-20*|/opt/vpurelux/releases/dbmigrator-20*) ;; *) exit 31 ;; esac
  test "$(dirname "$path")" = /opt/vpurelux/releases
  test ! -L "$path"
  test "$(realpath "$path")" = "$path"
  test "$path" != "$active"
  test "$path" != "$rollback"
  if grep -rFl -- "$path" /etc/systemd/system /etc/nginx /etc/cron.d >/dev/null 2>&1; then echo "REFERENCED: $path"; exit 32; fi
  for proc in /proc/[0-9]*/cwd; do test "$(readlink -f "$proc" 2>/dev/null || true)" != "$path" || exit 33; done
  name=$(basename "$path")
  for data in Logs logs App_Data DataProtection-Keys wwwroot/uploads; do
    if test -d "$path/$data" && test ! -L "$path/$data"; then
      target="/opt/vpurelux/shared/retained-release-data-20260907/$name/$(dirname "$data")"
      mkdir -p "$target"
      mv -- "$path/$data" "$target/"
      echo "PRESERVED $path/$data"
    fi
  done
  echo "DELETE $path"
  rm -rf --one-file-system -- "$path"
done <<'PATHS'
/opt/vpurelux/releases/dbmigrator-20260701-231046
/opt/vpurelux/releases/web-20260701-230728
/opt/vpurelux/releases/dbmigrator-20260702-010210
/opt/vpurelux/releases/web-20260702-010210
/opt/vpurelux/releases/dbmigrator-20260702-020830
/opt/vpurelux/releases/web-20260702-020830
/opt/vpurelux/releases/dbmigrator-20260702-020904
/opt/vpurelux/releases/web-20260702-020904
/opt/vpurelux/releases/dbmigrator-20260702-022145
/opt/vpurelux/releases/web-20260702-022145
/opt/vpurelux/releases/dbmigrator-20260702-022915
/opt/vpurelux/releases/web-20260702-022915
/opt/vpurelux/releases/dbmigrator-20260702-023516
/opt/vpurelux/releases/web-20260702-023516
/opt/vpurelux/releases/dbmigrator-20260702-024309
/opt/vpurelux/releases/web-20260702-024309
/opt/vpurelux/releases/dbmigrator-20260702-030001
/opt/vpurelux/releases/web-20260702-030001
/opt/vpurelux/releases/dbmigrator-20260702-030932
/opt/vpurelux/releases/web-20260702-030932
/opt/vpurelux/releases/dbmigrator-20260702-032021
/opt/vpurelux/releases/web-20260702-032021
/opt/vpurelux/releases/dbmigrator-20260702-032957
/opt/vpurelux/releases/web-20260702-032957
/opt/vpurelux/releases/dbmigrator-20260702-084924
/opt/vpurelux/releases/web-20260702-084924
/opt/vpurelux/releases/dbmigrator-20260706-191409
/opt/vpurelux/releases/web-20260706-191409
/opt/vpurelux/releases/dbmigrator-20260707-002137
/opt/vpurelux/releases/web-20260707-002137
/opt/vpurelux/releases/dbmigrator-20260707-020417
/opt/vpurelux/releases/web-20260707-020417
/opt/vpurelux/releases/dbmigrator-20260707-120328
/opt/vpurelux/releases/web-20260707-120328
/opt/vpurelux/releases/dbmigrator-20260721-182233
/opt/vpurelux/releases/web-20260721-182233
/opt/vpurelux/releases/dbmigrator-20260722-092134
/opt/vpurelux/releases/web-20260722-092134
/opt/vpurelux/releases/dbmigrator-20260804-110438
/opt/vpurelux/releases/web-20260804-110438
/opt/vpurelux/releases/dbmigrator-20260806-085220
/opt/vpurelux/releases/web-20260806-085220
/opt/vpurelux/releases/dbmigrator-20260806-141511
/opt/vpurelux/releases/web-20260806-141511
/opt/vpurelux/releases/web-20260807-113556
/opt/vpurelux/releases/dbmigrator-20260807-113556
/opt/vpurelux/releases/dbmigrator-20260807-113828
/opt/vpurelux/releases/web-20260807-113828
/opt/vpurelux/releases/dbmigrator-20260807-225514
/opt/vpurelux/releases/web-20260807-225514
/opt/vpurelux/releases/dbmigrator-20260807-232948
/opt/vpurelux/releases/web-20260807-232948
/opt/vpurelux/releases/dbmigrator-20260808-003221
/opt/vpurelux/releases/web-20260808-003221
/opt/vpurelux/releases/dbmigrator-20260808-185401
/opt/vpurelux/releases/web-20260808-185401
/opt/vpurelux/releases/dbmigrator-20260808-190545
/opt/vpurelux/releases/web-20260808-190545
/opt/vpurelux/releases/dbmigrator-20260808-230506
/opt/vpurelux/releases/web-20260808-230506
/opt/vpurelux/releases/dbmigrator-20260808-233537
/opt/vpurelux/releases/web-20260808-233537
/opt/vpurelux/releases/dbmigrator-20260809-000829
/opt/vpurelux/releases/web-20260809-000829
/opt/vpurelux/releases/dbmigrator-20260809-001638
/opt/vpurelux/releases/web-20260809-001638
/opt/vpurelux/releases/dbmigrator-20260809-002820
/opt/vpurelux/releases/web-20260809-002820
/opt/vpurelux/releases/dbmigrator-20260809-003849
/opt/vpurelux/releases/web-20260809-003849
/opt/vpurelux/releases/dbmigrator-20260809-005319
/opt/vpurelux/releases/web-20260809-005319
/opt/vpurelux/releases/dbmigrator-20260809-010437
/opt/vpurelux/releases/web-20260809-010437
/opt/vpurelux/releases/dbmigrator-20260809-013307
/opt/vpurelux/releases/web-20260809-013307
/opt/vpurelux/releases/dbmigrator-20260809-014752
/opt/vpurelux/releases/web-20260809-014752
/opt/vpurelux/releases/dbmigrator-20260809-020639
/opt/vpurelux/releases/web-20260809-020639
/opt/vpurelux/releases/dbmigrator-20260809-022154
/opt/vpurelux/releases/web-20260809-022154
/opt/vpurelux/releases/dbmigrator-20260809-025307
/opt/vpurelux/releases/web-20260809-025307
/opt/vpurelux/releases/dbmigrator-20260809-230307
/opt/vpurelux/releases/web-20260809-230307
/opt/vpurelux/releases/dbmigrator-20260809-232724
/opt/vpurelux/releases/web-20260809-232724
/opt/vpurelux/releases/dbmigrator-20260809-233935
/opt/vpurelux/releases/web-20260809-233935
/opt/vpurelux/releases/dbmigrator-20260810-001429
/opt/vpurelux/releases/web-20260810-001429
/opt/vpurelux/releases/dbmigrator-20260810-002543
/opt/vpurelux/releases/web-20260810-002543
/opt/vpurelux/releases/dbmigrator-20260810-003815
/opt/vpurelux/releases/web-20260810-003815
/opt/vpurelux/releases/dbmigrator-20260810-005933
/opt/vpurelux/releases/web-20260810-005933
/opt/vpurelux/releases/dbmigrator-20260810-084803
/opt/vpurelux/releases/web-20260810-084803
/opt/vpurelux/releases/dbmigrator-20260810-135531
/opt/vpurelux/releases/web-20260810-135531
/opt/vpurelux/releases/dbmigrator-20260810-144742
/opt/vpurelux/releases/web-20260810-144742
/opt/vpurelux/releases/dbmigrator-20260810-145814
/opt/vpurelux/releases/web-20260810-145814
/opt/vpurelux/releases/dbmigrator-20260810-160833
/opt/vpurelux/releases/web-20260810-160833
/opt/vpurelux/releases/dbmigrator-20260810-163351
/opt/vpurelux/releases/web-20260810-163351
/opt/vpurelux/releases/dbmigrator-20260810-224334
/opt/vpurelux/releases/web-20260810-224334
/opt/vpurelux/releases/dbmigrator-20260811-002548
/opt/vpurelux/releases/web-20260811-002548
/opt/vpurelux/releases/dbmigrator-20260811-011622
/opt/vpurelux/releases/web-20260811-011622
/opt/vpurelux/releases/dbmigrator-20260811-013512
/opt/vpurelux/releases/web-20260811-013512
/opt/vpurelux/releases/dbmigrator-20260811-014738
/opt/vpurelux/releases/web-20260811-014738
/opt/vpurelux/releases/dbmigrator-20260811-090326
/opt/vpurelux/releases/web-20260811-090326
/opt/vpurelux/releases/dbmigrator-20260811-092220
/opt/vpurelux/releases/web-20260811-092220
/opt/vpurelux/releases/dbmigrator-20260811-101717
/opt/vpurelux/releases/web-20260811-101717
/opt/vpurelux/releases/dbmigrator-20260811-140524
/opt/vpurelux/releases/web-20260811-140524
/opt/vpurelux/releases/dbmigrator-20260812-234708
/opt/vpurelux/releases/web-20260812-234708
/opt/vpurelux/releases/dbmigrator-20260813-001130
/opt/vpurelux/releases/web-20260813-001130
/opt/vpurelux/releases/dbmigrator-20260813-003837
/opt/vpurelux/releases/web-20260813-003837
/opt/vpurelux/releases/dbmigrator-20260813-010442
/opt/vpurelux/releases/web-20260813-010442
/opt/vpurelux/releases/dbmigrator-20260813-011344
/opt/vpurelux/releases/web-20260813-011344
/opt/vpurelux/releases/dbmigrator-20260813-145255
/opt/vpurelux/releases/web-20260813-145255
/opt/vpurelux/releases/dbmigrator-20260813-202436
/opt/vpurelux/releases/web-20260813-202436
/opt/vpurelux/releases/dbmigrator-20260814-111116
/opt/vpurelux/releases/web-20260814-111116
/opt/vpurelux/releases/dbmigrator-20260814-121645
/opt/vpurelux/releases/web-20260814-121645
/opt/vpurelux/releases/dbmigrator-20260816-003731
/opt/vpurelux/releases/web-20260816-003731
/opt/vpurelux/releases/dbmigrator-20260816-010212
/opt/vpurelux/releases/web-20260816-010212
/opt/vpurelux/releases/dbmigrator-20260816-011628
/opt/vpurelux/releases/web-20260816-011628
/opt/vpurelux/releases/dbmigrator-20260816-014020
/opt/vpurelux/releases/web-20260816-014020
/opt/vpurelux/releases/dbmigrator-20260816-015455
/opt/vpurelux/releases/web-20260816-015455
/opt/vpurelux/releases/web-20260822-235524
/opt/vpurelux/releases/web-20260823-002524
/opt/vpurelux/releases/web-20260823-010107
/opt/vpurelux/releases/dbmigrator-20260823-013141
/opt/vpurelux/releases/web-20260823-013141
/opt/vpurelux/releases/web-20260824-135947
/opt/vpurelux/releases/web-20260824-143228
/opt/vpurelux/releases/web-20260824-151442
/opt/vpurelux/releases/web-20260824-154709
/opt/vpurelux/releases/web-20260824-165930
/opt/vpurelux/releases/web-20260825-110141
PATHS
for path in /tmp/vpurelux-sales-v1-web-a4717aa.tar.gz /tmp/vpurelux-sales-v1-dbmigrator-a4717aa.tar.gz /var/opt/mssql/data/VPL-pre-service-s006-20260907_113345.bak /var/opt/mssql/data/VPL-pre-warranty-20260824-134046.bak /var/opt/mssql/data/VPL-pre-service-uat-20260825-170432.bak; do
  if test -f "$path"; then stat -c '%n %s %y' "$path"; echo "DELETE $path"; rm -- "$path"; fi
done
df -B1 /
df -i /
systemctl is-active vpurelux-web
