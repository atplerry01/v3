namespace Whycespace.Systems.Upstream.WhycePolicy.Dsl;

using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyDslDefinition
{
    public required string PolicyId { get; init; }
    public required string PolicyName { get; init; }
    public required string PolicyDomain { get; init; }
    public string PolicyDescription { get; init; } = string.Empty;
    public required PolicyPriority Priority { get; init; }
    public required IReadOnlyList<PolicyDslCondition> Conditions { get; init; }
    public required IReadOnlyList<PolicyDslAction> Actions { get; init; }
    public PolicyLifecycleState LifecycleState { get; init; } = PolicyLifecycleState.Draft;
    public int Version { get; init; } = 1;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    public static PolicyDslDefinition Create(
        string policyId,
        string policyName,
        string policyDomain,
        IReadOnlyList<PolicyDslCondition> conditions,
        IReadOnlyList<PolicyDslAction> actions,
        PolicyPriority priority = PolicyPriority.Medium,
        string description = "")
    {
        if (conditions.Count == 0)
            throw new ArgumentException("Policy must have at least one condition.", nameof(conditions));

        if (actions.Count == 0)
            throw new ArgumentException("Policy must have at least one action.", nameof(actions));

        return new PolicyDslDefinition
        {
            PolicyId = policyId,
            PolicyName = policyName,
            PolicyDomain = policyDomain,
            PolicyDescription = description,
            Priority = priority,
            Conditions = conditions,
            Actions = actions
        };
    }
}
