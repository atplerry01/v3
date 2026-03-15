namespace Whycespace.Engines.T3I.WhycePolicy;

using global::System.Security.Cryptography;
using global::System.Text;

public static class PolicyAuditHashGenerator
{
    public static string GenerateContextHash(string policyId, string actorId, string evaluationContext)
    {
        var input = $"{policyId}|{actorId}|{evaluationContext}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    public static string GenerateAuditId(
        string policyId,
        string actorId,
        PolicyAuditActionType actionType,
        string contextHash,
        DateTime timestamp)
    {
        var input = $"{policyId}|{actorId}|{actionType}|{contextHash}|{timestamp:O}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}
