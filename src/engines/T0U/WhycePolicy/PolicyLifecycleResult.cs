namespace Whycespace.Engines.T0U.WhycePolicy;

using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyLifecycleResult(
    string PolicyId,
    PolicyLifecycleState PreviousState,
    PolicyLifecycleState NewState,
    bool TransitionAllowed,
    string TransitionReason,
    DateTime ProcessedAt
);
