namespace Whycespace.Engines.T0U.WhycePolicy.Lifecycle;

using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyLifecycleCommand(
    string PolicyId,
    PolicyLifecycleState CurrentState,
    PolicyLifecycleState TargetState,
    string RequestedBy,
    string Reason
);
