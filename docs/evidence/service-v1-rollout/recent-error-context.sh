set -eu
journalctl -u vpurelux-web --since '2026-09-07 15:45:00' --until '2026-09-07 15:46:00' --no-pager -o short-iso | \
  sed -E 's#(Password|password|Token|token|Cookie|cookie|Authorization|authorization)[=:][^ ,;]+#\1=<redacted>#g'
