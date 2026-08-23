using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ParkingPejam.Domain.Entities;

namespace ParkingPejam.Infrastructure.Licensing;

public sealed class LicenseService
{
    // Vendor public key only. The matching private key must never be committed or shipped.
    private const string PublicKeyPem = """-----BEGIN PUBLIC KEY-----
MIIBojANBgkqhkiG9w0BAQEFAAOCAY8AMIIBigKCAYEA1CpbjCnFudMYXZ20O/v7
Ykz16eVlxuVbWxfKwtNaT7IgbhEHgxKmPFb3e3GJX2WBg4W2+PeZesSaKkcUArVO
yJbQ6VlQHCnwLLhrObrYa4BS9Xtkcsnqcvj8EJCyiQxnqNIM3Dc4bHJLiRn7gF2L
GjuY1kL6LhWXnI/LD0VOwTJYe7mgTpseh7vUUKhDetlbOZpW6csQNijziI0cRE6r
9uWHU+xQT3YatWiD5SGFyRcpG+WcbOjj0gD6RPp21j+n56koQG39VQ64p/0mYocv
eBpZwn1Ip16rAw9Wz8mz8IdeHtHv1JmcdpV8iURQAEyhqnUruGHC+v+eBicDwOB
W4XxBFjXsdxzClwlcbDTzdxo6Qo7RdLzLzR3JBdxgaj2X+/xGiRPt+Wz2Ku2mXXH
C+bT+QcNMYK3xWZ2eecFr/cHkZxcaBttcW/ONoFRtqaXPo+IVfyj2WfJQCWXS1xk
74U46ZWrTGPhXCOq62u1jI1Ka7396OvS6PGuDXj5oD+fAgMBAAE=
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
                RSASignaturePadding.Pss);

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
