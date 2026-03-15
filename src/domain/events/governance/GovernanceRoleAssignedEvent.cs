namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceRoleAssignedEvent(
    Guid EventId,
    Guid GuardianId,
    string AssignedRole,
    string AuthorityDomain,
    Guid AssignedBy,
    DateTime Timestamp);
