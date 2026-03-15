namespace Whycespace.Engines.T0U.WhycePolicy;

public sealed record PolicyConflictResult(
    IReadOnlyList<PolicyConflictRecord> Conflicts,
    int ConflictCount
);
