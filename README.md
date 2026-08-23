# Parking Pejam

A production-oriented **vehicle import yard / internal vehicle parking management platform** for importers, terminals and automotive logistics operations.

## 🚀 Live showcase

**GitHub Pages demo:** https://moli1369.github.io/parking-pejam/

The public showcase is a static interactive demo in English, German and Persian (RTL). The **real product backend** is ASP.NET Core and includes authentication, roles, persistent inventory, vessel tally intake, yard assignment, sensor ingestion and controlled vehicle dispatch.

## End-to-end vehicle workflow

```text
Vessel / shipment
      ↓
Tally intake
      ↓
VIN + vehicle details
      ↓
Received inventory
      ↓
Yard slot assignment
      ↓
Sensor / LPR monitoring
      ↓
Customs clearance
      ↓
Ready for dispatch
      ↓
Controlled vehicle exit
      ↓
Dispatch record + released slot + audit trail
```

## Core capabilities

- Import shipment / vessel / voyage / B/L registration
- Tally intake with declared vs received vs remaining count
- Unique VIN validation and duplicate prevention
- Imported vehicle inventory with Make / Model / Year / Condition / Origin / Engine / Plate / Customs / Damage notes
- Yard slot assignment linked directly to a vehicle
- Sensor-driven Free / Occupied state
- Parking sensor ingestion API with device credential
- Sensor heartbeat / online status
- Stored sensor readings (occupancy, battery and temperature)
- Audit trail for sensor, tally, yard-assignment and dispatch events
- Secure username/password login with HttpOnly cookie session
- Roles: `Admin`, `Operator`, `Viewer`
- Viewer is read-only; Operator/Admin can perform operational actions
- Controlled dispatch workflow: customs clearance check → ReadyForDispatch → final exit
- Exit record with dispatch reference, release authorization, driver, destination and carrier
- Automatic release of the yard slot after final dispatch
- Responsive operational dashboard and vehicle intelligence popup
- CSV / JSON / printable PDF reporting
- ASP.NET Core REST API + OpenAPI
- Clean Architecture: Domain → Application → Infrastructure → Web
- Entity Framework Core + SQLite persistence
- Docker / Docker Compose
- GitHub Actions CI
- GitHub Pages showcase deployment

## Import / tally API

```text
POST /api/import/shipments
GET  /api/import/shipments
GET  /api/import/shipments/{id}
POST /api/import/shipments/{id}/vehicles
GET  /api/import/vehicles/{id}
POST /api/import/vehicles/{id}/assign-slot
POST /api/import/shipments/{id}/complete
```

The backend records each vehicle against its shipment and tally sequence, prevents duplicate VINs, and keeps the declared/received/remaining counts consistent.

## Dispatch / vehicle exit API

```text
GET  /api/dispatch/candidates
POST /api/dispatch/{vehicleId}/ready
POST /api/dispatch/{vehicleId}/complete
```

A vehicle must be inside the yard and customs-cleared before it can be marked `ReadyForDispatch`. Final dispatch records the operator, dispatch reference, release authorization, driver, destination and transport company, marks the vehicle as `Dispatched`, releases its yard slot and creates an audit event.

## Sensor protocol

A sensor sends an occupancy reading to:

```text
POST /api/sensors/{externalId}/readings
X-Sensor-Key: <device-ingress-key>
Content-Type: application/json

{
  "occupied": true,
  "batteryPercent": 87,
  "temperatureC": 23.4
}
```

The backend validates the device key, stores the reading, updates `LastSeenUtc`, and changes the linked yard slot to `Occupied` or `Free`. The change is recorded in the audit trail with the sensor ID as the actor.

## Authentication

Login:

```text
POST /api/auth/login
```

Session:

```text
GET  /api/auth/me
POST /api/auth/logout
```

The first admin user is bootstrapped only when `Parking:BootstrapAdminPassword` is provided. Passwords are stored as salted password hashes, never plaintext.

## Roles

| Role | Read dashboard | Export | Operational actions | Sensor inventory |
|---|---:|---:|---:|---:|
| Viewer | ✓ | ✓ | — | ✓ |
| Operator | ✓ | ✓ | ✓ | ✓ |
| Admin | ✓ | ✓ | ✓ | ✓ |

## Run locally

Set the first admin password and sensor ingress key as environment variables, then run:

```bash
Parking__BootstrapAdminPassword="use-a-long-random-password"
Parking__SensorIngressKey="use-a-long-random-sensor-key"
dotnet run --project src/ParkingPejam.Web
```

Open `/login.html` and sign in as:

```text
username: admin
password: <your bootstrap password>
```

## Docker

```bash
export PARKING_BOOTSTRAP_ADMIN_PASSWORD="use-a-long-random-password"
export PARKING_SENSOR_INGRESS_KEY="use-a-long-random-sensor-key"
docker compose up --build -d
```

## API

```text
POST /api/auth/login
POST /api/auth/logout
GET  /api/auth/me

GET  /api/parking/spots
GET  /api/parking/spots/{id}
GET  /api/parking/statistics
GET  /api/parking/events?take=50
PUT  /api/parking/spots/{id}/status
GET  /api/parking/export/spots.csv
GET  /api/parking/export/events.csv?take=200
GET  /api/parking/export/report.json

POST /api/import/shipments
GET  /api/import/shipments
POST /api/import/shipments/{id}/vehicles
GET  /api/import/vehicles/{id}
POST /api/import/vehicles/{id}/assign-slot
POST /api/import/shipments/{id}/complete

GET  /api/dispatch/candidates
POST /api/dispatch/{vehicleId}/ready
POST /api/dispatch/{vehicleId}/complete

POST /api/sensors/{externalId}/readings
GET  /api/sensors
GET  /health
```

## Architecture

```text
       Vessel / Tally Users
                │
                ▼
       Import / Intake API
                │
                ▼
        Vehicle Inventory
                │
        ┌───────┴────────┐
        │                │
        ▼                ▼
   Yard Assignment   Customs / Dispatch
        │                │
        ▼                ▼
   Sensors / LPR    Controlled Vehicle Exit
        │                │
        └───────┬────────┘
                ▼
        EF Core / SQLite
                │
                ▼
      Authenticated Web UI
```

## Important deployment note

GitHub Pages can host the **showcase demo** but cannot run the ASP.NET Core backend or securely process user authentication and live sensor traffic. The real product therefore needs a backend deployment (Docker/server/cloud) behind HTTPS.

## Author

**Mohammad Askari Dehestani** · https://github.com/moli1369
