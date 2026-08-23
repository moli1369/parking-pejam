# Parking Pejam Security Model

Parking Pejam is designed as a defense-in-depth self-hosted application. No self-hosted application can honestly guarantee that a determined party cannot reverse-engineer or patch its binaries, especially when source code is public. Production intellectual-property protection therefore depends on keeping the production source/image private and distributing signed customer licenses.

## Production controls

- HTTPS is terminated by Caddy with automatic certificates.
- The application container runs as a non-root UID.
- Production containers drop Linux capabilities and enable `no-new-privileges`.
- The application filesystem is read-only except for its explicit data volume and temporary filesystem.
- PostgreSQL is isolated on a private Docker network and is not published to the Internet.
- Secrets are supplied by environment/secret management and are excluded from Git and Docker build context.
- Authentication uses hashed passwords and HttpOnly/SameSite cookies.
- Login is rate-limited.
- Security response headers are emitted by the application and reverse proxy.
- ASP.NET Core Data Protection keys are persisted in the application data volume so authentication remains stable across restarts.
- Commercial licenses are signed with a vendor-only RSA private key and verified with an embedded public key.
- Licenses are bound to an explicit InstallationId and can expire with an offline grace period.
- License module enforcement happens server-side; UI-only feature hiding is never treated as an authorization boundary.
- Sensor ingestion requires a separate secret header.
- Health endpoints are separated into liveness and readiness checks.

## Intellectual-property protection

The public GitHub repository is suitable for the showcase/demo and deployment templates. Do not publish the vendor private signing key, customer licenses, production database, or production-only code here.

For strong commercial protection, keep the production repository and production container image private. Give each customer a signed license and a customer-specific deployment artifact or private image credential. A public source tree cannot provide cryptographic protection against someone editing the code and removing a local license check.

## Incident response

If the vendor signing key is suspected to be exposed, stop issuing licenses with it and rotate the embedded public key in the next production release. Reissue all active customer licenses with the new key. Never store the private signing key on a customer server.

## Security reporting

Do not disclose vulnerabilities or private customer data in public GitHub issues. Report them privately to the project owner and include the affected version, reproduction steps, and impact.
