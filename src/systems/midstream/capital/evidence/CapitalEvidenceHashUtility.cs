namespace Whycespace.Systems.Midstream.Capital.Evidence;

using global::System.Security.Cryptography;
using global::System.Text;

public static class CapitalEvidenceHashUtility
{
    public static string ComputeEvidenceHash(
        Guid capitalId,
        Guid poolId,
        Guid referenceId,
        decimal amount,
        string currency,
        DateTime timestamp)
    {
        var input = string.Join("|",
            capitalId.ToString(),
            poolId.ToString(),
            referenceId.ToString(),
            amount.ToString("F8"),
            currency,
            timestamp.ToString("O"));

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}
