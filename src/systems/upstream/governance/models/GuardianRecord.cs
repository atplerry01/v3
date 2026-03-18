namespace Whycespace.Systems.Upstream.Governance.Models;

public sealed record GuardianRecord(
    Guid GuardianId,
    string IdentityId,
    string GuardianName,
    GuardianRole GuardianRole,
    GuardianStatus GuardianStatus,
    IReadOnlyList<string> AuthorityDomains,
    DateTime AppointedAt,
    string AppointedBy,
    DateTime TermStart,
    DateTime? TermEnd,
    IReadOnlyDictionary<string, string> Metadata);
