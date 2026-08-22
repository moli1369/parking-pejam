using System.Text;
using Microsoft.EntityFrameworkCore;
using ParkingPejam.Application.Contracts;
using ParkingPejam.Domain.Entities;
using ParkingPejam.Infrastructure;
using ParkingPejam.Web;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ParkingDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Parking") ?? "Data Source=parking.db"));
builder.Services.AddScoped<IParkingService, ParkingService>();
builder.Services.AddHealthChecks().AddDbContextCheck<ParkingDbContext>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHostedService<SimulationService>();

var app = builder.Build();

app.UseExceptionHandler();
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ParkingDbContext>();
    await db.Database.EnsureCreatedAsync();
    await SeedAsync(db);
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapOpenApi("/openapi/{documentName}.json");
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

api.MapGet("/export/spots.csv", async (IParkingService service, CancellationToken ct) =>
{
    var spots = await service.GetSpotsAsync(cancellationToken: ct);
    var csv = new StringBuilder();
    csv.AppendLine("SpotNumber,Zone,Row,Column,Status,UpdatedAtUtc");
    foreach (var spot in spots)
        csv.AppendLine(Csv(spot.SpotNumber) + "," + Csv(spot.Zone) + "," + spot.Row + "," + spot.Column + "," + Csv(spot.Status.ToString()) + "," + Csv(spot.UpdatedAtUtc.ToString("O")));

    return Results.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"parking-spots-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
});

api.MapGet("/export/events.csv", async (int? take, IParkingService service, CancellationToken ct) =>
{
    var events = await service.GetEventsAsync(take ?? 200, ct);
    var csv = new StringBuilder();
    csv.AppendLine("SpotNumber,OldStatus,NewStatus,Source,Actor,TimestampUtc");
    foreach (var item in events)
        csv.AppendLine(string.Join(",", Csv(item.SpotNumber), Csv(item.OldStatus.ToString()), Csv(item.NewStatus.ToString()), Csv(item.Source), Csv(item.Actor), Csv(item.TimestampUtc.ToString("O"))));

    return Results.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"parking-events-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
});

api.MapGet("/export/report.json", async (IParkingService service, CancellationToken ct) =>
{
    var stats = await service.GetStatisticsAsync(ct);
    var spots = await service.GetSpotsAsync(cancellationToken: ct);
    var events = await service.GetEventsAsync(200, ct);
    return Results.Ok(new { generatedAtUtc = DateTimeOffset.UtcNow, statistics = stats, spots, events });
});

app.MapFallbackToFile("index.html");
app.Run();

static string Csv(string? value)
{
    value ??= string.Empty;
    return "\"" + value.Replace("\"", "\"\"") + "\"";
}

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
