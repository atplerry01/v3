namespace Whycespace.Engines.T3I.Reporting.Policy.Models;

public sealed record PolicyDiffResult(
    string PolicyId,
    IReadOnlyList<PolicyChangeRecord> Changes,
    int ChangeCount,
    DateTime GeneratedAt
);
