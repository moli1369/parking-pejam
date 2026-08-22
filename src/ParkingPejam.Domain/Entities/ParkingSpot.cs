namespace ParkingPejam.Domain.Entities;

public enum ParkingSpotStatus
{
    Free = 0,
    Occupied = 1,
    Reserved = 2,
    OutOfService = 3
}

public sealed class ParkingSpot
{
    public Guid Id { get; set; }
    public string SpotNumber { get; set; } = string.Empty;
    public string Zone { get; set; } = "A";
    public int Row { get; set; }
    public int Column { get; set; }
    public ParkingSpotStatus Status { get; set; } = ParkingSpotStatus.Free;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public bool IsActive { get; set; } = true;

    public void ChangeStatus(ParkingSpotStatus status)
    {
        Status = status;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
