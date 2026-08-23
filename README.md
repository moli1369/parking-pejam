# Parking Pejam

A production-oriented **sensor-driven parking monitoring and management platform** built as a web-first product.

## 🚀 Live showcase

**GitHub Pages demo:** https://moli1369.github.io/parking-pejam/

The public showcase is a static interactive demo in English, German and Persian (RTL). The **real product backend** is ASP.NET Core and includes authentication, roles, persistent parking state and sensor ingestion.

## Core capabilities

- Sensor-driven Free / Occupied state
- Parking sensor ingestion API with device credential
- Sensor heartbeat / online status
- Stored sensor readings (occupancy, battery and temperature)
- Audit trail with `source=sensor` for automatic state changes
- Secure username/password login with HttpOnly cookie session
- Roles: `Admin`, `Operator`, `Viewer`
- Viewer is read-only; Operator/Admin can change state manually
- Responsive live parking map and operational dashboard
- CSV / JSON / printable PDF reporting
- ASP.NET Core REST API + OpenAPI
- Clean Architecture: Domain → Application → Infrastructure → Web
- Entity Framework Core + SQLite persistence
- Docker / Docker Compose
- GitHub Actions CI
- GitHub Pages showcase deployment

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

The backend validates the device key, stores the reading, updates `LastSeenUtc`, and changes the linked parking spot to `Occupied` or `Free`. The change is recorded in the audit trail with the sensor ID as the actor.

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

| Role | Read dashboard | Export | Change spot | Sensor inventory |
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

POST /api/sensors/{externalId}/readings
GET  /api/sensors
GET  /health
```

## Architecture

```text
        Sensors / IoT Devices
                 │
                 │ X-Sensor-Key
                 ▼
        ASP.NET Core Sensor API
                 │
          persisted readings
                 │
        ┌────────▼────────┐
        │  EF Core/SQLite │
        └────────┬────────┘
                 │
      ┌──────────▼──────────┐
      │ Parking Domain      │
      │ Spots + Events      │
      └──────────┬──────────┘
                 │
      ┌──────────▼──────────┐
      │ Authenticated Web   │
      │ Dashboard / PWA     │
      └─────────────────────┘
```

## Important deployment note

GitHub Pages can host the **showcase demo** but cannot run the ASP.NET Core backend or securely process user authentication and live sensor traffic. The real product therefore needs a backend deployment (Docker/server/cloud) behind HTTPS.

## Author

**Mohammad Askari Dehestani** · https://github.com/moli1369
