namespace ParkingPejam.Domain.Entities;

public sealed class ParkingSensor
{
    public Guid Id { get; set; }
    public Guid ParkingSpotId { get; set; }
    public ParkingSpot? ParkingSpot { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string DeviceKey { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool? CurrentOccupied { get; set; }
    public DateTimeOffset? LastSeenUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
