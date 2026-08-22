# Parking Pejam · Parking Operations Platform

A production-oriented parking monitoring and management platform evolved from the original Pejam WinForms prototype.

## What is included

- **Web-first operations dashboard**: responsive live map, KPI cards, recent activity and mobile-friendly UI.
- **ASP.NET Core REST API**: spots, statistics, event history and protected status changes.
- **Clean Architecture**: Domain → Application → Infrastructure → Web.
- **SQLite persistence**: real parking state and audit events instead of a flat text file.
- **Audit trail**: every status change records previous status, new status, source, actor and UTC timestamp.
- **PWA-ready**: installable web experience for tablets and phones.
- **Health check**: `/health` endpoint for monitoring and deployment checks.
- **Docker**: production-oriented container and Compose setup with persistent database volume.
- **CI**: GitHub Actions restore/build pipeline using .NET 10.
- **Legacy client retained**: the original .NET Framework 4.7.2 WinForms application remains available as a historical/local client.

## Architecture

```text
                    ┌──────────────────────────────┐
                    │ Responsive Web / PWA         │
                    │ Dashboard + Parking Map      │
                    └──────────────┬───────────────┘
                                   │ HTTP/JSON
                    ┌──────────────▼───────────────┐
                    │ ASP.NET Core Web / API       │
                    │ Auth guard + Health checks   │
                    └──────────────┬───────────────┘
                                   │
              ┌────────────────────▼────────────────────┐
              │ Application                             │
              │ Use cases / DTOs / service contracts   │
              └────────────────────┬────────────────────┘
                                   │
              ┌────────────────────▼────────────────────┐
              │ Domain                                 │
              │ ParkingSpot / ParkingEvent / statuses │
              └────────────────────┬────────────────────┘
                                   │
              ┌────────────────────▼────────────────────┐
              │ Infrastructure                         │
              │ EF Core + SQLite + persistence        │
              └────────────────────────────────────────┘
```

## Tech stack

- C# / .NET 10 LTS
- ASP.NET Core Minimal API
- Entity Framework Core 10 + SQLite
- HTML/CSS/JavaScript PWA dashboard
- Docker / Docker Compose
- GitHub Actions

## Run locally

```bash
dotnet run --project src/ParkingPejam.Web
```

The app creates `parking.db` automatically on first run and seeds 48 demo spaces across zones A–C.

For local development, status changes are allowed when `ASPNETCORE_ENVIRONMENT=Development` and no `Parking:AdminKey` is configured.

For a configured environment, set:

```bash
Parking__AdminKey="use-a-long-random-secret"
ConnectionStrings__Parking="Data Source=parking.db"
```

Then open the application root. Use the `⚙` control to enter the operator key and actor name for protected status changes.

## Docker

Create a strong secret in your environment and run:

```bash
export PARKING_ADMIN_KEY="use-a-long-random-secret"
docker compose up --build -d
```

The database is stored in a persistent Docker volume.

## API

```text
GET  /api/parking/spots
GET  /api/parking/spots/{id}
GET  /api/parking/statistics
GET  /api/parking/events?take=50
PUT  /api/parking/spots/{id}/status
GET  /health
```

Mutation requests require `X-Admin-Key` outside Development. `X-Actor` can be supplied to identify the operator in the audit trail.

## Next product-level steps

The architecture intentionally leaves room for the commercial version: real sensor/IoT ingestion, authentication with users/roles, multi-site tenancy, reservations, license-plate recognition integration, notifications, reporting, payment integration, and PostgreSQL for higher-scale deployments.

## Original application

The repository also contains the original WinForms implementation built on .NET Framework 4.7.2. It is preserved to document the evolution from the initial local prototype to the current web platform.

## Author

**Mohammad Askari Dehestani** · https://github.com/moli1369
