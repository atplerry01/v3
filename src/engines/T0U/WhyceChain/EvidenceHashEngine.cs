namespace Whycespace.Engines.T0U.WhyceChain;

using global::System.Security.Cryptography;
using global::System.Text;
using global::System.Text.Json;
using Whycespace.System.Upstream.WhyceChain.Models;

public sealed class EvidenceHashEngine
{
    private const string Algorithm = "SHA256";

    public EvidenceHash HashPayload(string payload)
    {
        var hash = ComputeHash(payload);
        return new EvidenceHash(hash, Algorithm, DateTimeOffset.UtcNow);
    }

    public EvidenceHash HashObject(object obj)
    {
        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
        return HashPayload(json);
    }

    public bool VerifyHash(string payload, string expectedHash)
    {
        var actual = ComputeHash(payload);
        return actual == expectedHash;
    }

    private static string ComputeHash(string input)
    {
        return Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(input)));
    }
}
