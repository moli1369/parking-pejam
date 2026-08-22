using ParkingPejam.Domain.Entities;

namespace ParkingPejam.Application.Contracts;

public sealed record ParkingSpotDto(
    Guid Id,
    string SpotNumber,
    string Zone,
    int Row,
    int Column,
    ParkingSpotStatus Status,
    DateTimeOffset UpdatedAtUtc);

public sealed record ParkingStatisticsDto(
    int Total,
    int Free,
    int Occupied,
    int Reserved,
    int OutOfService,
    double OccupancyPercent);

public sealed record ParkingEventDto(
    long Id,
    string SpotNumber,
    ParkingSpotStatus OldStatus,
    ParkingSpotStatus NewStatus,
    string Source,
    string? Actor,
    DateTimeOffset TimestampUtc);

public sealed record ChangeParkingStatusRequest(ParkingSpotStatus Status, string? Source = null);
