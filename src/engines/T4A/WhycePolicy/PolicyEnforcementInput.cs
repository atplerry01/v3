namespace Whycespace.Engines.T4A.WhycePolicy;

using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyEnforcementInput(
    PolicyDecision FinalDecision,
    IReadOnlyList<PolicyDecision> Decisions,
    string CommandType,
    string ResourceType,
    string ResourceId,
    string ActorId
);
