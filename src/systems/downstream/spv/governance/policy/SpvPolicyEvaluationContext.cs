namespace Whycespace.Systems.Downstream.Spv.Governance.Policy;

public sealed record SpvPolicyEvaluationContext(
    Guid SpvId,
    Guid InitiatorId,
    string Action,
    string? FromState = null,
    string? ToState = null,
    decimal? Capital = null,
    DateTimeOffset? Timestamp = null
);
