# Parking Pejam Vendor License Admin

This folder is for the **vendor-side** license issuance workflow only. It is not part of the customer-facing Parking Pejam application.

## Security boundary

- The customer application contains only the license validator and the public verification key.
- The **private signing key must never be committed to GitHub**.
- Run issuance on a trusted vendor workstation or a private deployment.
- A customer must never receive the private signing key or the issuer environment.

## Recommended workflow

1. Keep the signing private key in a protected local path or secret manager.
2. Create a license payload for the customer company and installation.
3. Sign the payload using the private key.
4. Deliver only the resulting `license.json` to the customer.
5. The customer imports the license into their Parking Pejam production installation.
6. The application verifies the signature using the embedded public key and enforces limits/modules.

## Planned vendor UI

The eventual private Vendor License Admin should provide:

- Customer/company records
- Plans (Basic / Professional / Enterprise)
- Users, yards and vehicle limits
- Module entitlements
- Installation IDs
- Issue/expiry dates
- Grace period
- License generation and download
- License revocation records
- Audit trail of issuance/revocation

The UI should be deployed separately from the customer application and protected with strong vendor authentication (preferably MFA) and network restrictions.

## Important

This public repository does not contain the production signing private key. A public codebase can be inspected and modified by a customer, so licensing is a legal/business control and signed-license enforcement is a defense-in-depth mechanism, not a substitute for server-side code confidentiality.
