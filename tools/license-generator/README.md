# Parking Pejam License Generator

The generator signs a JSON license payload with the vendor's RSA private key.

## Security

Never commit the private key, a production license for a customer, or a license generator secret to Git.

Keep the private key in a secure offline location or secret manager and pass its path to the generator.

## Payload

The signed payload supports:

- `licenseId`
- `companyName`
- `installationId`
- `plan`
- `issuedAtUtc`
- `expiresAtUtc`
- `maxUsers`
- `maxYards`
- `maxVehiclesPerMonth`
- `gracePeriodDays`
- `modules`

## Example

```powershell
./New-ParkingPejamLicense.ps1 `
  -PrivateKeyPath "C:\secure\parking-pejam-private-key.pem" `
  -OutputPath "C:\secure\customer-a\license.json" `
  -LicenseId "PP-2026-000001" `
  -CompanyName "Example Importer" `
  -InstallationId "example-yard-01" `
  -Plan "Enterprise" `
  -ExpiresAtUtc "2027-08-23T00:00:00Z" `
  -MaxUsers 50 `
  -MaxYards 3 `
  -MaxVehiclesPerMonth 10000 `
  -Modules "Import,Manifest,Tally,Yard,Inspection,Customs,Gate,Dispatch,Documents,LPR,Sensors,Reports,CustomerPortal,Billing,SmartSlot"
```

The private key is read locally and never sent to the application repository.
