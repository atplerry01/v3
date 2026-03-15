namespace Whycespace.System.Upstream.Governance.Models;

public sealed record GovernanceDomainScope(
    string ScopeId,
    string Name,
    string Description,
    bool IsActive,
    Guid RegisteredByGuardianId,
    DateTime RegisteredAt,
    DateTime? DeactivatedAt);
