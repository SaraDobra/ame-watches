#!/usr/bin/env bash
# Ekzekutohet NJË HERË në server, si përdoruesi "ame":  bash /opt/ame/deploy/setup-server.sh amewatches.com
set -euo pipefail

DOMAIN="${1:?Përdorim: setup-server.sh amewatches.com}"
ENV_FILE=/etc/ame/api.env
SHOP_EMAIL="ame.accessories.ks@gmail.com"

if [ -f "$ENV_FILE" ]; then
  echo "$ENV_FILE ekziston tashmë. Fshije vetë (sudo rm $ENV_FILE) nëse do ta rikrijosh."
  exit 1
fi

SA=$(grep '^MSSQL_SA_PASSWORD=' /opt/ame/secrets.env | cut -d= -f2)
JWT=$(openssl rand -base64 96 | tr -dc 'A-Za-z0-9' | head -c 64)
ADMIN_PW="$(openssl rand -base64 48 | tr -dc 'A-Za-z0-9' | head -c 20)Aa1"

read -rsp "Ngjit App Password të Gmail-it (16 shkronja, nuk shfaqet): " GMAIL
echo
GMAIL="${GMAIL// /}"
[ -n "$GMAIL" ] || { echo "App Password është bosh."; exit 1; }

sudo mkdir -p /etc/ame /opt/ame/uploads
sudo chown ame:ame /opt/ame/uploads
sudo install -m 600 -o ame -g ame /dev/null "$ENV_FILE"
cat >> "$ENV_FILE" <<ENV
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:5260
ConnectionStrings__Default=Server=127.0.0.1,1433;Database=AmeWatches;User Id=sa;Password=${SA};TrustServerCertificate=True
Jwt__SecretKey=${JWT}
Admin__Email=${SHOP_EMAIL}
Admin__Password=${ADMIN_PW}
Cors__Origins__0=https://${DOMAIN}
Payments__Provider=Disabled
Payments__FrontendBaseUrl=https://${DOMAIN}
Email__Mode=Smtp
Email__Username=${SHOP_EMAIL}
Email__Password=${GMAIL}
Email__PublicBaseUrl=https://${DOMAIN}
Storage__UploadsPath=/opt/ame/uploads
ENV

sed "s/__DOMAIN__/${DOMAIN}/g" /opt/ame/deploy/Caddyfile.template | sudo tee /etc/caddy/Caddyfile >/dev/null
sed "s/__DOMAIN__/${DOMAIN}/g" /opt/ame/deploy/ame-web.service.template | sudo tee /etc/systemd/system/ame-web.service >/dev/null
sudo cp /opt/ame/deploy/ame-api.service /etc/systemd/system/ame-api.service

sudo systemctl daemon-reload
sudo systemctl enable --now ame-api ame-web
sudo systemctl reload caddy || sudo systemctl restart caddy

echo
echo "================================================================"
echo "Hyrja në panel:  https://${DOMAIN}/admin"
echo "Email:           ${SHOP_EMAIL}"
echo "Fjalëkalimi:     ${ADMIN_PW}"
echo "RUAJE TANI në password manager. Nuk shfaqet më, dhe MOS e dërgo askujt."
echo "================================================================"
