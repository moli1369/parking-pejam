namespace ParkingPejam.Domain.Entities;

public enum ShipmentStatus
{
    Planned = 0,
    Receiving = 1,
    Completed = 2,
    Closed = 3
}

public sealed class ImportShipment
{
    public Guid Id { get; set; }
    public string VesselName { get; set; } = string.Empty;
    public string VoyageNumber { get; set; } = string.Empty;
    public string PortOfEntry { get; set; } = string.Empty;
    public string ShipmentReference { get; set; } = string.Empty;
    public string BillOfLadingNumber { get; set; } = string.Empty;
    public int DeclaredVehicleCount { get; set; }
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Planned;
    public DateTimeOffset? ArrivalStartedUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
