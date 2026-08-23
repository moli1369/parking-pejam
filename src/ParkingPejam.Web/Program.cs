using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using ParkingPejam.Application.Contracts;
using ParkingPejam.Domain.Entities;
using ParkingPejam.Infrastructure;
using ParkingPejam.Infrastructure.Licensing;

var builder = WebApplication.CreateBuilder(args);
var provider = builder.Configuration["Database:Provider"]?.Trim().ToLowerInvariant() ?? "sqlite";
var connectionString = builder.Configuration.GetConnectionString("Parking") ??
    (provider is "postgres" or "postgresql"
        ? "Host=localhost;Port=5432;Database=parking_pejam;Username=parking;Password=change-me"
        : "Data Source=parking.db");

var dataDir = builder.Configuration["Storage:DataPath"] ?? Path.Combine(builder.Environment.ContentRootPath, "Data");
Directory.CreateDirectory(dataDir);
Directory.CreateDirectory(Path.Combine(dataDir, "keys"));

builder.Services.AddDbContext<ParkingDbContext>(options =>
{
    if (provider is "postgres" or "postgresql") options.UseNpgsql(connectionString);
    else options.UseSqlite(connectionString);
});
builder.Services.AddScoped<IParkingService, ParkingService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<LicenseService>();
builder.Services.AddHealthChecks().AddDbContextCheck<ParkingDbContext>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddDataProtection()
    .SetApplicationName("ParkingPejam")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataDir, "keys")));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("login", policy =>
    {
        policy.PermitLimit = 12;
        policy.Window = TimeSpan.FromMinutes(1);
        policy.QueueLimit = 0;
    });
});
builder.Services.AddHostedService<SimulationService>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "parking-pejam-auth";
        options.LoginPath = "/login.html";
        options.AccessDeniedPath = "/login.html?denied=1";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsProduction() ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
    });
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStaticFiles();

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
    ctx.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    ctx.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
    ctx.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";
    await next();
});

if (app.Environment.IsProduction()) app.UseHsts();
app.UseRateLimiter();
app.UseAuthentication();

app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api") &&
        !ctx.Request.Path.StartsWithSegments("/api/auth") &&
        !ctx.Request.Path.StartsWithSegments("/api/license") &&
        !ctx.Request.Path.StartsWithSegments("/health") &&
        ctx.User.Identity?.IsAuthenticated == true)
    {
        var licensing = ctx.RequestServices.GetRequiredService<LicenseService>();
        var license = licensing.Validate();
        if (!license.IsValid)
        {
            ctx.Response.StatusCode = StatusCodes.Status423Locked;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new
            {
                error = "license_required",
                status = license.Status,
                message = license.Message ?? "A valid commercial license is required."
            });
            return;
        }

        var module = ResolveLicenseModule(ctx.Request.Path);
        if (!string.IsNullOrWhiteSpace(module) && !licensing.HasModule(module))
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new
            {
                error = "module_not_licensed",
                module,
                message = $"The '{module}' module is not enabled by this license."
            });
            return;
        }
    }
    await next();
});

app.UseAuthorization();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ParkingDbContext>();
    await db.Database.EnsureCreatedAsync();
    if (provider == "sqlite")
    {
        await DatabaseSchemaBootstrapper.EnsureImportSchemaAsync(db);
        await DispatchSchemaBootstrapper.EnsureAsync(db);
        await AdvancedSchemaBootstrapper.EnsureAsync(db);
    }
    await SeedAsync(db, app.Configuration, scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>());
}

app.MapOpenApi("/openapi/{documentName}.json");
app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }));
app.MapHealthChecks("/health/ready");

var licenseApi = app.MapGroup("/api/license");
licenseApi.MapGet("/status", (LicenseService service) => Results.Ok(service.Validate()));

var auth = app.MapGroup("/api/auth");
auth.MapPost("/login", async (LoginRequest request, ParkingDbContext db, IPasswordHasher<User> hasher, HttpContext ctx, CancellationToken ct) =>
{
    var username = request.Username?.Trim();
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest(new { message = "Username and password are required." });

    var user = await db.Users.SingleOrDefaultAsync(x => x.Username == username && x.IsActive, ct);
    if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        return Results.Unauthorized();

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, user.Role)
    };
    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
    return Results.Ok(new { username = user.Username, role = user.Role });
}).RequireRateLimiting("login");

auth.MapPost("/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok();
});
auth.MapGet("/me", (ClaimsPrincipal user) => user.Identity?.IsAuthenticated == true
    ? Results.Ok(new { username = user.Identity.Name, role = user.FindFirstValue(ClaimTypes.Role) })
    : Results.Unauthorized());

var api = app.MapGroup("/api/parking").RequireAuthorization();
api.MapGet("/spots", async (string? zone, IParkingService service, CancellationToken ct) => Results.Ok(await service.GetSpotsAsync(zone, ct)));
api.MapGet("/spots/{id:guid}", async (Guid id, IParkingService service, CancellationToken ct) =>
{
    var spot = await service.GetSpotAsync(id, ct);
    return spot is null ? Results.NotFound() : Results.Ok(spot);
});
api.MapGet("/statistics", async (IParkingService service, CancellationToken ct) => Results.Ok(await service.GetStatisticsAsync(ct)));
api.MapGet("/events", async (int? take, IParkingService service, CancellationToken ct) => Results.Ok(await service.GetEventsAsync(take ?? 50, ct)));
api.MapPut("/spots/{id:guid}/status", async (Guid id, ChangeParkingStatusRequest request, ClaimsPrincipal user, IParkingService service, CancellationToken ct) =>
{
    if (!(user.IsInRole("Admin") || user.IsInRole("Operator"))) return Results.Forbid();
    var updated = await service.ChangeStatusAsync(id, request.Status, request.Source ?? "web", user.Identity?.Name, ct);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
});
api.MapGet("/export/spots.csv", async (IParkingService service, CancellationToken ct) =>
{
    var spots = await service.GetSpotsAsync(cancellationToken: ct);
    var csv = new StringBuilder("SpotNumber,Zone,Row,Column,Status,UpdatedAtUtc\n");
    foreach (var s in spots) csv.AppendLine(string.Join(",", Csv(s.SpotNumber), Csv(s.Zone), s.Row, s.Column, Csv(s.Status.ToString()), Csv(s.UpdatedAtUtc.ToString("O"))));
    return Results.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"parking-spots-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
});
api.MapGet("/export/events.csv", async (int? take, IParkingService service, CancellationToken ct) =>
{
    var events = await service.GetEventsAsync(take ?? 200, ct);
    var csv = new StringBuilder("SpotNumber,OldStatus,NewStatus,Source,Actor,TimestampUtc\n");
    foreach (var e in events) csv.AppendLine(string.Join(",", Csv(e.SpotNumber), Csv(e.OldStatus.ToString()), Csv(e.NewStatus.ToString()), Csv(e.Source), Csv(e.Actor), Csv(e.TimestampUtc.ToString("O"))));
    return Results.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"parking-events-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
});
api.MapGet("/export/report.json", async (IParkingService service, CancellationToken ct) => Results.Ok(new
{
    generatedAtUtc = DateTimeOffset.UtcNow,
    statistics = await service.GetStatisticsAsync(ct),
    spots = await service.GetSpotsAsync(cancellationToken: ct),
    events = await service.GetEventsAsync(200, ct)
}));

var sensors = app.MapGroup("/api/sensors");
sensors.MapPost("/{externalId}/readings", async (string externalId, SensorReadingRequest request, HttpRequest requestHttp, ParkingDbContext db, CancellationToken ct) =>
{
    var expected = app.Configuration["Parking:SensorIngressKey"];
    if (string.IsNullOrWhiteSpace(expected) || !requestHttp.Headers.TryGetValue("X-Sensor-Key", out var key) || !string.Equals(expected, key.ToString(), StringComparison.Ordinal))
        return Results.Unauthorized();
    var sensor = await db.ParkingSensors.Include(x => x.ParkingSpot).SingleOrDefaultAsync(x => x.ExternalId == externalId && x.IsActive, ct);
    if (sensor?.ParkingSpot is null) return Results.NotFound();
    var now = DateTimeOffset.UtcNow;
    sensor.CurrentOccupied = request.Occupied;
    sensor.LastSeenUtc = now;
    sensor.UpdatedAtUtc = now;
    db.ParkingSensorReadings.Add(new ParkingSensorReading { ParkingSensorId = sensor.Id, Occupied = request.Occupied, BatteryPercent = request.BatteryPercent, TemperatureC = request.TemperatureC, ReceivedAtUtc = now });
    var next = request.Occupied ? ParkingSpotStatus.Occupied : ParkingSpotStatus.Free;
    if (sensor.ParkingSpot.Status != next)
    {
        var old = sensor.ParkingSpot.Status;
        sensor.ParkingSpot.ChangeStatus(next);
        db.ParkingEvents.Add(new ParkingEvent { ParkingSpotId = sensor.ParkingSpot.Id, OldStatus = old, NewStatus = next, Source = "sensor", Actor = externalId, TimestampUtc = now });
    }
    await db.SaveChangesAsync(ct);
    return Results.Ok(new { sensor = externalId, spot = sensor.ParkingSpot.SpotNumber, status = sensor.ParkingSpot.Status.ToString(), receivedAtUtc = now });
});
sensors.MapGet("", async (ParkingDbContext db, CancellationToken ct) => Results.Ok(await db.ParkingSensors.AsNoTracking().Include(x => x.ParkingSpot).OrderBy(x => x.ExternalId).Select(x => new
{
    x.ExternalId,
    spot = x.ParkingSpot!.SpotNumber,
    x.CurrentOccupied,
    x.LastSeenUtc,
    online = x.LastSeenUtc != null && x.LastSeenUtc > DateTimeOffset.UtcNow.AddMinutes(-2)
}).ToListAsync(ct))).RequireAuthorization();

app.MapImportWorkflow();
app.MapDispatchWorkflow();
app.MapAdvancedOperations();
app.MapOperationsQuery();
app.MapGet("/", (ClaimsPrincipal user) => user.Identity?.IsAuthenticated == true
    ? Results.Ok(new { application = "Parking Pejam", status = "authenticated" })
    : Results.Redirect("/login.html"));
app.MapFallbackToFile("index.html");
app.Run();

static string? ResolveLicenseModule(PathString path)
{
    var p = path.Value ?? string.Empty;
    if (p.StartsWith("/api/sensors", StringComparison.OrdinalIgnoreCase)) return "Sensors";
    if (p.StartsWith("/api/import", StringComparison.OrdinalIgnoreCase)) return "Import";
    if (p.StartsWith("/api/dispatch", StringComparison.OrdinalIgnoreCase)) return "Dispatch";
    if (p.Contains("/manifest", StringComparison.OrdinalIgnoreCase)) return "Manifest";
    if (p.Contains("/inspection", StringComparison.OrdinalIgnoreCase)) return "Inspection";
    if (p.Contains("/holds", StringComparison.OrdinalIgnoreCase)) return "Customs";
    if (p.Contains("/yard", StringComparison.OrdinalIgnoreCase)) return "Yard";
    if (p.Contains("/gate", StringComparison.OrdinalIgnoreCase)) return "Gate";
    if (p.Contains("/documents", StringComparison.OrdinalIgnoreCase)) return "Documents";
    if (p.Contains("/customers", StringComparison.OrdinalIgnoreCase) || p.Contains("/customer", StringComparison.OrdinalIgnoreCase)) return "Customers";
    if (p.Contains("/keys", StringComparison.OrdinalIgnoreCase)) return "Keys";
    if (p.Contains("/billing", StringComparison.OrdinalIgnoreCase)) return "Billing";
    if (p.Contains("/lpr", StringComparison.OrdinalIgnoreCase)) return "LPR";
    if (p.Contains("/analytics", StringComparison.OrdinalIgnoreCase) || p.Contains("/export", StringComparison.OrdinalIgnoreCase)) return "Reports";
    if (p.StartsWith("/api/parking", StringComparison.OrdinalIgnoreCase)) return "Yard";
    return null;
}

static string Csv(string? value) => "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";

static async Task SeedAsync(ParkingDbContext db, IConfiguration config, IPasswordHasher<User> hasher)
{
    if (!await db.ParkingSpots.AnyAsync())
    {
        var spots = new List<ParkingSpot>();
        for (var zi = 0; zi < 3; zi++)
        {
            var zone = ((char)('A' + zi)).ToString();
            for (var row = 1; row <= 2; row++)
                for (var column = 1; column <= 8; column++)
                    spots.Add(new ParkingSpot { Id = Guid.NewGuid(), SpotNumber = $"{zone}-{row:D1}{column:D2}", Zone = zone, Row = row, Column = column });
        }
        db.ParkingSpots.AddRange(spots);
        await db.SaveChangesAsync();
    }

    if (!await db.Users.AnyAsync())
    {
        var password = config["Parking:BootstrapAdminPassword"];
        if (!string.IsNullOrWhiteSpace(password))
        {
            var admin = new User { Id = Guid.NewGuid(), Username = "admin", Role = "Admin" };
            admin.PasswordHash = hasher.HashPassword(admin, password);
            db.Users.Add(admin);
        }
    }

    if (!await db.ParkingSensors.AnyAsync())
    {
        var sensorKey = config["Parking:SensorIngressKey"];
        if (!string.IsNullOrWhiteSpace(sensorKey))
        {
            var spots = await db.ParkingSpots.ToListAsync();
            db.ParkingSensors.AddRange(spots.Select(s => new ParkingSensor { Id = Guid.NewGuid(), ParkingSpotId = s.Id, ExternalId = $"PEJAM-{s.SpotNumber}", DeviceKey = sensorKey, CurrentOccupied = false }));
        }
    }
    await db.SaveChangesAsync();
}

public sealed record LoginRequest(string? Username, string? Password);
public sealed record SensorReadingRequest(bool Occupied, double? BatteryPercent = null, double? TemperatureC = null);
