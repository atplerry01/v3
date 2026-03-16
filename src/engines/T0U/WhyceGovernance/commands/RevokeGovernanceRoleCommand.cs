namespace Whycespace.Engines.T0U.WhyceGovernance.Commands;

using Whycespace.Systems.Upstream.Governance.Models;

public sealed record RevokeGovernanceRoleCommand(
    Guid CommandId,
    Guid TargetGuardianId,
    GuardianRole RevokedRole,
    string Reason,
    Guid RequestedBy,
    DateTime Timestamp);
