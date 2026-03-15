namespace Whycespace.Engines.T3I.WhycePolicy;

public sealed record PolicyAnomalyRecord(
    string PolicyId,
    PolicyAnomalyType AnomalyType,
    PolicyAnomalySeverity Severity,
    string ObservationDetails,
    DateTime DetectedAt
);

public enum PolicyAnomalyType
{
    EXCESSIVE_DENIAL_RATE,
    FREQUENT_ESCALATION,
    UNEXPECTED_POLICY_ACTIVATION,
    CONFLICT_SPIKE,
    DECISION_PATTERN_CHANGE
}

public enum PolicyAnomalySeverity
{
    LOW,
    MEDIUM,
    HIGH,
    CRITICAL
}
