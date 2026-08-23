# Parking Pejam Production Docker Deployment

This directory contains the customer-facing deployment assets for a licensed self-hosted installation.

## Requirements

- Linux VPS/server with Docker Engine and Docker Compose v2
- Public DNS record pointing the customer domain to the server
- Ports 80 and 443 available for Caddy/HTTPS
- A signed `license.json` issued by the vendor

## Installation

1. Copy the repository to the server.
2. Run:

```bash
cp .env.example .env
chmod 600 .env
```

3. Edit `.env` and set:

```text
PARKING_DOMAIN=parking.customer.example
PARKING_LICENSE_INSTALLATION_ID=CUSTOMER-YARD-01
```

4. Put the signed customer license at:

```text
deploy/license/license.json
```

5. Run:

```bash
./deploy/install.sh
```

The installer creates random PostgreSQL, bootstrap-admin and sensor-ingress secrets under `deploy/secrets/`. That directory is ignored by Git.

## First login

The initial Administrator is created only when `bootstrap_admin_password.txt` is available. Retrieve that secret from the server and change the administrator password immediately after first login.

## HTTPS

Caddy automatically obtains and renews the production certificate for `PARKING_DOMAIN`. The application itself is not published directly to the host; only Caddy exposes ports 80/443.

## Backup

```bash
./deploy/backup-postgres.sh
```

## Restore

```bash
./deploy/restore-postgres.sh ./backups/parking_pejam_YYYYMMDDTHHMMSSZ.dump
```

Restore requires explicit `RESTORE` confirmation.

## License

The customer receives only a signed license file. Customers must never receive the vendor signing private key. The private key is kept by the vendor on a trusted machine or hardware-backed secret store.

The public repository contains the license verification public key and deployment templates. For stronger IP protection, production source code and production container images should be distributed privately; a public source repository cannot make locally executed software unpatchable.
