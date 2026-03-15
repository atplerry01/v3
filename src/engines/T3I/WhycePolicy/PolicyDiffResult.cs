namespace Whycespace.Engines.T3I.WhycePolicy;

public sealed record PolicyDiffResult(
    string PolicyId,
    IReadOnlyList<PolicyChangeRecord> Changes,
    int ChangeCount,
    DateTime GeneratedAt
);
