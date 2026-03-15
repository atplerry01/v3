namespace Whycespace.Engines.T0U.WhycePolicy;

using Whycespace.System.Upstream.WhycePolicy.Models;

public sealed record PolicyEvaluationResult(
    List<PolicyDecision> Decisions,
    PolicyDecision FinalDecision
);
