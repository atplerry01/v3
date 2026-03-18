using Whycespace.Engines.T0U.WhycePolicy.Evaluation.Engines;
using Whycespace.Engines.T0U.WhycePolicy.Evaluation.Models;
using Whycespace.Engines.T0U.WhycePolicy.Evaluation.Cache;
using Whycespace.Engines.T0U.WhycePolicy.Evaluation.Parser;
namespace Whycespace.Engines.T3I.Monitoring.Policy.Models;

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
