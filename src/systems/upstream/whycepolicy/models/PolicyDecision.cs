namespace Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyDecision(
    string PolicyId,
    bool Allowed,
    string Action,
    string Reason,
    DateTime EvaluatedAt
);
