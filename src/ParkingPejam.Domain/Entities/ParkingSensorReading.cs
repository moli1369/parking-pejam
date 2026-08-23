namespace ParkingPejam.Domain.Entities;

public sealed class ParkingSensorReading
{
    public long Id { get; set; }
    public Guid ParkingSensorId { get; set; }
    public ParkingSensor? ParkingSensor { get; set; }
    public bool Occupied { get; set; }
    public double? BatteryPercent { get; set; }
    public double? TemperatureC { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
