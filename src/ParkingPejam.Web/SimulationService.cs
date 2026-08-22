using ParkingPejam.Application.Contracts;
using ParkingPejam.Domain.Entities;

namespace ParkingPejam.Web;

public sealed class SimulationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SimulationService> _logger;

    public SimulationService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<SimulationService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var enabled = _configuration.GetValue<bool>("Parking:SimulationEnabled");
            if (enabled)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IParkingService>();
                    var spots = await service.GetSpotsAsync(cancellationToken: stoppingToken);
                    var candidate = spots.FirstOrDefault(x => x.Status is ParkingSpotStatus.Free or ParkingSpotStatus.Occupied);
                    if (candidate is not null)
                    {
                        var next = candidate.Status == ParkingSpotStatus.Free ? ParkingSpotStatus.Occupied : ParkingSpotStatus.Free;
                        await service.ChangeStatusAsync(candidate.Id, next, "simulation", "Simulation Engine", stoppingToken);
                        _logger.LogInformation("Simulation changed {Spot} to {Status}", candidate.SpotNumber, next);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Parking simulation tick failed");
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(4), stoppingToken);
        }
    }
}
