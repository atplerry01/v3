namespace Whycespace.Systems.Downstream.Clusters.Administration.Policy;

public sealed record ClusterPolicyEvaluationContext(
    string ClusterId,
    Guid InitiatorId,
    string Action,
    string? ProviderType = null,
    string? Role = null,
    DateTimeOffset? Timestamp = null
);
