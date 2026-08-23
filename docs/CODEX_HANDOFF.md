# Parking Pejam — Codex Handoff

## Product
Parking Pejam is a vehicle import yard / internal vehicle parking management platform for automotive importers and terminals. It is **not** a public city-parking application.

## Current architecture
- .NET 10 / ASP.NET Core
- Clean Architecture: Domain → Application → Infrastructure → Web
- EF Core
- SQLite for lightweight/demo/local use
- PostgreSQL provider for production (`Database:Provider=postgres`)
- Cookie authentication + roles: Admin, Operator, Viewer
- Sensor ingress API using `X-Sensor-Key`
- GitHub Pages showcase in `docs/`
- Production Docker stack in `docker-compose.prod.yml`
- Nginx reverse proxy in `deploy/nginx.conf`

## Vehicle lifecycle
Vessel/Shipment → Manifest → Tally/Arrival → Vehicle Inventory → Inspection → Holds/Customs → Yard allocation → Sensor/LPR monitoring → Aging/Dwell → Dispatch planning → Gate-out → Dispatch record → Audit.

## Implemented domain areas
- ImportShipment / ImportedVehicle / VehicleArrivalRecord / VehicleDispatchRecord
- Manifest reconciliation
- Inspection + damage metadata
- Vehicle holds and release
- Yard hierarchy + QR tokens
- Gate visits
- Drivers / Transport Trucks
- Dispatch Load Plans / Load Items
- Vehicle Documents
- Customer Accounts / Vehicle Links
- Key Assignments
- Billing Activities
- LPR detections
- Aging/Dwell analytics
- Smart slot suggestion
- Parking sensors / readings / heartbeat
- Audit events

## Important APIs
### Auth
- POST `/api/auth/login`
- GET `/api/auth/me`
- POST `/api/auth/logout`

### Parking / Sensors
- GET `/api/parking/spots`
- GET `/api/parking/statistics`
- GET `/api/parking/events`
- POST `/api/sensors/{externalId}/readings`
- GET `/api/sensors`

### Import / Dispatch
See `MapImportWorkflow()` and `MapDispatchWorkflow()` in the Web project.

### Advanced Operations
`/api/ops/*` includes manifest, inspection, holds, yard, gate, dispatch, documents, customers, keys, billing, LPR, aging and smart-slot endpoints.

### Query API
- GET `/api/ops/query/overview`
- GET `/api/ops/query/vehicles?search=...`
- GET `/api/ops/query/shipments`
- GET `/api/ops/query/vehicles/{id}/summary`

## Production
Use `docker-compose.prod.yml` with PostgreSQL.
Required environment variables:
- `POSTGRES_PASSWORD`
- `PARKING_BOOTSTRAP_ADMIN_PASSWORD`
- `PARKING_SENSOR_INGRESS_KEY`

Database provider:
- `Database__Provider=postgres`

## Next priorities
1. Formal EF Core migrations for PostgreSQL instead of `EnsureCreated` in production.
2. File/object storage with authenticated download endpoints for inspection photos and documents.
3. Production Operations UI fully wired to `/api/ops` and `/api/ops/query`.
4. Customer role + tenant/customer-scoped authorization.
5. Multi-company / multi-yard isolation.
6. Offline-first PWA workflows for tally/inspection/gate staff.
7. LPR/OCR integration adapters.
8. ERP/EDI/Webhook integration layer.
9. Backup/restore automation and observability.
10. Automated unit/integration tests and green CI.

## Important constraint
Do not put real passwords, sensor keys, SSH keys, database secrets, or certificates into GitHub. Keep them in environment variables or secret management.
