namespace Whycespace.Systems.Downstream.Work.Shared.Policy;

public sealed record WorkPolicyEvaluationContext(
    string WorkerId,
    string TaskType,
    string ClusterId,
    string SubClusterId,
    string Action,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, object>? AdditionalAttributes = null
);
