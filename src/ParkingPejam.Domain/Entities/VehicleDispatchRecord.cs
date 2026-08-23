namespace ParkingPejam.Domain.Entities;

public sealed class VehicleDispatchRecord
{
    public long Id { get; set; }
    public Guid ImportedVehicleId { get; set; }
    public ImportedVehicle? ImportedVehicle { get; set; }
    public string DispatchReference { get; set; } = string.Empty;
    public string? ReleaseAuthorization { get; set; }
    public string? DriverName { get; set; }
    public string? DriverId { get; set; }
    public string? Destination { get; set; }
    public string? TransportCompany { get; set; }
    public string? Notes { get; set; }
    public string? OperatorUsername { get; set; }
    public DateTimeOffset DispatchedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
