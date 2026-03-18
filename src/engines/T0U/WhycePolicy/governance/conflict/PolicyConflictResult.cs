namespace Whycespace.Engines.T0U.WhycePolicy.Governance.Conflict;

public sealed record PolicyConflictResult(
    IReadOnlyList<PolicyConflictRecord> Conflicts,
    int ConflictCount
);
