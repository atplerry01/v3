namespace Whycespace.Engines.T0U.WhycePolicy.Enforcement;

public sealed record PolicyConflictResult(
    IReadOnlyList<PolicyConflictRecord> Conflicts,
    int ConflictCount
);
