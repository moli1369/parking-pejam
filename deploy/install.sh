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

for name in POSTGRES_PASSWORD PARKING_BOOTSTRAP_ADMIN_PASSWORD PARKING_SENSOR_INGRESS_KEY PARKING_LICENSE_INSTALLATION_ID PARKING_DOMAIN; do
  value="${!name:-}"
  if [[ -z "$value" || "$value" == CHANGE_ME* ]]; then
    echo "Missing secure value: $name" >&2
    exit 1
  fi
done

[[ -f deploy/license/license.json ]] || {
  echo "Missing deploy/license/license.json. Obtain a signed commercial license from Parking Pejam." >&2
  exit 1
}

mkdir -p deploy/license
chmod 700 deploy/license
chmod 600 deploy/license/license.json

docker compose -f docker-compose.prod.yml up -d --build

echo "Parking Pejam is starting behind Caddy at https://$PARKING_DOMAIN"
echo "Use: docker compose -f docker-compose.prod.yml ps"
