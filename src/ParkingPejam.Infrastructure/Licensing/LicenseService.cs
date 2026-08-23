using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ParkingPejam.Domain.Entities;

namespace ParkingPejam.Infrastructure.Licensing;

public sealed class LicenseService
{
    private const string PublicKeyPem = """-----BEGIN PUBLIC KEY-----
MIIBojANBgkqhkiG9w0BAQEFAAOCAYEAtJqx2HLgF6hF6D4XepH1
Fw09VyxvGnr9yLo50sNdHP+zYHc+99IMVN4cfazXg1KPsTC4v0MakrgoRy0ge8fB
l3Iwc5Bna/+I2LJmIhKZXx+/6FWNIOFKwPc2LsTDolhWCrAs/3prVwBhiaNYxDGq
s5YZPPqCQsWgY1eSkBDF2ft0LMRpq47WsTokhTTlpSZI3QsSTzuA9hQ6+SuQ0ipP
gfjrj47idrUeIuPFDRp9wD1k5laLLPizG+9molfB+FCrUpsrZ+QxTBGePJ3gFhy0
DxBrzTK/6U0eXO6EXGsYX44wmPPnEL3QQ3DhQhGdiaZvExU0uTTt4CEhnjTjPcDd
rofoINnvxIkGVntpbWxBxSIs7rGo0ohyx97qFZDBQNMQFcGLVfIfGJRSAWO9UQSb
SmP6/SDWawfxBusFVZyoB6kTKRVVudOcFCmdP43t3T2DBD/HaexUUvY8QXkUZvTy
kmbtpsMB/MNBYH4GBppyDu66Zc7mzUqatRZhXX43GApVAgMBAAE=
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
