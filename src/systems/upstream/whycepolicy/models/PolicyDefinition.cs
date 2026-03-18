namespace Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyCondition(string Field, string Operator, string Value);

public sealed record PolicyAction(string ActionType, IReadOnlyDictionary<string, string> Parameters);

public sealed record PolicyDefinition(
    string PolicyId,
    string Name,
    int Version,
    string TargetDomain,
    IReadOnlyList<PolicyCondition> Conditions,
    IReadOnlyList<PolicyAction> Actions,
    DateTime CreatedAt,
    PolicyPriority Priority = PolicyPriority.Medium,
    PolicyLifecycleState LifecycleState = PolicyLifecycleState.Draft
);
