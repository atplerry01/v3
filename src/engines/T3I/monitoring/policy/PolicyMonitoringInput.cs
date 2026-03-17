using Whycespace.Engines.T0U.WhycePolicy.Evaluation;
namespace Whycespace.Engines.T3I.Monitoring.Policy;

using Whycespace.Engines.T0U.WhycePolicy.Evaluation;
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
