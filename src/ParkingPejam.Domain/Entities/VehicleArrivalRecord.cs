namespace ParkingPejam.Domain.Entities;

public sealed class VehicleArrivalRecord
{
    public Guid Id { get; set; }
    public Guid ImportedVehicleId { get; set; }
    public ImportedVehicle? ImportedVehicle { get; set; }
    public Guid ImportShipmentId { get; set; }
    public int TallySequence { get; set; }
    public string Source { get; set; } = "tally";
    public string? OperatorUsername { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? Notes { get; set; }
}
