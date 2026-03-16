namespace Whycespace.Engines.T0U.Governance.Commands;

using Whycespace.Systems.Upstream.Governance.Models;

public sealed record AssignGovernanceRoleCommand(
    Guid CommandId,
    Guid TargetGuardianId,
    GuardianRole AssignedRole,
    string AuthorityDomain,
    Guid RequestedBy,
    DateTime Timestamp);
