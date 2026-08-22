using Microsoft.EntityFrameworkCore;
using ParkingPejam.Application.Contracts;
using ParkingPejam.Domain.Entities;

namespace ParkingPejam.Infrastructure;

public sealed class ParkingService(ParkingDbContext db) : IParkingService
{
    public async Task<IReadOnlyList<ParkingSpotDto>> GetSpotsAsync(string? zone = null, CancellationToken cancellationToken = default)
    {
        var query = db.ParkingSpots.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(zone))
            query = query.Where(x => x.Zone == zone);

        return await query
            .OrderBy(x => x.Zone).ThenBy(x => x.Row).ThenBy(x => x.Column)
            .Select(x => new ParkingSpotDto(x.Id, x.SpotNumber, x.Zone, x.Row, x.Column, x.Status, x.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<ParkingSpotDto?> GetSpotAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.ParkingSpots.AsNoTracking()
            .Where(x => x.Id == id && x.IsActive)
            .Select(x => new ParkingSpotDto(x.Id, x.SpotNumber, x.Zone, x.Row, x.Column, x.Status, x.UpdatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ParkingSpotDto?> ChangeStatusAsync(Guid id, ParkingSpotStatus status, string source, string? actor, CancellationToken cancellationToken = default)
    {
        var spot = await db.ParkingSpots.SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        if (spot is null) return null;

        var oldStatus = spot.Status;
        if (oldStatus == status)
            return new ParkingSpotDto(spot.Id, spot.SpotNumber, spot.Zone, spot.Row, spot.Column, spot.Status, spot.UpdatedAtUtc);

        spot.ChangeStatus(status);
        db.ParkingEvents.Add(new ParkingEvent
        {
            ParkingSpotId = spot.Id,
            OldStatus = oldStatus,
            NewStatus = status,
            Source = string.IsNullOrWhiteSpace(source) ? "web" : source,
            Actor = actor,
            TimestampUtc = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);

        return new ParkingSpotDto(spot.Id, spot.SpotNumber, spot.Zone, spot.Row, spot.Column, spot.Status, spot.UpdatedAtUtc);
    }

    public async Task<ParkingStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var counts = await db.ParkingSpots.AsNoTracking().Where(x => x.IsActive)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Free = g.Count(x => x.Status == ParkingSpotStatus.Free),
                Occupied = g.Count(x => x.Status == ParkingSpotStatus.Occupied),
                Reserved = g.Count(x => x.Status == ParkingSpotStatus.Reserved),
                OutOfService = g.Count(x => x.Status == ParkingSpotStatus.OutOfService)
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (counts is null) return new ParkingStatisticsDto(0, 0, 0, 0, 0, 0);
        var occupancy = counts.Total == 0 ? 0 : counts.Occupied * 100d / counts.Total;
        return new ParkingStatisticsDto(counts.Total, counts.Free, counts.Occupied, counts.Reserved, counts.OutOfService, Math.Round(occupancy, 1));
    }

    public async Task<IReadOnlyList<ParkingEventDto>> GetEventsAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 200);
        return await db.ParkingEvents.AsNoTracking()
            .Include(x => x.ParkingSpot)
            .OrderByDescending(x => x.TimestampUtc)
            .Take(take)
            .Select(x => new ParkingEventDto(x.Id, x.ParkingSpot!.SpotNumber, x.OldStatus, x.NewStatus, x.Source, x.Actor, x.TimestampUtc))
            .ToListAsync(cancellationToken);
    }
}
