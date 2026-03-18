namespace Whycespace.Systems.Downstream.Cwg.Governance.Policy;

public sealed record CwgPolicyEvaluationContext(
    Guid SubjectId,
    string ResourceType,
    Guid ResourceId,
    string Action,
    decimal? Amount = null,
    string? ContributionType = null,
    DateTimeOffset? Timestamp = null
);
