namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceRoleAssignedEvent(
    Guid EventId,
    Guid GuardianId,
    string AssignedRole,
    string AuthorityDomain,
    Guid AssignedBy,
    DateTime Timestamp);
