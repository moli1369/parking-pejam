using ParkingPejam.Domain.Entities;

namespace ParkingPejam.Application.Contracts;

public interface IParkingService
{
    Task<IReadOnlyList<ParkingSpotDto>> GetSpotsAsync(string? zone = null, CancellationToken cancellationToken = default);
    Task<ParkingSpotDto?> GetSpotAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ParkingSpotDto?> ChangeStatusAsync(Guid id, ParkingSpotStatus status, string source, string? actor, CancellationToken cancellationToken = default);
    Task<ParkingStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ParkingEventDto>> GetEventsAsync(int take = 50, CancellationToken cancellationToken = default);
}
