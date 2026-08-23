using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ParkingPejam.Domain.Entities;
using ParkingPejam.Infrastructure;

namespace ParkingPejam.Web;

public static class ImportWorkflowEndpoints
{
    public static IEndpointRouteBuilder MapImportWorkflow(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/import").RequireAuthorization();

        group.MapPost("/shipments", async (CreateShipmentRequest request, ParkingDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!IsOperator(user)) return Results.Forbid();
            if (request.DeclaredVehicleCount < 0) return Results.BadRequest(new { message = "Declared vehicle count cannot be negative." });
            if (string.IsNullOrWhiteSpace(request.VesselName) || string.IsNullOrWhiteSpace(request.VoyageNumber) || string.IsNullOrWhiteSpace(request.ShipmentReference))
                return Results.BadRequest(new { message = "Vessel, voyage and shipment reference are required." });

            var exists = await db.ImportShipments.AnyAsync(x => x.ShipmentReference == request.ShipmentReference.Trim(), ct);
            if (exists) return Results.Conflict(new { message = "A shipment with this reference already exists." });

            var now = DateTimeOffset.UtcNow;
            var shipment = new ImportShipment
            {
                Id = Guid.NewGuid(),
                VesselName = request.VesselName.Trim(),
                VoyageNumber = request.VoyageNumber.Trim(),
                PortOfEntry = request.PortOfEntry?.Trim() ?? string.Empty,
                ShipmentReference = request.ShipmentReference.Trim(),
                BillOfLadingNumber = request.BillOfLadingNumber?.Trim() ?? string.Empty,
                DeclaredVehicleCount = request.DeclaredVehicleCount,
                Status = ShipmentStatus.Planned,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            db.ImportShipments.Add(shipment);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/import/shipments/{shipment.Id}", await ShipmentDtoAsync(db, shipment.Id, ct));
        });

        group.MapGet("/shipments", async (ParkingDbContext db, CancellationToken ct) =>
            Results.Ok(await db.ImportShipments.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Select(x => new
            {
                x.Id, x.VesselName, x.VoyageNumber, x.PortOfEntry, x.ShipmentReference, x.BillOfLadingNumber,
                x.DeclaredVehicleCount, x.Status, x.CreatedAtUtc, x.UpdatedAtUtc,
                ReceivedVehicleCount = x.Id == Guid.Empty ? 0 : db.ImportedVehicles.Count(v => v.ImportShipmentId == x.Id),
                RemainingVehicleCount = x.DeclaredVehicleCount - db.ImportedVehicles.Count(v => v.ImportShipmentId == x.Id)
            }).ToListAsync(ct)));

        group.MapGet("/shipments/{id:guid}", async (Guid id, ParkingDbContext db, CancellationToken ct) =>
        {
            var dto = await ShipmentDtoAsync(db, id, ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        group.MapPost("/shipments/{shipmentId:guid}/vehicles", async (Guid shipmentId, RegisterVehicleRequest request, ParkingDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!IsOperator(user)) return Results.Forbid();
            var shipment = await db.ImportShipments.SingleOrDefaultAsync(x => x.Id == shipmentId, ct);
            if (shipment is null) return Results.NotFound(new { message = "Shipment not found." });

            var vin = request.Vin.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(vin) || vin.Length < 11) return Results.BadRequest(new { message = "A valid VIN is required." });
            if (await db.ImportedVehicles.AnyAsync(x => x.Vin == vin, ct)) return Results.Conflict(new { message = "This VIN is already registered." });

            var nextSequence = (await db.ImportedVehicles.Where(x => x.ImportShipmentId == shipmentId).MaxAsync(x => (int?)x.TallySequence, ct) ?? 0) + 1;
            if (shipment.DeclaredVehicleCount > 0 && nextSequence > shipment.DeclaredVehicleCount)
                return Results.Conflict(new { message = "The declared shipment vehicle count has already been reached." });

            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var now = DateTimeOffset.UtcNow;
            var vehicle = new ImportedVehicle
            {
                Id = Guid.NewGuid(),
                ImportShipmentId = shipmentId,
                Vin = vin,
                EngineNumber = request.EngineNumber?.Trim(),
                Make = request.Make.Trim(),
                Model = request.Model.Trim(),
                ModelYear = request.ModelYear,
                Color = request.Color?.Trim(),
                Condition = request.Condition,
                OriginCountry = request.OriginCountry?.Trim(),
                TemporaryPlate = request.TemporaryPlate?.Trim(),
                CustomsStatus = request.CustomsStatus?.Trim(),
                DamageNotes = request.DamageNotes?.Trim(),
                TallySequence = nextSequence,
                InventoryStatus = VehicleInventoryStatus.Received,
                ReceivedAtUtc = now,
                UpdatedAtUtc = now
            };
            db.ImportedVehicles.Add(vehicle);
            db.VehicleArrivalRecords.Add(new VehicleArrivalRecord
            {
                Id = Guid.NewGuid(),
                ImportedVehicleId = vehicle.Id,
                ImportShipmentId = shipmentId,
                TallySequence = nextSequence,
                OperatorUsername = user.Identity?.Name,
                ReceivedAtUtc = now,
                Notes = request.Notes?.Trim()
            });
            shipment.Status = ShipmentStatus.Receiving;
            shipment.ArrivalStartedUtc ??= now;
            shipment.UpdatedAtUtc = now;
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return Results.Created($"/api/import/vehicles/{vehicle.Id}", new { vehicle.Id, vehicle.Vin, vehicle.TallySequence, vehicle.InventoryStatus, shipmentId });
        });

        group.MapGet("/vehicles/{id:guid}", async (Guid id, ParkingDbContext db, CancellationToken ct) =>
        {
            var vehicle = await db.ImportedVehicles.AsNoTracking().Include(x => x.ImportShipment).SingleOrDefaultAsync(x => x.Id == id, ct);
            if (vehicle is null) return Results.NotFound();
            var slot = await db.ParkingSpots.AsNoTracking().Where(x => x.ImportedVehicleId == id).Select(x => new { x.Id, x.SpotNumber, x.Zone, x.Status }).SingleOrDefaultAsync(ct);
            return Results.Ok(new
            {
                vehicle.Id, vehicle.Vin, vehicle.EngineNumber, vehicle.Make, vehicle.Model, vehicle.ModelYear, vehicle.Color,
                vehicle.Condition, vehicle.OriginCountry, vehicle.TemporaryPlate, vehicle.CustomsStatus, vehicle.DamageNotes,
                vehicle.TallySequence, vehicle.InventoryStatus, vehicle.ReceivedAtUtc, vehicle.UpdatedAtUtc,
                Shipment = vehicle.ImportShipment is null ? null : new { vehicle.ImportShipment.Id, vehicle.ImportShipment.VesselName, vehicle.ImportShipment.VoyageNumber, vehicle.ImportShipment.ShipmentReference, vehicle.ImportShipment.BillOfLadingNumber },
                Slot = slot
            });
        });

        group.MapPost("/vehicles/{vehicleId:guid}/assign-slot", async (Guid vehicleId, AssignVehicleSlotRequest request, ParkingDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!IsOperator(user)) return Results.Forbid();
            var vehicle = await db.ImportedVehicles.SingleOrDefaultAsync(x => x.Id == vehicleId, ct);
            if (vehicle is null) return Results.NotFound(new { message = "Vehicle not found." });
            var spot = await db.ParkingSpots.SingleOrDefaultAsync(x => x.SpotNumber == request.SpotNumber && x.IsActive, ct);
            if (spot is null) return Results.NotFound(new { message = "Yard slot not found." });
            if (spot.Status != ParkingSpotStatus.Free || spot.ImportedVehicleId is not null)
                return Results.Conflict(new { message = "The selected yard slot is not available." });

            var previousSpot = await db.ParkingSpots.SingleOrDefaultAsync(x => x.ImportedVehicleId == vehicleId, ct);
            if (previousSpot is not null)
            {
                previousSpot.ImportedVehicleId = null;
                previousSpot.ChangeStatus(ParkingSpotStatus.Free);
            }

            var oldStatus = spot.Status;
            spot.ImportedVehicleId = vehicleId;
            spot.ChangeStatus(ParkingSpotStatus.Occupied);
            vehicle.InventoryStatus = VehicleInventoryStatus.InYard;
            vehicle.UpdatedAtUtc = DateTimeOffset.UtcNow;
            db.ParkingEvents.Add(new ParkingEvent
            {
                ParkingSpotId = spot.Id,
                OldStatus = oldStatus,
                NewStatus = ParkingSpotStatus.Occupied,
                Source = "yard-assignment",
                Actor = user.Identity?.Name,
                TimestampUtc = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { vehicleId, vehicle.Vin, spot = new { spot.SpotNumber, spot.Zone, spot.Status }, vehicle.InventoryStatus });
        });

        group.MapPost("/shipments/{shipmentId:guid}/complete", async (Guid shipmentId, ParkingDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!IsOperator(user)) return Results.Forbid();
            var shipment = await db.ImportShipments.SingleOrDefaultAsync(x => x.Id == shipmentId, ct);
            if (shipment is null) return Results.NotFound();
            var received = await db.ImportedVehicles.CountAsync(x => x.ImportShipmentId == shipmentId, ct);
            if (shipment.DeclaredVehicleCount > 0 && received != shipment.DeclaredVehicleCount)
                return Results.Conflict(new { message = "Shipment cannot be closed while received count does not match the declared count.", declared = shipment.DeclaredVehicleCount, received, remaining = shipment.DeclaredVehicleCount - received });
            shipment.Status = ShipmentStatus.Completed;
            shipment.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(await ShipmentDtoAsync(db, shipmentId, ct));
        });

        return endpoints;
    }

    private static bool IsOperator(ClaimsPrincipal user) => user.IsInRole("Admin") || user.IsInRole("Operator");

    private static async Task<object?> ShipmentDtoAsync(ParkingDbContext db, Guid id, CancellationToken ct)
    {
        var shipment = await db.ImportShipments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (shipment is null) return null;
        var received = await db.ImportedVehicles.CountAsync(x => x.ImportShipmentId == id, ct);
        return new
        {
            shipment.Id, shipment.VesselName, shipment.VoyageNumber, shipment.PortOfEntry, shipment.ShipmentReference,
            shipment.BillOfLadingNumber, shipment.DeclaredVehicleCount, ReceivedVehicleCount = received,
            RemainingVehicleCount = Math.Max(0, shipment.DeclaredVehicleCount - received),
            shipment.Status, shipment.ArrivalStartedUtc, shipment.CreatedAtUtc, shipment.UpdatedAtUtc
        };
    }

    public sealed record CreateShipmentRequest(string VesselName, string VoyageNumber, string? PortOfEntry, string ShipmentReference, string? BillOfLadingNumber, int DeclaredVehicleCount);
    public sealed record RegisterVehicleRequest(string Vin, string Make, string Model, int? ModelYear, string? Color, VehicleCondition Condition, string? OriginCountry, string? EngineNumber, string? TemporaryPlate, string? CustomsStatus, string? DamageNotes, string? Notes);
    public sealed record AssignVehicleSlotRequest(string SpotNumber);
}
