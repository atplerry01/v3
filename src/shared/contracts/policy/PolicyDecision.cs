namespace Whycespace.Contracts.Policy;

public sealed record PolicyDecision(
    string PolicyId,
    bool IsAllowed,
    string Reason,
    DateTimeOffset EvaluatedAt
);
