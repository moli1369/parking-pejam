using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ParkingPejam.Domain.Entities;
using ParkingPejam.Infrastructure;

namespace ParkingPejam.Web;

public static class AdvancedOperationsEndpoints
{
    public static IEndpointRouteBuilder MapAdvancedOperations(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/ops").RequireAuthorization();
        static bool CanOperate(ClaimsPrincipal u) => u.IsInRole("Admin") || u.IsInRole("Operator");

        api.MapGet("/manifest/{shipmentId:guid}", async (Guid shipmentId, ParkingDbContext db, CancellationToken ct) =>
            Results.Ok(await db.ImportManifestEntries.AsNoTracking().Where(x => x.ImportShipmentId == shipmentId).OrderBy(x => x.Vin).ToListAsync(ct)));

        api.MapPost("/manifest/{shipmentId:guid}", async (Guid shipmentId, ManifestRequest req, ParkingDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!CanOperate(user)) return Results.Forbid();
            if (!await db.ImportShipments.AnyAsync(x => x.Id == shipmentId, ct)) return Results.NotFound();
            var vin = req.Vin.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(vin)) return Results.BadRequest(new { message = "VIN is required." });
            if (await db.ImportManifestEntries.AnyAsync(x => x.ImportShipmentId == shipmentId && x.Vin == vin, ct)) return Results.Conflict(new { message = "VIN already exists in manifest." });
            var row = new ImportManifestEntry { Id = Guid.NewGuid(), ImportShipmentId = shipmentId, Vin = vin, Make = req.Make, Model = req.Model, ModelYear = req.ModelYear, EngineNumber = req.EngineNumber, Color = req.Color, Destination = req.Destination };
            db.ImportManifestEntries.Add(row); await db.SaveChangesAsync(ct); return Results.Created($"/api/ops/manifest/{shipmentId}", row);
        });

        api.MapGet("/manifest/{shipmentId:guid}/reconcile", async (Guid shipmentId, ParkingDbContext db, CancellationToken ct) =>
        {
            var entries = await db.ImportManifestEntries.AsNoTracking().Where(x => x.ImportShipmentId == shipmentId).ToListAsync(ct);
            var vehicles = await db.ImportedVehicles.AsNoTracking().Where(x => x.ImportShipmentId == shipmentId).ToListAsync(ct);
            var manifestVins = entries.Select(x => x.Vin).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var actualVins = vehicles.Select(x => x.Vin).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return Results.Ok(new { expected = entries.Count, received = vehicles.Count, matched = manifestVins.Intersect(actualVins).Count(), unexpected = actualVins.Except(manifestVins).Count(), missing = manifestVins.Except(actualVins).Count(), duplicates = entries.GroupBy(x => x.Vin).Count(g => g.Count() > 1) });
        });

        api.MapPost("/vehicles/{vehicleId:guid}/inspection", async (Guid vehicleId, InspectionRequest req, ParkingDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!CanOperate(user)) return Results.Forbid();
            if (!await db.ImportedVehicles.AnyAsync(x => x.Id == vehicleId, ct)) return Results.NotFound();
            var inspection = new VehicleInspection { Id = Guid.NewGuid(), ImportedVehicleId = vehicleId, Status = req.Passed ? InspectionStatus.Passed : InspectionStatus.Failed, InspectorUsername = user.Identity?.Name, DamageCode = req.DamageCode, Notes = req.Notes };
            db.VehicleInspections.Add(inspection);
            if (!req.Passed) db.VehicleHolds.Add(new VehicleHold { ImportedVehicleId = vehicleId, Type = HoldType.Inspection, Reason = req.Notes ?? "Inspection failed", CreatedBy = user.Identity?.Name });
            await db.SaveChangesAsync(ct); return Results.Ok(inspection);
        });

        api.MapPost("/vehicles/{vehicleId:guid}/holds", async (Guid vehicleId, HoldRequest req, ParkingDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!CanOperate(user)) return Results.Forbid();
            if (!await db.ImportedVehicles.AnyAsync(x => x.Id == vehicleId, ct)) return Results.NotFound();
            var hold = new VehicleHold { ImportedVehicleId = vehicleId, Type = req.Type, Reason = req.Reason, CreatedBy = user.Identity?.Name };
            db.VehicleHolds.Add(hold); await db.SaveChangesAsync(ct); return Results.Created($"/api/ops/vehicles/{vehicleId}/holds", hold);
        });

        api.MapPost("/holds/{id:long}/release", async (long id, ParkingDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!CanOperate(user)) return Results.Forbid();
            var hold = await db.VehicleHolds.SingleOrDefaultAsync(x => x.Id == id, ct); if (hold is null) return Results.NotFound();
            hold.Status = HoldStatus.Released; hold.ReleasedBy = user.Identity?.Name; hold.ReleasedAtUtc = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct); return Results.Ok(hold);
        });

        api.MapGet("/vehicles/{vehicleId:guid}/holds", async (Guid vehicleId, ParkingDbContext db, CancellationToken ct) => Results.Ok(await db.VehicleHolds.AsNoTracking().Where(x => x.ImportedVehicleId == vehicleId).OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct)));

        api.MapGet("/yard/nodes", async (ParkingDbContext db, CancellationToken ct) => Results.Ok(await db.YardNodes.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct)));
        api.MapPost("/yard/nodes", async (YardNodeRequest req, ParkingDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!CanOperate(user)) return Results.Forbid(); if (await db.YardNodes.AnyAsync(x => x.Code == req.Code, ct)) return Results.Conflict(new { message = "Yard code exists." });
            var node = new YardNode { Id = Guid.NewGuid(), ParentId = req.ParentId, Code = req.Code.Trim(), Name = req.Name.Trim(), NodeType = req.NodeType }; db.YardNodes.Add(node); await db.SaveChangesAsync(ct); return Results.Created($"/api/ops/yard/nodes/{node.Id}", node);
        });
        api.MapPost("/yard/nodes/{id:guid}/qr", async (Guid id, ParkingDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!CanOperate(user)) return Results.Forbid(); if (!await db.YardNodes.AnyAsync(x => x.Id == id, ct)) return Results.NotFound();
            var qr = new YardQrCode { YardNodeId = id, Token = $"YARD-{Guid.NewGuid():N}" }; db.YardQrCodes.Add(qr); await db.SaveChangesAsync(ct); return Results.Ok(qr);
        });

        api.MapPost("/gate/visits", async (GateVisitRequest req, ParkingDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!CanOperate(user)) return Results.Forbid(); if (!await db.ImportedVehicles.AnyAsync(x => x.Id == req.VehicleId, ct)) return Results.NotFound();
            var visit = new GateVisit { ImportedVehicleId = req.VehicleId, Type = req.Type, GateCode = req.GateCode, VehiclePlate = req.VehiclePlate, DriverName = req.DriverName, DriverId = req.DriverId, TruckPlate = req.TruckPlate, OperatorUsername = user.Identity?.Name, Notes = req.Notes };
            db.GateVisits.Add(visit); await db.SaveChangesAsync(ct); return Results.Ok(visit);
        });
        api.MapPost("/gate/visits/{id:long}/complete", async (long id, ParkingDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!CanOperate(user)) return Results.Forbid(); var visit = await db.GateVisits.SingleOrDefaultAsync(x => x.Id == id, ct); if (visit is null) return Results.NotFound(); visit.Status = GateVisitStatus.Completed; visit.CompletedAtUtc = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct); return Results.Ok(visit);
        });

        api.MapPost("/dispatch/plans", async (LoadPlanRequest req, ParkingDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!CanOperate(user)) return Results.Forbid(); var plan = new DispatchLoadPlan { Id = Guid.NewGuid(), LoadReference = req.LoadReference.Trim(), Destination = req.Destination, TruckPlate = req.TruckPlate, DriverId = req.DriverId }; db.DispatchLoadPlans.Add(plan); await db.SaveChangesAsync(ct); return Results.Created($"/api/ops/dispatch/plans/{plan.Id}", plan);
        });
        api.MapPost("/dispatch/plans/{planId:guid}/items", async (Guid planId, LoadItemRequest req, ParkingDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!CanOperate(user)) return Results.Forbid(); if (!await db.DispatchLoadPlans.AnyAsync(x => x.Id == planId, ct)) return Results.NotFound(); if (await db.DispatchLoadItems.AnyAsync(x => x.DispatchLoadPlanId == planId && x.ImportedVehicleId == req.VehicleId, ct)) return Results.Conflict();
            var item = new DispatchLoadItem { DispatchLoadPlanId = planId, ImportedVehicleId = req.VehicleId, LoadSequence = req.LoadSequence }; db.DispatchLoadItems.Add(item); await db.SaveChangesAsync(ct); return Results.Ok(item);
        });
        api.MapPost("/dispatch/plans/{planId:guid}/complete", async (Guid planId, ParkingDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!CanOperate(user)) return Results.Forbid(); var plan = await db.DispatchLoadPlans.SingleOrDefaultAsync(x => x.Id == planId, ct); if (plan is null) return Results.NotFound(); plan.Status = DispatchPlanStatus.Completed; await db.SaveChangesAsync(ct); return Results.Ok(plan);
        });

        api.MapPost("/vehicles/{vehicleId:guid}/documents", async (Guid vehicleId, DocumentRequest req, ParkingDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!CanOperate(user)) return Results.Forbid(); var doc = new VehicleDocument { ImportedVehicleId = vehicleId, Type = req.Type, FileName = req.FileName, StorageKey = req.StorageKey, UploadedBy = user.Identity?.Name }; db.VehicleDocuments.Add(doc); await db.SaveChangesAsync(ct); return Results.Created($"/api/ops/vehicles/{vehicleId}/documents", doc);
        });
        api.MapGet("/vehicles/{vehicleId:guid}/documents", async (Guid vehicleId, ParkingDbContext db, CancellationToken ct) => Results.Ok(await db.VehicleDocuments.AsNoTracking().Where(x => x.ImportedVehicleId == vehicleId).OrderByDescending(x => x.UploadedAtUtc).ToListAsync(ct)));

        api.MapGet("/customers", async (ParkingDbContext db, CancellationToken ct) => Results.Ok(await db.CustomerAccounts.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(ct)));
        api.MapPost("/customers", async (CustomerRequest req, ParkingDbContext db, ClaimsPrincipal user, CancellationToken ct) => { if (!CanOperate(user)) return Results.Forbid(); var c = new CustomerAccount { Id = Guid.NewGuid(), Name = req.Name.Trim(), ExternalReference = req.ExternalReference }; db.CustomerAccounts.Add(c); await db.SaveChangesAsync(ct); return Results.Created($"/api/ops/customers/{c.Id}", c); });
        api.MapPost("/vehicles/{vehicleId:guid}/customer", async (Guid vehicleId, CustomerLinkRequest req, ParkingDbContext db, ClaimsPrincipal user, CancellationToken ct) => { if (!CanOperate(user)) return Results.Forbid(); var link = new VehicleCustomerLink { ImportedVehicleId = vehicleId, CustomerAccountId = req.CustomerId, Primary = true }; db.VehicleCustomerLinks.Add(link); await db.SaveChangesAsync(ct); return Results.Ok(link); });

        api.MapPost("/vehicles/{vehicleId:guid}/keys/assign", async (Guid vehicleId, KeyRequest req, ParkingDbContext db, ClaimsPrincipal user, CancellationToken ct) => { if (!CanOperate(user)) return Results.Forbid(); var k = new KeyAssignment { ImportedVehicleId = vehicleId, KeyNumber = req.KeyNumber.Trim(), AssignedTo = req.AssignedTo, AssignedAtUtc = DateTimeOffset.UtcNow }; db.KeyAssignments.Add(k); await db.SaveChangesAsync(ct); return Results.Ok(k); });
        api.MapPost("/keys/{id:long}/return", async (long id, ParkingDbContext db, ClaimsPrincipal user, CancellationToken ct) => { if (!CanOperate(user)) return Results.Forbid(); var k = await db.KeyAssignments.SingleOrDefaultAsync(x => x.Id == id, ct); if (k is null) return Results.NotFound(); k.ReturnedAtUtc = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct); return Results.Ok(k); });

        api.MapPost("/vehicles/{vehicleId:guid}/billing", async (Guid vehicleId, BillingRequest req, ParkingDbContext db, ClaimsPrincipal user, CancellationToken ct) => { if (!CanOperate(user)) return Results.Forbid(); var row = new BillingActivity { ImportedVehicleId = vehicleId, Type = req.Type, Quantity = req.Quantity, UnitPrice = req.UnitPrice, Currency = req.Currency ?? "EUR", Notes = req.Notes }; db.BillingActivities.Add(row); await db.SaveChangesAsync(ct); return Results.Ok(new { row.Id, row.Type, row.Quantity, row.UnitPrice, row.Currency, Total = row.Quantity * row.UnitPrice }); });
        api.MapGet("/vehicles/{vehicleId:guid}/billing", async (Guid vehicleId, ParkingDbContext db, CancellationToken ct) => { var rows = await db.BillingActivities.AsNoTracking().Where(x => x.ImportedVehicleId == vehicleId).ToListAsync(ct); return Results.Ok(new { rows, total = rows.Sum(x => x.Quantity * x.UnitPrice), currency = rows.FirstOrDefault()?.Currency ?? "EUR" }); });

        api.MapPost("/lpr/detections", async (LprRequest req, ParkingDbContext db, CancellationToken ct) => { var row = new VehicleLprDetection { ImportedVehicleId = req.VehicleId, PlateNumber = req.PlateNumber.Trim(), Confidence = req.Confidence, CameraId = req.CameraId }; db.VehicleLprDetections.Add(row); await db.SaveChangesAsync(ct); return Results.Ok(row); });
        api.MapGet("/analytics/aging", async (ParkingDbContext db, CancellationToken ct) =>
        {
            var now = DateTimeOffset.UtcNow; var rows = await db.ImportedVehicles.AsNoTracking().Where(v => v.InventoryStatus != VehicleInventoryStatus.Dispatched).Select(v => new { v.Id, v.Vin, v.Make, v.Model, v.ReceivedAtUtc, v.InventoryStatus }).ToListAsync(ct);
            return Results.Ok(rows.Select(v => new { v.Id, v.Vin, v.Make, v.Model, v.InventoryStatus, v.ReceivedAtUtc, DaysInYard = Math.Max(0, (now - v.ReceivedAtUtc).TotalDays), Bucket = (now - v.ReceivedAtUtc).TotalDays switch { < 8 => "0-7", < 15 => "8-14", < 31 => "15-30", _ => "30+" } }));
        });
        api.MapGet("/vehicles/{vehicleId:guid}/slot-suggestion", async (Guid vehicleId, ParkingDbContext db, CancellationToken ct) =>
        {
            var v = await db.ImportedVehicles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == vehicleId, ct); if (v is null) return Results.NotFound();
            var occupied = await db.ParkingSpots.AsNoTracking().Where(x => x.Status != ParkingSpotStatus.Free || x.ImportedVehicleId != null).Select(x => x.Id).ToListAsync(ct);
            var slot = await db.ParkingSpots.AsNoTracking().Where(x => x.IsActive && x.Status == ParkingSpotStatus.Free && x.ImportedVehicleId == null).OrderBy(x => x.Zone).ThenBy(x => x.Row).ThenBy(x => x.Column).FirstOrDefaultAsync(ct);
            return slot is null ? Results.Ok(new { found = false }) : Results.Ok(new { found = true, reason = "First available slot by deterministic yard ordering", slot = new { slot.Id, slot.SpotNumber, slot.Zone, slot.Row, slot.Column } });
        });

        return endpoints;
    }

    public sealed record ManifestRequest(string Vin, string? Make, string? Model, int? ModelYear, string? EngineNumber, string? Color, string? Destination);
    public sealed record InspectionRequest(bool Passed, string? DamageCode, string? Notes);
    public sealed record HoldRequest(HoldType Type, string Reason);
    public sealed record YardNodeRequest(Guid? ParentId, string Code, string Name, YardNodeType NodeType);
    public sealed record GateVisitRequest(Guid VehicleId, GateVisitType Type, string? GateCode, string? VehiclePlate, string? DriverName, string? DriverId, string? TruckPlate, string? Notes);
    public sealed record LoadPlanRequest(string LoadReference, string? Destination, string? TruckPlate, Guid? DriverId);
    public sealed record LoadItemRequest(Guid VehicleId, int LoadSequence);
    public sealed record DocumentRequest(DocumentType Type, string FileName, string StorageKey);
    public sealed record CustomerRequest(string Name, string? ExternalReference);
    public sealed record CustomerLinkRequest(Guid CustomerId);
    public sealed record KeyRequest(string KeyNumber, string? AssignedTo);
    public sealed record BillingRequest(BillingActivityType Type, decimal Quantity, decimal UnitPrice, string? Currency, string? Notes);
    public sealed record LprRequest(Guid? VehicleId, string PlateNumber, double Confidence, string? CameraId);
}
