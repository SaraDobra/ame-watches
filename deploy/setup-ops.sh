#!/usr/bin/env bash
# Ekzekutohet NJË HERË në server, si "ame":  bash /opt/ame/deploy/setup-ops.sh
# Instalon backup-in e natës dhe kontrollin e shëndetit, pastaj i provon.
set -euo pipefail

cd /opt/ame/deploy
chmod +x backup.sh healthcheck.sh alert.py
mkdir -p /opt/ame/backups

sudo cp ame-backup.service ame-backup.timer ame-health.service ame-health.timer /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now ame-backup.timer ame-health.timer

echo "== Email prove (duhet të të vijë te ame.accessories.ks@gmail.com)"
python3 alert.py "Provë e alarmit" "Nëse e lexon këtë, alarmet nga serveri punojnë."

echo "== Backup i parë tani"
sudo systemctl start ame-backup.service
sudo systemctl status ame-backup.service --no-pager | tail -n 8 || true
ls -lh /opt/ame/backups

echo
systemctl list-timers ame-backup.timer ame-health.timer --no-pager
