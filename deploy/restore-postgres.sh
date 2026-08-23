#!/usr/bin/env bash
set -euo pipefail

FILE="${1:?usage: ./restore-postgres.sh /path/to/backup.dump}"
: "${POSTGRES_USER:?POSTGRES_USER is required}"
: "${POSTGRES_DB:?POSTGRES_DB is required}"

if [ ! -f "$FILE" ]; then
  echo "Backup file not found: $FILE" >&2
  exit 1
fi

echo "WARNING: restore replaces the current database contents."

docker compose -f docker-compose.prod.yml exec -T db \
  pg_restore -U "$POSTGRES_USER" -d "$POSTGRES_DB" --clean --if-exists < "$FILE"

echo "Restore completed."
