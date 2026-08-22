# Parking Pejam

A production-oriented parking monitoring and management platform.

## Features

- Responsive web operations dashboard with live parking map
- Parking status management: Free, Occupied, Reserved, Out of Service
- Audit trail for every status change
- ASP.NET Core REST API
- Clean Architecture: Domain → Application → Infrastructure → Web
- Entity Framework Core + SQLite persistence
- CSV exports compatible with Excel
- JSON snapshot export for analysis
- Printable operations report that can be saved as PDF
- PWA-ready frontend for tablets and phones
- Health check endpoint
- Docker and Docker Compose deployment
- GitHub Actions CI
- Demo simulation engine for presentations and testing

## Architecture

```text
Responsive Web / PWA
        │
        ▼
ASP.NET Core Web + REST API
        │
        ├── Application
        ├── Domain
        └── Infrastructure
              │
              └── EF Core + SQLite
```

## Tech Stack

- C# / .NET 10 LTS
- ASP.NET Core Minimal API
- Entity Framework Core 10
- SQLite
- HTML / CSS / JavaScript
- Docker / Docker Compose
- GitHub Actions

## Run locally

```bash
dotnet run --project src/ParkingPejam.Web
```

The application creates `parking.db` automatically and seeds demo parking spaces across zones A–C.

For local development, status changes are allowed when `ASPNETCORE_ENVIRONMENT=Development` and no `Parking:AdminKey` is configured.

For a configured environment:

```bash
Parking__AdminKey="use-a-long-random-secret"
ConnectionStrings__Parking="Data Source=parking.db"
```

## Docker

```bash
export PARKING_ADMIN_KEY="use-a-long-random-secret"
docker compose up --build -d
```

## API

```text
GET  /api/parking/spots
GET  /api/parking/spots/{id}
GET  /api/parking/statistics
GET  /api/parking/events?take=50
PUT  /api/parking/spots/{id}/status
GET  /api/parking/export/spots.csv
GET  /api/parking/export/events.csv?take=200
GET  /api/parking/export/report.json
GET  /health
```

Protected status changes require `X-Admin-Key` outside Development. `X-Actor` can identify the operator in the audit trail.

## Commercial roadmap

The architecture leaves room for multi-site tenancy, user roles, real sensor/IoT ingestion, reservations, license-plate recognition, notifications, payment integration, reporting, and PostgreSQL for larger deployments.

## Author

**Mohammad Askari Dehestani** · https://github.com/moli1369
