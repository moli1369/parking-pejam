#!/bin/sh
set -eu

read_secret() {
  file="$1"
  if [ -r "$file" ]; then
    # Strip a single trailing newline; Docker secrets are normally newline terminated.
    tr -d '\r\n' < "$file"
  else
    return 1
  fi
}

if [ -r /run/secrets/postgres_password ]; then
  export ConnectionStrings__Parking="Host=${POSTGRES_HOST:-db};Port=${POSTGRES_PORT:-5432};Database=${POSTGRES_DB:-parking_pejam};Username=${POSTGRES_USER:-parking};Password=$(read_secret /run/secrets/postgres_password)"
fi

if [ -r /run/secrets/bootstrap_admin_password ]; then
  export Parking__BootstrapAdminPassword="$(read_secret /run/secrets/bootstrap_admin_password)"
fi

if [ -r /run/secrets/sensor_ingress_key ]; then
  export Parking__SensorIngressKey="$(read_secret /run/secrets/sensor_ingress_key)"
fi

exec dotnet ParkingPejam.Web.dll
