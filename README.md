# Parking Pejam

A production-oriented **parking monitoring and management platform** built as a web-first product.

## 🚀 Live demo

**Interactive three-language showcase:** https://moli1369.github.io/parking-pejam/

The public demo supports **English, German and Persian**, including RTL mode for Persian. It runs without a backend and includes local simulation, search/filtering, parking-state changes, audit activity and browser-based exports.

## Features

- Responsive operations dashboard with live parking map
- EN / DE / FA localization with RTL Persian layout
- Parking status management: Free, Occupied, Reserved, Out of Service
- Search, zone filters and status filters
- Audit trail for every status change
- Live simulation mode for presentations and testing
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
- GitHub Pages showcase deployment

## Architecture

```text
                         Public Showcase
                       GitHub Pages / Demo
                              │
                              │ static
                              ▼
                  ┌─────────────────────────┐
                  │ Responsive Web / PWA    │
                  │ Dashboard + Parking Map │
                  └────────────┬────────────┘
                               │ HTTP/JSON
                  ┌────────────▼────────────┐
                  │ ASP.NET Core Web / API  │
                  │ Auth guard + Health     │
                  └────────────┬────────────┘
                               │
            ┌──────────────────▼──────────────────┐
            │ Application                         │
            │ Use cases / DTOs / contracts       │
            └──────────────────┬──────────────────┘
                               │
            ┌──────────────────▼──────────────────┐
            │ Domain                             │
            │ ParkingSpot / ParkingEvent        │
            └──────────────────┬──────────────────┘
                               │
            ┌──────────────────▼──────────────────┐
            │ Infrastructure                     │
            │ EF Core + SQLite / persistence    │
            └────────────────────────────────────┘
```

## Tech stack

- C# / .NET 10 LTS
- ASP.NET Core Minimal API
- Entity Framework Core 10
- SQLite
- HTML / CSS / JavaScript PWA frontend
- Docker / Docker Compose
- GitHub Actions
- GitHub Pages

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

The architecture is ready for the next product layer: multi-site tenancy, user roles, real sensor/IoT ingestion, reservations, license-plate recognition, notifications, reporting, payment integration, PostgreSQL for larger deployments, and external integrations.

## Repository structure

```text
parking-pejam/
├── docs/                         # GitHub Pages interactive showcase
├── src/
│   ├── ParkingPejam.Domain/     # Business entities and rules
│   ├── ParkingPejam.Application/ # Contracts and application services
│   ├── ParkingPejam.Infrastructure/ # EF Core + SQLite
│   └── ParkingPejam.Web/        # API + dashboard
├── Dockerfile
├── docker-compose.yml
└── .github/workflows/            # CI + Pages deployment
```

## Author

**Mohammad Askari Dehestani** · https://github.com/moli1369
