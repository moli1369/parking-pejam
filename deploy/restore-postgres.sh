#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"
FILE="${1:?usage: ./restore-postgres.sh /path/to/backup.dump}"
POSTGRES_USER="${POSTGRES_USER:-parking}"
POSTGRES_DB="${POSTGRES_DB:-parking_pejam}"

if [ ! -f "$FILE" ]; then
  echo "Backup file not found: $FILE" >&2
  exit 1
fi

printf '%s\n' "WARNING: restore replaces the current database contents."
printf '%s\n' "Type RESTORE to continue: "
read -r CONFIRM
[ "$CONFIRM" = "RESTORE" ] || { echo "Restore cancelled."; exit 1; }

docker compose -f docker-compose.prod.yml exec -T db \
  pg_restore -U "$POSTGRES_USER" -d "$POSTGRES_DB" --clean --if-exists < "$FILE"

echo "Restore completed."
