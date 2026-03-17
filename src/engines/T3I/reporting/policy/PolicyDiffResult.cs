namespace Whycespace.Engines.T3I.Reporting.Policy;

public sealed record PolicyDiffResult(
    string PolicyId,
    IReadOnlyList<PolicyChangeRecord> Changes,
    int ChangeCount,
    DateTime GeneratedAt
);
