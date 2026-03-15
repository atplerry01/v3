namespace Whycespace.Engines.T3I.WhycePolicy;

public enum ImpactType
{
    NO_CHANGE,
    DECISION_CHANGE,
    ESCALATION_CHANGE,
    ACCESS_CHANGE,
    GOVERNANCE_CHANGE
}

public enum ImpactSeverity
{
    LOW,
    MEDIUM,
    HIGH,
    CRITICAL
}

public sealed record PolicyImpactRecord(
    Guid ContextId,
    string CurrentDecision,
    string ProposedDecision,
    ImpactType ImpactType,
    ImpactSeverity Severity
);
