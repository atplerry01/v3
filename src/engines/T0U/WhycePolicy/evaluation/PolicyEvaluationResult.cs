namespace Whycespace.Engines.T0U.WhycePolicy.Evaluation;

using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyEvaluationResult(
    List<PolicyDecision> Decisions,
    PolicyDecision FinalDecision
);
