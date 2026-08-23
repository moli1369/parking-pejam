# Secure Docker Deployment

## What the customer receives

The customer deployment bundle contains:

- `Dockerfile`
- `docker-compose.prod.yml`
- `.env.example`
- `deploy/Caddyfile`
- `deploy/install.sh`
- `deploy/license/license.json` (customer-specific, signed by the vendor)

The vendor private signing key is never distributed to a customer and is never committed to Git.

## Production setup

1. Install Docker Engine and Docker Compose v2 on an Ubuntu LTS server.
2. Copy the repository/deployment bundle to the server.
3. Create `.env` from `.env.example` and replace every `CHANGE_ME` value with long random secrets.
4. Put the signed customer license at `deploy/license/license.json`.
5. Point the DNS A/AAAA record for `PARKING_DOMAIN` to the server.
6. Run:

```bash
bash deploy/install.sh
```

Caddy obtains and renews the public TLS certificate automatically when the DNS record resolves correctly.

## Network model

Only Caddy publishes ports 80/443. PostgreSQL has no host port and is reachable only from the private Docker network. The application container is also not published directly.

## Container hardening

The application runs as a non-root UID, drops Linux capabilities, uses `no-new-privileges`, has a read-only root filesystem, and has a small writable data volume plus a temporary filesystem.

## License model

The license is RSA-SHA256 signed. The application contains only the public key. The private signing key stays with Parking Pejam.

Each license should use a unique `InstallationId` and a customer-specific expiry/plan/module set. Copying the file to another installation therefore fails validation when the configured InstallationId differs.

## Backups

Create a PostgreSQL backup with:

```bash
export POSTGRES_USER=parking
export POSTGRES_DB=parking_pejam
bash deploy/backup-postgres.sh
```

Restore only during a controlled maintenance window:

```bash
export POSTGRES_USER=parking
export POSTGRES_DB=parking_pejam
bash deploy/restore-postgres.sh ./backups/parking_pejam_<timestamp>.dump
```

## Secrets

Never put `.env`, `license.json`, PEM keys, database dumps, or customer documents in the public repository.

## Strong commercial protection

A public source tree cannot prevent a determined attacker from modifying the source and rebuilding the application with licensing disabled. For paid Production, keep the production source repository and production image private. Publish only the demo/showcase repository and deployment templates publicly.

For stronger anti-copy protection than offline licenses, add a vendor activation service that issues short-lived signed activations and periodically revalidates installations. The application can retain an offline grace period for temporary network loss.
