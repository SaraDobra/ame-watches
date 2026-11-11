#!/usr/bin/env bash
# Tërheq kopjet e backup-it nga serveri te kompjuteri yt (që të mos ruhen vetëm në server).
# Përdorim (nga Mac-u):  ./tools/pull-backups.sh      -> ruhen te ~/ame-backups
set -euo pipefail
SERVER="${SERVER:-ame@187.126.28.204}"
TARGET="${TARGET:-$HOME/ame-backups}"
mkdir -p "$TARGET"
rsync -az --progress "$SERVER:/opt/ame/backups/" "$TARGET/"
echo "Kopjet janë te $TARGET"
ls -lh "$TARGET" | tail -n 8
