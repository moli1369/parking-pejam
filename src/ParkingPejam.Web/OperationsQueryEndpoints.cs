using Microsoft.EntityFrameworkCore;
using ParkingPejam.Domain.Entities;
using ParkingPejam.Infrastructure;

namespace ParkingPejam.Web;

public static class OperationsQueryEndpoints
{
    public static IEndpointRouteBuilder MapOperationsQuery(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/ops/query").RequireAuthorization();

        api.MapGet("/overview", async (ParkingDbContext db, CancellationToken ct) =>
        {
            var now = DateTimeOffset.UtcNow;
            var vehicles = await db.ImportedVehicles.AsNoTracking().ToListAsync(ct);
            var activeHolds = await db.VehicleHolds.AsNoTracking().CountAsync(x => x.Status != HoldStatus.Released, ct);
            var sensors = await db.ParkingSensors.AsNoTracking().ToListAsync(ct);
            var onlineSensors = sensors.Count(x => x.LastSeenUtc != null && x.LastSeenUtc > now.AddMinutes(-2));
            var dispatchReady = vehicles.Count(x => x.InventoryStatus == VehicleInventoryStatus.ReadyForDispatch);
            var inYard = vehicles.Count(x => x.InventoryStatus == VehicleInventoryStatus.InYard);
            var dwell30 = vehicles.Count(x => x.InventoryStatus != VehicleInventoryStatus.Dispatched && (now - x.ReceivedAtUtc).TotalDays >= 30);
            return Results.Ok(new
            {
                vehicles = vehicles.Count,
                inYard,
                dispatchReady,
                activeHolds,
                sensors = sensors.Count,
                onlineSensors,
                offlineSensors = sensors.Count - onlineSensors,
                dwell30Plus = dwell30
            });
        });

        api.MapGet("/vehicles", async (string? search, ParkingDbContext db, CancellationToken ct) =>
        {
            var q = db.ImportedVehicles.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToUpperInvariant();
                q = q.Where(x => x.Vin.Contains(s) || x.Make.Contains(s) || x.Model.Contains(s) || (x.TemporaryPlate != null && x.TemporaryPlate.Contains(s)));
            }
            var rows = await q.OrderByDescending(x => x.UpdatedAtUtc).Take(200).Select(x => new
            {
                x.Id,
                x.Vin,
                x.Make,
                x.Model,
                x.ModelYear,
                x.Color,
                x.TemporaryPlate,
                x.CustomsStatus,
                x.Condition,
                x.InventoryStatus,
                x.ReceivedAtUtc,
                slot = db.ParkingSpots.Where(s => s.ImportedVehicleId == x.Id).Select(s => new { s.SpotNumber, s.Zone, s.Row, s.Column }).SingleOrDefault()
            }).ToListAsync(ct);
            return Results.Ok(rows);
        });

        api.MapGet("/shipments", async (ParkingDbContext db, CancellationToken ct) =>
            Results.Ok(await db.ImportShipments.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(100).Select(x => new
            {
                x.Id,
                x.VesselName,
                x.VoyageNumber,
                x.PortOfEntry,
                x.ShipmentReference,
                x.BillOfLadingNumber,
                x.DeclaredVehicleCount,
                Received = db.ImportedVehicles.Count(v => v.ImportShipmentId == x.Id),
                x.Status,
                x.CreatedAtUtc
            }).ToListAsync(ct)));

        api.MapGet("/vehicles/{id:guid}/summary", async (Guid id, ParkingDbContext db, CancellationToken ct) =>
        {
            var vehicle = await db.ImportedVehicles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
            if (vehicle is null) return Results.NotFound();
            var slot = await db.ParkingSpots.AsNoTracking().Where(x => x.ImportedVehicleId == id).Select(x => new { x.SpotNumber, x.Zone, x.Row, x.Column }).SingleOrDefaultAsync(ct);
            var inspections = await db.VehicleInspections.AsNoTracking().Where(x => x.ImportedVehicleId == id).OrderByDescending(x => x.InspectedAtUtc).Take(10).ToListAsync(ct);
            var holds = await db.VehicleHolds.AsNoTracking().Where(x => x.ImportedVehicleId == id).OrderByDescending(x => x.CreatedAtUtc).Take(20).ToListAsync(ct);
            var documents = await db.VehicleDocuments.AsNoTracking().Where(x => x.ImportedVehicleId == id).OrderByDescending(x => x.UploadedAtUtc).Take(20).ToListAsync(ct);
            var lpr = await db.VehicleLprDetections.AsNoTracking().Where(x => x.ImportedVehicleId == id).OrderByDescending(x => x.DetectedAtUtc).Take(20).ToListAsync(ct);
            var gate = await db.GateVisits.AsNoTracking().Where(x => x.ImportedVehicleId == id).OrderByDescending(x => x.StartedAtUtc).Take(20).ToListAsync(ct);
            var billing = await db.BillingActivities.AsNoTracking().Where(x => x.ImportedVehicleId == id).OrderByDescending(x => x.ActivityAtUtc).Take(50).ToListAsync(ct);
            var keys = await db.KeyAssignments.AsNoTracking().Where(x => x.ImportedVehicleId == id).OrderByDescending(x => x.AssignedAtUtc).Take(20).ToListAsync(ct);
            return Results.Ok(new { vehicle, slot, inspections, holds, documents, lpr, gate, billing, keys });
        });

        return endpoints;
    }
}
