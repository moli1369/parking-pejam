using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ParkingPejam.Domain.Entities;
using ParkingPejam.Infrastructure;

namespace ParkingPejam.Web;

public static class DispatchWorkflowEndpoints
{
    public static IEndpointRouteBuilder MapDispatchWorkflow(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/dispatch").RequireAuthorization();

        group.MapGet("/candidates", async (ParkingDbContext db, CancellationToken ct) =>
            Results.Ok(await db.ImportedVehicles.AsNoTracking()
                .Where(v => v.InventoryStatus == VehicleInventoryStatus.InYard || v.InventoryStatus == VehicleInventoryStatus.ReadyForDispatch)
                .OrderBy(v => v.UpdatedAtUtc)
                .Select(v => new
                {
                    v.Id, v.Vin, v.Make, v.Model, v.ModelYear, v.TemporaryPlate,
                    v.CustomsStatus, v.InventoryStatus, v.ReceivedAtUtc, v.UpdatedAtUtc,
                    Slot = db.ParkingSpots.AsNoTracking().Where(s => s.ImportedVehicleId == v.Id)
                        .Select(s => s.SpotNumber).SingleOrDefault()
                }).ToListAsync(ct)));

        group.MapPost("/{vehicleId:guid}/ready", async (Guid vehicleId, ParkingDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!IsOperator(user)) return Results.Forbid();
            var vehicle = await db.ImportedVehicles.SingleOrDefaultAsync(x => x.Id == vehicleId, ct);
            if (vehicle is null) return Results.NotFound(new { message = "Vehicle not found." });
            if (vehicle.InventoryStatus != VehicleInventoryStatus.InYard)
                return Results.Conflict(new { message = "Only vehicles currently inside the yard can be prepared for dispatch.", status = vehicle.InventoryStatus.ToString() });
            if (vehicle.CustomsStatus is null || !vehicle.CustomsStatus.Trim().Equals("Cleared", StringComparison.OrdinalIgnoreCase))
                return Results.Conflict(new { message = "Vehicle is not customs-cleared and cannot be released.", customsStatus = vehicle.CustomsStatus });

            vehicle.InventoryStatus = VehicleInventoryStatus.ReadyForDispatch;
            vehicle.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { vehicle.Id, vehicle.Vin, vehicle.InventoryStatus });
        });

        group.MapPost("/{vehicleId:guid}/complete", async (Guid vehicleId, DispatchVehicleRequest request, ParkingDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!IsOperator(user)) return Results.Forbid();
            if (string.IsNullOrWhiteSpace(request.DispatchReference)) return Results.BadRequest(new { message = "Dispatch reference is required." });
            if (string.IsNullOrWhiteSpace(request.DriverName)) return Results.BadRequest(new { message = "Driver name is required." });

            var vehicle = await db.ImportedVehicles.SingleOrDefaultAsync(x => x.Id == vehicleId, ct);
            if (vehicle is null) return Results.NotFound(new { message = "Vehicle not found." });
            if (vehicle.InventoryStatus != VehicleInventoryStatus.ReadyForDispatch)
                return Results.Conflict(new { message = "Vehicle must be marked ReadyForDispatch before final exit.", status = vehicle.InventoryStatus.ToString() });

            var duplicateReference = await db.VehicleDispatchRecords.AnyAsync(x => x.DispatchReference == request.DispatchReference.Trim(), ct);
            if (duplicateReference) return Results.Conflict(new { message = "Dispatch reference already exists." });

            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var spot = await db.ParkingSpots.SingleOrDefaultAsync(x => x.ImportedVehicleId == vehicleId, ct);
            var now = DateTimeOffset.UtcNow;

            db.VehicleDispatchRecords.Add(new VehicleDispatchRecord
            {
                ImportedVehicleId = vehicleId,
                DispatchReference = request.DispatchReference.Trim(),
                ReleaseAuthorization = request.ReleaseAuthorization?.Trim(),
                DriverName = request.DriverName.Trim(),
                DriverId = request.DriverId?.Trim(),
                Destination = request.Destination?.Trim(),
                TransportCompany = request.TransportCompany?.Trim(),
                Notes = request.Notes?.Trim(),
                OperatorUsername = user.Identity?.Name,
                DispatchedAtUtc = now
            });

            if (spot is not null)
            {
                var previous = spot.Status;
                spot.ImportedVehicleId = null;
                spot.ChangeStatus(ParkingSpotStatus.Free);
                db.ParkingEvents.Add(new ParkingEvent
                {
                    ParkingSpotId = spot.Id,
                    OldStatus = previous,
                    NewStatus = ParkingSpotStatus.Free,
                    Source = "vehicle-dispatch",
                    Actor = user.Identity?.Name,
                    TimestampUtc = now
                });
            }

            vehicle.InventoryStatus = VehicleInventoryStatus.Dispatched;
            vehicle.UpdatedAtUtc = now;
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return Results.Ok(new { vehicle.Id, vehicle.Vin, vehicle.InventoryStatus, dispatchedAtUtc = now, releasedSlot = spot?.SpotNumber, request.DispatchReference });
        });

        return endpoints;
    }

    private static bool IsOperator(ClaimsPrincipal user) => user.IsInRole("Admin") || user.IsInRole("Operator");

    public sealed record DispatchVehicleRequest(string DispatchReference, string DriverName, string? DriverId, string? ReleaseAuthorization, string? Destination, string? TransportCompany, string? Notes);
}
