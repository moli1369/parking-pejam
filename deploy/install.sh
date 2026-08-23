#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

command -v docker >/dev/null 2>&1 || { echo "Docker is required." >&2; exit 1; }
docker compose version >/dev/null 2>&1 || { echo "Docker Compose v2 is required." >&2; exit 1; }

if [[ ! -f .env ]]; then
  cp .env.example .env
  chmod 600 .env
  echo "Created .env from .env.example. Edit it before continuing."
  exit 1
fi

set -a
source .env
set +a

for name in PARKING_LICENSE_INSTALLATION_ID PARKING_DOMAIN; do
  value="${!name:-}"
  if [[ -z "$value" || "$value" == CHANGE_ME* ]]; then
    echo "Missing secure value: $name" >&2
    exit 1
  fi
done

SECRETS_DIR="$ROOT_DIR/deploy/secrets"
LICENSE_DIR="$ROOT_DIR/deploy/license"
mkdir -p "$SECRETS_DIR" "$LICENSE_DIR"
chmod 700 "$SECRETS_DIR" "$LICENSE_DIR"

make_secret() {
  local name="$1"
  local path="$SECRETS_DIR/$name.txt"
  if [[ ! -s "$path" ]]; then
    umask 077
    if command -v openssl >/dev/null 2>&1; then
      openssl rand -hex 32 > "$path"
    else
      head -c 48 /dev/urandom | base64 | tr -d '\n' > "$path"
      printf '\n' >> "$path"
    fi
  fi
  chmod 600 "$path"
}

make_secret postgres_password
make_secret bootstrap_admin_password
make_secret sensor_ingress_key

[[ -f "$LICENSE_DIR/license.json" ]] || {
  echo "Missing $LICENSE_DIR/license.json. Obtain a signed commercial license from Parking Pejam." >&2
  exit 1
}
chmod 600 "$LICENSE_DIR/license.json"

docker compose -f docker-compose.prod.yml config >/dev/null
docker compose -f docker-compose.prod.yml up -d --build

echo "Parking Pejam is starting behind Caddy at https://$PARKING_DOMAIN"
echo "Use: docker compose -f docker-compose.prod.yml ps"
echo "Backups: ./deploy/backup-postgres.sh"
