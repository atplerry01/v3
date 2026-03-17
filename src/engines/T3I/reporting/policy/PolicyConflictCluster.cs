namespace Whycespace.Engines.T3I.Reporting.Policy;

public sealed record PolicyConflictCluster(
    string ClusterId,
    IReadOnlyList<string> Policies,
    AnalysisConflictType ConflictType,
    ConflictSeverity Severity,
    string RecommendedAction
);

public enum AnalysisConflictType
{
    ACTION_CONFLICT,
    PRIORITY_CHAIN_CONFLICT,
    DEPENDENCY_CONFLICT,
    DOMAIN_CONFLICT
}

public enum ConflictSeverity
{
    LOW,
    MEDIUM,
    HIGH,
    CRITICAL
}
