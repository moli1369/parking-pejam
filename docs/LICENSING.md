# Parking Pejam Licensing

Parking Pejam uses a proprietary commercial licensing model for production use.

## Runtime model

The application verifies a signed `license.json` file with an embedded RSA public key. The corresponding private signing key is never stored in the application repository.

`Licensing:RequireLicense=false` is intended only for local development and the public demo. Production deployments must use `Licensing:RequireLicense=true` and provide a valid signed license.

## License claims

A license can contain:

- License ID
- Company name
- Installation ID
- Plan
- Issue and expiry dates
- Offline grace period
- Maximum users
- Maximum yards
- Maximum vehicles per month
- Enabled modules

## Activation

Set the installation identifier in the production environment and mount the signed license file as `license.json`.

Example environment values:

```env
Licensing__RequireLicense=true
Licensing__LicensePath=/app/license/license.json
Licensing__InstallationId=customer-abc-yard-01
```

The application exposes `/api/license/status` for operational diagnostics.

## Signing

Keep the vendor private RSA key outside Git and outside customer servers. The repository contains only the public verification key. A small signing utility is provided under `tools/license-generator` and expects the private key to be supplied from a secure local path.

## Commercial terms

The technical runtime check does not replace a written commercial agreement. Customer pricing, support, SLA, hosting, renewal, seat limits and permitted deployments should be defined in the customer contract.
