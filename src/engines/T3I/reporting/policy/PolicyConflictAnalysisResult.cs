namespace Whycespace.Engines.T3I.Reporting.Policy;

public sealed record PolicyConflictAnalysisResult(
    List<PolicyConflictCluster> ConflictClusters,
    List<EscalationRisk> EscalationRisks,
    List<PriorityOverrideChain> PriorityOverrideChains,
    DateTime AnalysisGeneratedAt
);

public sealed record EscalationRisk(
    string PolicyId,
    string Domain,
    string RiskType,
    ConflictSeverity Severity,
    string Description
);

public sealed record PriorityOverrideChain(
    IReadOnlyList<string> PolicyChain,
    string Domain,
    string Description
);
