namespace ParkingPejam.Domain.Entities;

public sealed class ParkingEvent
{
    public long Id { get; set; }
    public Guid ParkingSpotId { get; set; }
    public ParkingSpot? ParkingSpot { get; set; }
    public ParkingSpotStatus OldStatus { get; set; }
    public ParkingSpotStatus NewStatus { get; set; }
    public string Source { get; set; } = "web";
    public string? Actor { get; set; }
    public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
}
