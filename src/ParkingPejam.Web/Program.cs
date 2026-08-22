using Microsoft.EntityFrameworkCore;
using ParkingPejam.Application.Contracts;
using ParkingPejam.Domain.Entities;
using ParkingPejam.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ParkingDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Parking") ?? "Data Source=parking.db"));
builder.Services.AddScoped<IParkingService, ParkingService>();
builder.Services.AddHealthChecks().AddDbContextCheck<ParkingDbContext>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ParkingDbContext>();
    await db.Database.EnsureCreatedAsync();
    await SeedAsync(db);
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapHealthChecks("/health");

var api = app.MapGroup("/api/parking");

api.MapGet("/spots", async (string? zone, IParkingService service, CancellationToken ct) =>
    Results.Ok(await service.GetSpotsAsync(zone, ct)));

api.MapGet("/spots/{id:guid}", async (Guid id, IParkingService service, CancellationToken ct) =>
{
    var spot = await service.GetSpotAsync(id, ct);
    return spot is null ? Results.NotFound() : Results.Ok(spot);
});

api.MapGet("/statistics", async (IParkingService service, CancellationToken ct) =>
    Results.Ok(await service.GetStatisticsAsync(ct)));

api.MapGet("/events", async (int? take, IParkingService service, CancellationToken ct) =>
    Results.Ok(await service.GetEventsAsync(take ?? 50, ct)));

api.MapPut("/spots/{id:guid}/status", async (
    Guid id,
    ChangeParkingStatusRequest request,
    HttpRequest httpRequest,
    IParkingService service,
    CancellationToken ct) =>
{
    if (!IsAuthorized(httpRequest, app.Environment, app.Configuration))
        return Results.StatusCode(StatusCodes.Status401Unauthorized);

    var actor = httpRequest.Headers.TryGetValue("X-Actor", out var actorValue) ? actorValue.ToString() : null;
    var updated = await service.ChangeStatusAsync(id, request.Status, request.Source ?? "web", actor, ct);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
});

app.MapFallbackToFile("index.html");
app.Run();

static bool IsAuthorized(HttpRequest request, IWebHostEnvironment env, IConfiguration config)
{
    if (env.IsDevelopment() && string.IsNullOrWhiteSpace(config["Parking:AdminKey"]))
        return true;

    var expected = config["Parking:AdminKey"];
    return !string.IsNullOrWhiteSpace(expected)
        && request.Headers.TryGetValue("X-Admin-Key", out var provided)
        && string.Equals(expected, provided.ToString(), StringComparison.Ordinal);
}

static async Task SeedAsync(ParkingDbContext db)
{
    if (await db.ParkingSpots.AnyAsync()) return;

    var spots = new List<ParkingSpot>();
    for (var zoneIndex = 0; zoneIndex < 3; zoneIndex++)
    {
        var zone = ((char)('A' + zoneIndex)).ToString();
        for (var row = 1; row <= 2; row++)
        for (var column = 1; column <= 8; column++)
        {
            spots.Add(new ParkingSpot
            {
                Id = Guid.NewGuid(),
                SpotNumber = $"{zone}-{row:D1}{column:D2}",
                Zone = zone,
                Row = row,
                Column = column,
                Status = ParkingSpotStatus.Free
            });
        }
    }

    db.ParkingSpots.AddRange(spots);
    await db.SaveChangesAsync();
}
