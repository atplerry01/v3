namespace Whycespace.Engines.T0U.Governance.Commands;

using Whycespace.System.Upstream.Governance.Models;

public sealed record RevokeGovernanceRoleCommand(
    Guid CommandId,
    Guid TargetGuardianId,
    GuardianRole RevokedRole,
    string Reason,
    Guid RequestedBy,
    DateTime Timestamp);
