namespace Whycespace.Engines.T3I.WhycePolicy;

using global::System.Security.Cryptography;
using global::System.Text;
using global::System.Text.Json;

public static class PolicyEvidenceHashGenerator
{
    public static string GenerateContextHash(Dictionary<string, object> evidenceContext)
    {
        var sortedEntries = evidenceContext
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $"{kv.Key}={SerializeValue(kv.Value)}");

        var input = string.Join("|", sortedEntries);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    public static string GenerateEvidenceHash(
        string policyId,
        string actionType,
        string actorId,
        string contextHash,
        DateTime timestamp)
    {
        var input = $"{policyId}|{actionType}|{actorId}|{contextHash}|{timestamp:O}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    public static string GenerateEvidenceId(
        string policyId,
        string actionType,
        string actorId,
        string evidenceHash,
        DateTime timestamp)
    {
        var input = $"evidence|{policyId}|{actionType}|{actorId}|{evidenceHash}|{timestamp:O}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    private static string SerializeValue(object value)
    {
        return value switch
        {
            string s => s,
            null => string.Empty,
            _ => JsonSerializer.Serialize(value)
        };
    }
}
