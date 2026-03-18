namespace Whycespace.Engines.T0U.Governance.Roles.Assignment;

using Whycespace.Systems.Upstream.Governance.Models;

public sealed record AssignGovernanceRoleCommand(
    Guid CommandId,
    Guid TargetGuardianId,
    GuardianRole AssignedRole,
    string AuthorityDomain,
    Guid RequestedBy,
    DateTime Timestamp);
