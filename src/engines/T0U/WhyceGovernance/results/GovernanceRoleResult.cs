namespace Whycespace.Engines.T0U.WhyceGovernance.Results;

using Whycespace.Systems.Upstream.Governance.Models;

public enum GovernanceRoleAction
{
    Assigned,
    Revoked,
}

public sealed record GovernanceRoleResult(
    bool Success,
    Guid GuardianId,
    GuardianRole Role,
    GovernanceRoleAction Action,
    string Message,
    DateTime ExecutedAt)
{
    public static GovernanceRoleResult Failure(Guid guardianId, GuardianRole role, GovernanceRoleAction action, string message)
        => new(false, guardianId, role, action, message, DateTime.UtcNow);
}
