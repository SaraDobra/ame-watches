#!/usr/bin/env bash
# Ekzekutohet nga kompjuteri yt, nga rrënja e projektit:  ./deploy/deploy.sh
# Teston, ndërton, ngarkon te serveri dhe rinis shërbimet.
set -euo pipefail

SERVER="${SERVER:-ame@187.126.28.204}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$(mktemp -d)"
cd "$ROOT"

echo "== 1/4 Testet"
dotnet test AmeWatches.slnx --verbosity quiet

echo "== 2/4 Ndërtimi i API-t"
dotnet publish src/AmeWatches.Api -c Release -r linux-x64 --self-contained false -o "$OUT/api" --verbosity quiet

echo "== 3/4 Ndërtimi i faqes"
(cd frontend && npm ci --silent && npm run build --silent)

echo "== 4/4 Ngarkimi te $SERVER"
# appsettings.Development.json ka sekrete lokale: nuk ngarkohet kurrë.
rsync -az --delete --exclude 'appsettings.Development.json' --exclude 'uploads' --exclude 'emails' "$OUT/api/" "$SERVER:/opt/ame/api/"
rsync -az --delete "frontend/dist/frontend/" "$SERVER:/opt/ame/web/"
rsync -az --delete "deploy/" "$SERVER:/opt/ame/deploy/"

ssh -t "$SERVER" 'if systemctl list-unit-files | grep -q "^ame-api.service"; then
  sudo systemctl restart ame-api ame-web
  for i in $(seq 1 20); do curl -fsS http://127.0.0.1:5260/health && echo && break; sleep 3; done
else
  echo "Shërbimet nuk janë instaluar ende: ekzekuto setup-server.sh në server."
fi'
rm -rf "$OUT"
echo "Mbaroi."
