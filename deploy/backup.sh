#!/usr/bin/env bash
# Backup i databazës dhe i fotove të ngarkuara. Ekzekutohet çdo natë nga ame-backup.timer.
# Ruan kopjet te BACKUP_DIR dhe fshin ato më të vjetra se KEEP_DAYS ditë.
#
# RIKTHIMI i databazës (nga një kopje db_DATA.bak.gz):
#   gunzip -k db_DATA.bak.gz
#   docker cp db_DATA.bak ame-sql-1:/var/opt/mssql/data/rikthim.bak
#   docker exec -u root ame-sql-1 chown mssql /var/opt/mssql/data/rikthim.bak   # përndryshe SQL Server nuk e lexon
#   sudo systemctl stop ame-api
#   docker exec ame-sql-1 /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$SA" \
#     -Q "RESTORE DATABASE [AmeWatches] FROM DISK = N'/var/opt/mssql/data/rikthim.bak' WITH REPLACE"
#   sudo systemctl start ame-api
# Fotot: tar -xzf uploads_DATA.tar.gz -C /opt/ame
set -Eeuo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
BACKUP_DIR="${BACKUP_DIR:-/opt/ame/backups}"
UPLOADS_DIR="${UPLOADS_DIR:-/opt/ame/uploads}"
SECRETS_FILE="${SECRETS_FILE:-/opt/ame/secrets.env}"
CONTAINER="${CONTAINER:-ame-sql-1}"
DB="${DB:-AmeWatches}"
KEEP_DAYS="${KEEP_DAYS:-14}"
STAMP="$(date +%Y-%m-%d_%H%M)"
IN_CONTAINER="/var/opt/mssql/data/backup_${STAMP}.bak"

fail() {
  python3 "$HERE/alert.py" "Backup-i dështoi" "Backup-i i natës nuk u krye (rreshti $1). Shiko: journalctl -u ame-backup -n 50" || true
}
trap 'fail $LINENO' ERR

mkdir -p "$BACKUP_DIR"
SA="$(grep '^MSSQL_SA_PASSWORD=' "$SECRETS_FILE" | cut -d= -f2)"
sqlcmd() { docker exec -e SQLCMDPASSWORD="$SA" "$CONTAINER" /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -b "$@"; }

echo "== Databaza"
sqlcmd -Q "BACKUP DATABASE [$DB] TO DISK = N'$IN_CONTAINER' WITH INIT, CHECKSUM"
sqlcmd -Q "RESTORE VERIFYONLY FROM DISK = N'$IN_CONTAINER' WITH CHECKSUM"
docker cp "$CONTAINER:$IN_CONTAINER" "$BACKUP_DIR/db_${STAMP}.bak"
docker exec "$CONTAINER" rm -f "$IN_CONTAINER"
gzip -f "$BACKUP_DIR/db_${STAMP}.bak"

echo "== Fotot"
if [ -d "$UPLOADS_DIR" ]; then
  tar -czf "$BACKUP_DIR/uploads_${STAMP}.tar.gz" -C "$(dirname "$UPLOADS_DIR")" "$(basename "$UPLOADS_DIR")"
else
  echo "Dosja e fotove ($UPLOADS_DIR) nuk ekziston, kalohet."
fi

echo "== Pastrimi (kopjet mbi $KEEP_DAYS ditë)"
find "$BACKUP_DIR" -type f \( -name 'db_*.bak.gz' -o -name 'uploads_*.tar.gz' \) -mtime +"$KEEP_DAYS" -print -delete

ls -lh "$BACKUP_DIR" | tail -n 6
echo "Backup-i u krye: $STAMP"
