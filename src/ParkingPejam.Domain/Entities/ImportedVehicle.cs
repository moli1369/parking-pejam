namespace ParkingPejam.Domain.Entities;

public enum VehicleCondition
{
    New = 0,
    Used = 1
}

public enum VehicleInventoryStatus
{
    Expected = 0,
    Received = 1,
    InYard = 2,
    CustomsHold = 3,
    ReadyForDispatch = 4,
    Dispatched = 5
}

public sealed class ImportedVehicle
{
    public Guid Id { get; set; }
    public Guid ImportShipmentId { get; set; }
    public ImportShipment? ImportShipment { get; set; }
    public string Vin { get; set; } = string.Empty;
    public string? EngineNumber { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int? ModelYear { get; set; }
    public string? Color { get; set; }
    public VehicleCondition Condition { get; set; } = VehicleCondition.New;
    public string? OriginCountry { get; set; }
    public string? TemporaryPlate { get; set; }
    public string? CustomsStatus { get; set; }
    public string? DamageNotes { get; set; }
    public int TallySequence { get; set; }
    public VehicleInventoryStatus InventoryStatus { get; set; } = VehicleInventoryStatus.Received;
    public DateTimeOffset ReceivedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
