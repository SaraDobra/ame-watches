#!/usr/bin/env bash
# Kontrollon dyqanin çdo 2 minuta (nga ame-health.timer).
# Nëse dështon 3 herë me radhë: dërgon email dhe rinis API-n/faqen. Kur rikthehet: dërgon email njoftimi.
set -uo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
STATE_FILE="${STATE_FILE:-/var/tmp/ame-health.fails}"
LOCAL_API="${LOCAL_API:-http://127.0.0.1:5260/health}"
LOCAL_WEB="${LOCAL_WEB:-http://127.0.0.1:4000/}"
PUBLIC_URL="${PUBLIC_URL:-https://amewatches.com/health}"
RESTART_CMD="${RESTART_CMD:-sudo -n /usr/bin/systemctl restart ame-api ame-web}"

alert() { python3 "$HERE/alert.py" "$1" "$2" || true; }

failures=()
curl -fsS --max-time 10 "$LOCAL_API" | grep -q '"status":"ok"' || failures+=("API-ja ($LOCAL_API)")
curl -fsS --max-time 15 -o /dev/null "$LOCAL_WEB" || failures+=("faqja ($LOCAL_WEB)")
curl -fsS --max-time 15 "$PUBLIC_URL" | grep -q '"status":"ok"' || failures+=("adresa publike ($PUBLIC_URL)")

previous="$(cat "$STATE_FILE" 2>/dev/null || echo 0)"

if [ "${#failures[@]}" -eq 0 ]; then
  if [ "$previous" -ge 3 ]; then
    alert "Dyqani u rikthye" "Dyqani po përgjigjet përsëri normalisht."
  fi
  rm -f "$STATE_FILE"
  exit 0
fi

count=$((previous + 1))
echo "$count" > "$STATE_FILE"
list="$(printf '%s, ' "${failures[@]}")"
echo "Kontrolli dështoi ($count): ${list%, }"

if [ "$count" -eq 3 ]; then
  alert "Dyqani nuk përgjigjet" "3 kontrolle me radhë dështuan: ${list%, }. Po provoj t'i rinis shërbimet."
  $RESTART_CMD || echo "Rinisja dështoi."
elif [ "$count" -eq 10 ]; then
  alert "Dyqani ende nuk përgjigjet" "Pas rinisjes dyqani ende nuk punon (${list%, }). Duhet ndërhyrje: ssh ame@serveri dhe journalctl -u ame-api -n 50."
fi
exit 0
