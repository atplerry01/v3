namespace Whycespace.Engines.T0U.WhycePolicy;

using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyEvaluationResult(
    List<PolicyDecision> Decisions,
    PolicyDecision FinalDecision
);
