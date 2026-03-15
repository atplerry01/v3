namespace Whycespace.Engines.T0U.WhycePolicy;

using Whycespace.System.Upstream.WhycePolicy.Models;

public sealed record PolicyLifecycleCommand(
    string PolicyId,
    PolicyLifecycleState CurrentState,
    PolicyLifecycleState TargetState,
    string RequestedBy,
    string Reason
);
