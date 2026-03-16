namespace Whycespace.Engines.T3I.WhycePolicy;

using Whycespace.Engines.T0U.WhycePolicy;
using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyMonitoringInput(
    IReadOnlyList<PolicyEvaluationResult> PolicyDecisionRecords,
    IReadOnlyList<PolicyEnforcementResult> EnforcementRecords,
    TimeRange ObservationWindow
);

public sealed record TimeRange(
    DateTime Start,
    DateTime End
);
