namespace ParkingPejam.Domain.Entities;

public sealed class LicensePayload
{
    public string LicenseId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string InstallationId { get; set; } = string.Empty;
    public string Plan { get; set; } = "Evaluation";
    public DateTimeOffset IssuedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public int MaxUsers { get; set; }
    public int MaxYards { get; set; }
    public int MaxVehiclesPerMonth { get; set; }
    public int GracePeriodDays { get; set; } = 30;
    public List<string> Modules { get; set; } = [];
}

public sealed class SignedLicense
{
    public LicensePayload Payload { get; set; } = new();
    public string Signature { get; set; } = string.Empty;
}

public sealed record LicenseValidationResult(
    bool IsValid,
    string Status,
    string? Message,
    LicensePayload? License);
