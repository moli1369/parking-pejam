using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ParkingPejam.Domain.Entities;

namespace ParkingPejam.Infrastructure.Licensing;

public sealed class LicenseService
{
    private const string PublicKeyPem = """-----BEGIN PUBLIC KEY-----
MIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEAuf1jbKhNPPYH5EMcYCq9
nEIzYNrYB/edfAJlAm/e4hIjB6bfh/b19oIwrQdjD54r8Zkl9/UcSHACo66LeCre
TB5SM+L36ftqPOHO8i8eYJ8lVWv1DVT0OyvyoXmeoa7Vl82K9kGUIqdx8yAIlgIP
V3vVs3cdfWQtcayQNE7oDjDL7FkmnE4/Gwp30MFsUnfrznAu2fEa77jdXso83Cgf
pNrzP+mXbPT8Vh5NauDUehXfNYzG+iMpBTmrBGbDjDyQxz+NeoBvnsH/2zun6KXd
zoGcpUi2JAXxp59196Po71oAfoWchj6q1iFR06gJkLFFwbDWpdGyZqqipjhvJ13x
MDqeebV4bSAQ2EQdcBTy494AW5v9Iy72nS5GELDsE7ABqFcefE4beNr00XVeOMDq
4WcfBwKjdOCQjh7nATTrmnYG741nDI4aJB5AjxCzWtGc73rGi3KSUlYcRbTriI+z
0ZiWCuMjAXciUzNybWJA7RZos6JoesW0F9aVnTx4hB00HOd5fvYJXeCweWghNoa+
jiWas8gFFtPVu0KH+kvymkNXXA0hxHS3Ox0BhEGngtmAcgnV6kv48u55psr8J4oC
1WtSWAfZTUjipX7ka865BQTtThkUgvifiDzYdr+t1zMbxW0+yqoRwpKVRbfq+F2c
GkANxV/ps3IDIulTqP/8cTkCAwEAAQ==
-----END PUBLIC KEY-----""";

private readonly IConfiguration _configuration;
private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };

public LicenseService(IConfiguration configuration) => _configuration = configuration;

public LicenseValidationResult Validate()
{
    if (!_configuration.GetValue("Licensing:RequireLicense", false))
        return new(true, "bypass", "License enforcement is disabled for this environment.", null);

    var path = _configuration["Licensing:LicensePath"] ?? "license.json";
    if (!File.Exists(path))
        return new(false, "missing", "A valid commercial license is required.", null);

    try
    {
        var signed = JsonSerializer.Deserialize<SignedLicense>(File.ReadAllText(path), _jsonOptions);
        if (signed?.Payload is null || string.IsNullOrWhiteSpace(signed.Signature))
            return new(false, "invalid", "License file is malformed.", null);

        var payloadJson = JsonSerializer.Serialize(signed.Payload, _jsonOptions);
        using var rsa = RSA.Create();
        rsa.ImportFromPem(PublicKeyPem);
        var validSignature = rsa.VerifyData(
            Encoding.UTF8.GetBytes(payloadJson),
            Convert.FromBase64String(signed.Signature),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        if (!validSignature)
            return new(false, "invalid", "License signature is invalid.", null);

        var now = DateTimeOffset.UtcNow;
        if (now > signed.Payload.ExpiresAtUtc)
        {
            var graceUntil = signed.Payload.ExpiresAtUtc.AddDays(Math.Max(0, signed.Payload.GracePeriodDays));
            if (now <= graceUntil)
                return new(true, "grace", $"License expired; offline grace period ends {graceUntil:O}.", signed.Payload);
            return new(false, "expired", "License has expired.", signed.Payload);
        }

        var expectedInstallation = _configuration["Licensing:InstallationId"];
        if (!string.IsNullOrWhiteSpace(expectedInstallation) &&
            !string.Equals(expectedInstallation, signed.Payload.InstallationId, StringComparison.OrdinalIgnoreCase))
            return new(false, "installation-mismatch", "License is not valid for this installation.", signed.Payload);

        return new(true, "valid", null, signed.Payload);
    }
    catch (Exception ex) when (ex is JsonException or CryptographicException or FormatException or IOException)
    {
        return new(false, "invalid", "License could not be validated.", null);
    }
}

public bool HasModule(string module)
{
    var result = Validate();
    if (!result.IsValid || result.License is null) return false;
    return result.License.Modules.Contains(module, StringComparer.OrdinalIgnoreCase);
}
}
