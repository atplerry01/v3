namespace Whycespace.Engines.T3I.WhycePolicy;

using Whycespace.Engines.T0U.WhycePolicy;
using Whycespace.System.Upstream.WhycePolicy.Models;

public sealed record PolicyConflictAnalysisInput(
    IReadOnlyList<PolicyDefinition> Policies,
    IReadOnlyList<PolicyConflictRecord> DetectedConflicts,
    IReadOnlyList<PolicySimulationRecord> SimulationResults
);
