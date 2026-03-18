namespace Whycespace.Engines.T3I.Reporting.Governance.Engines;

using global::System.Security.Cryptography;
using global::System.Text;
using Whycespace.Engines.T3I.Reporting.Governance.Models;

public static class GovernanceAuditHashGenerator
{
    public static string GenerateAuditHash(
        Guid proposalId,
        GovernanceAuditActionType actionType,
        Guid performedBy,
        Guid actionReferenceId,
        DateTime timestamp)
    {
        var input = $"{proposalId}|{actionType}|{performedBy}|{actionReferenceId}|{timestamp:O}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    public static string GenerateAuditId(
        Guid proposalId,
        GovernanceAuditActionType actionType,
        Guid performedBy,
        DateTime timestamp)
    {
        var input = $"gov-audit|{proposalId}|{actionType}|{performedBy}|{timestamp:O}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}
