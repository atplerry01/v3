namespace Whycespace.Systems.Upstream.WhycePolicy.Dsl;

public sealed record PolicyDslAction(
    PolicyActionType ActionType,
    string Reason,
    IReadOnlyDictionary<string, string> Metadata
);
