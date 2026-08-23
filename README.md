# Parking Pejam

A production-oriented **vehicle import yard / internal vehicle parking management platform** for importers, terminals and automotive logistics operations.

## 🚀 Live showcase

**GitHub Pages demo:** https://moli1369.github.io/parking-pejam/

The public showcase is a static interactive demo in English, German and Persian (RTL). The real product backend is ASP.NET Core with authentication, persistent inventory, vessel tally intake, yard assignment, sensor ingestion and controlled vehicle dispatch.

## End-to-end operating model

```text
Vessel / Manifest
      ↓
Tally + VIN reconciliation
      ↓
Vehicle arrival / inspection / damage evidence
      ↓
Customs + operational holds
      ↓
Yard hierarchy + slot assignment + QR
      ↓
Sensors / LPR / dwell monitoring
      ↓
Documents + customer + key control
      ↓
Load planning + Gate-Out
      ↓
Dispatch record + released slot + audit
      ↓
Billing / analytics / reports
```

## Product modules

- **Imports:** vessels, voyage, B/L, shipment, manifest and tally reconciliation
- **Vehicle file:** VIN, engine, model, condition, origin, customs, damage and lifecycle
- **Inspection:** pass/fail, damage codes, inspector, notes and photo evidence metadata
- **Holds:** customs, inspection, document, customer, damage and operational holds
- **Yard:** Yard → Zone → Block → Row → Bay → Slot hierarchy with QR tokens
- **Gate:** gate-in / gate-out visit records with vehicle, driver, truck and operator trace
- **Dispatch:** load plans, vehicle sequence, ready-for-dispatch checks and controlled exit
- **Documents:** digital vehicle file metadata for B/L, customs, invoice, permits, release and POD
- **Customer:** customer accounts and vehicle-to-customer links
- **Keys:** key number assignment and return tracking
- **Billing:** storage, inspection, wash, repair, transfer and loading activities
- **Sensors:** occupancy, heartbeat, battery and temperature ingestion
- **LPR:** plate detection events, confidence and camera source
- **Analytics:** dwell / aging buckets and deterministic smart-slot suggestions
- **Reporting:** CSV / JSON / printable PDF plus operational events
- **Security:** username/password login, HttpOnly cookie session and Admin/Operator/Viewer roles
- **Deployment:** Docker / Compose, OpenAPI, health check and GitHub Actions CI

## Core APIs

### Import / manifest

```text
POST /api/import/shipments
GET  /api/import/shipments
GET  /api/import/shipments/{id}
POST /api/import/shipments/{id}/vehicles
GET  /api/import/vehicles/{id}
POST /api/import/vehicles/{id}/assign-slot
POST /api/import/shipments/{id}/complete

GET  /api/ops/manifest/{shipmentId}
POST /api/ops/manifest/{shipmentId}
GET  /api/ops/manifest/{shipmentId}/reconcile
```

### Inspection / holds

```text
POST /api/ops/vehicles/{vehicleId}/inspection
POST /api/ops/vehicles/{vehicleId}/holds
GET  /api/ops/vehicles/{vehicleId}/holds
POST /api/ops/holds/{id}/release
```

### Yard / Gate / Dispatch

```text
GET  /api/ops/yard/nodes
POST /api/ops/yard/nodes
POST /api/ops/yard/nodes/{id}/qr
POST /api/ops/gate/visits
POST /api/ops/gate/visits/{id}/complete
POST /api/ops/dispatch/plans
POST /api/ops/dispatch/plans/{planId}/items
POST /api/ops/dispatch/plans/{planId}/complete

GET  /api/dispatch/candidates
POST /api/dispatch/{vehicleId}/ready
POST /api/dispatch/{vehicleId}/complete
```

### Vehicle file / commercial operations

```text
POST /api/ops/vehicles/{vehicleId}/documents
GET  /api/ops/vehicles/{vehicleId}/documents
GET  /api/ops/customers
POST /api/ops/customers
POST /api/ops/vehicles/{vehicleId}/customer
POST /api/ops/vehicles/{vehicleId}/keys/assign
POST /api/ops/keys/{id}/return
POST /api/ops/vehicles/{vehicleId}/billing
GET  /api/ops/vehicles/{vehicleId}/billing
POST /api/ops/lpr/detections
GET  /api/ops/analytics/aging
GET  /api/ops/vehicles/{vehicleId}/slot-suggestion
```

### Sensors / core

```text
POST /api/sensors/{externalId}/readings
GET  /api/sensors
GET  /api/parking/spots
GET  /api/parking/statistics
GET  /api/parking/events
GET  /health
```

## Security model

- `Viewer`: dashboard, inventory and reporting only
- `Operator`: operational intake, yard, inspection, holds, gate and dispatch actions
- `Admin`: operator permissions plus administration
- Passwords are stored as salted hashes and sessions use HttpOnly cookies
- Sensor devices authenticate separately with `X-Sensor-Key`

## Run locally

Set the secrets as environment variables:

```bash
Parking__BootstrapAdminPassword="use-a-long-random-password"
Parking__SensorIngressKey="use-a-long-random-sensor-key"
dotnet run --project src/ParkingPejam.Web
```

Then open `/login.html` and sign in as `admin` with the bootstrap password.

## Docker

```bash
export PARKING_BOOTSTRAP_ADMIN_PASSWORD="use-a-long-random-password"
export PARKING_SENSOR_INGRESS_KEY="use-a-long-random-sensor-key"
docker compose up --build -d
```

## Architecture

```text
                         ┌──────────────────────┐
                         │  Web / PWA / Mobile  │
                         └──────────┬───────────┘
                                    │
                             ASP.NET Core API
                                    │
        ┌───────────────────────────┼────────────────────────────┐
        │                           │                            │
   Import / Tally               Yard / Gate              Sensor / LPR
   Manifest / VIN               Dispatch / Hold           Telemetry
        │                           │                            │
        └───────────────────────────┼────────────────────────────┘
                                    │
                              EF Core / SQLite
                                    │
                             Audit + Reporting
```

## Important deployment note

GitHub Pages hosts the showcase demo only. The real backend, authentication, database and live sensor traffic require a backend deployment behind HTTPS, such as Docker on a server or cloud environment.

## Author

**Mohammad Askari Dehestani** · https://github.com/moli1369
