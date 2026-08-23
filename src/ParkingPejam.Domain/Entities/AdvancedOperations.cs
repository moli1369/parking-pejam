namespace ParkingPejam.Domain.Entities;

public enum ManifestMatchStatus { Expected = 0, Matched = 1, Unexpected = 2, Duplicate = 3, Missing = 4 }
public enum InspectionStatus { Pending = 0, Passed = 1, Failed = 2 }
public enum HoldType { Customs = 0, Inspection = 1, Document = 2, Customer = 3, Damage = 4, Operational = 5 }
public enum HoldStatus { Active = 0, Released = 1 }
public enum YardNodeType { Yard = 0, Zone = 1, Block = 2, Row = 3, Bay = 4, Slot = 5 }
public enum GateVisitType { GateIn = 0, GateOut = 1 }
public enum GateVisitStatus { Open = 0, Completed = 1, Rejected = 2 }
public enum DispatchPlanStatus { Draft = 0, Loading = 1, Completed = 2, Cancelled = 3 }
public enum DocumentType { BillOfLading = 0, Customs = 1, Invoice = 2, ImportPermit = 3, Inspection = 4, Release = 5, ProofOfDelivery = 6, Other = 7 }
public enum DocumentStatus { Uploaded = 0, Verified = 1, Rejected = 2 }
public enum BillingActivityType { Storage = 0, Inspection = 1, Wash = 2, Repair = 3, Transfer = 4, Loading = 5, Other = 6 }

public sealed class ImportManifestEntry
{
    public Guid Id { get; set; }
    public Guid ImportShipmentId { get; set; }
    public string Vin { get; set; } = string.Empty;
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? ModelYear { get; set; }
    public string? EngineNumber { get; set; }
    public string? Color { get; set; }
    public string? Destination { get; set; }
    public ManifestMatchStatus MatchStatus { get; set; } = ManifestMatchStatus.Expected;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class VehicleInspection
{
    public Guid Id { get; set; }
    public Guid ImportedVehicleId { get; set; }
    public InspectionStatus Status { get; set; } = InspectionStatus.Pending;
    public string? InspectorUsername { get; set; }
    public string? DamageCode { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset InspectedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class VehicleInspectionPhoto
{
    public long Id { get; set; }
    public Guid VehicleInspectionId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class VehicleHold
{
    public long Id { get; set; }
    public Guid ImportedVehicleId { get; set; }
    public HoldType Type { get; set; }
    public HoldStatus Status { get; set; } = HoldStatus.Active;
    public string Reason { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
    public string? ReleasedBy { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReleasedAtUtc { get; set; }
}

public sealed class YardNode
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public YardNode? Parent { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public YardNodeType NodeType { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class YardQrCode
{
    public long Id { get; set; }
    public Guid YardNodeId { get; set; }
    public string Token { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class Driver
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? DriverNumber { get; set; }
    public string? Phone { get; set; }
    public string? TransportCompany { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class TransportTruck
{
    public Guid Id { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string? TruckType { get; set; }
    public string? TransportCompany { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class GateVisit
{
    public long Id { get; set; }
    public Guid ImportedVehicleId { get; set; }
    public GateVisitType Type { get; set; }
    public GateVisitStatus Status { get; set; } = GateVisitStatus.Open;
    public string? GateCode { get; set; }
    public string? VehiclePlate { get; set; }
    public string? DriverName { get; set; }
    public string? DriverId { get; set; }
    public string? TruckPlate { get; set; }
    public string? OperatorUsername { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? Notes { get; set; }
}

public sealed class DispatchLoadPlan
{
    public Guid Id { get; set; }
    public string LoadReference { get; set; } = string.Empty;
    public string? Destination { get; set; }
    public string? TruckPlate { get; set; }
    public Guid? DriverId { get; set; }
    public DispatchPlanStatus Status { get; set; } = DispatchPlanStatus.Draft;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class DispatchLoadItem
{
    public long Id { get; set; }
    public Guid DispatchLoadPlanId { get; set; }
    public Guid ImportedVehicleId { get; set; }
    public int LoadSequence { get; set; }
    public DateTimeOffset? LoadedAtUtc { get; set; }
}

public sealed class VehicleDocument
{
    public long Id { get; set; }
    public Guid ImportedVehicleId { get; set; }
    public DocumentType Type { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;
    public string FileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string? UploadedBy { get; set; }
    public DateTimeOffset UploadedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? VerifiedAtUtc { get; set; }
}

public sealed class CustomerAccount
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ExternalReference { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class VehicleCustomerLink
{
    public long Id { get; set; }
    public Guid ImportedVehicleId { get; set; }
    public Guid CustomerAccountId { get; set; }
    public bool Primary { get; set; } = true;
}

public sealed class KeyAssignment
{
    public long Id { get; set; }
    public Guid ImportedVehicleId { get; set; }
    public string KeyNumber { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
    public DateTimeOffset AssignedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReturnedAtUtc { get; set; }
}

public sealed class BillingActivity
{
    public long Id { get; set; }
    public Guid ImportedVehicleId { get; set; }
    public BillingActivityType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTimeOffset ActivityAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? Notes { get; set; }
}

public sealed class VehicleLprDetection
{
    public long Id { get; set; }
    public Guid? ImportedVehicleId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string? CameraId { get; set; }
    public DateTimeOffset DetectedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
