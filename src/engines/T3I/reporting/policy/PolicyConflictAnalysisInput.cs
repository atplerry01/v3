using Whycespace.Engines.T0U.WhycePolicy.Simulation;
namespace Whycespace.Engines.T3I.Reporting.Policy;

using Whycespace.Engines.T0U.WhycePolicy.Enforcement;
using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyConflictAnalysisInput(
    IReadOnlyList<PolicyDefinition> Policies,
    IReadOnlyList<PolicyConflictRecord> DetectedConflicts,
    IReadOnlyList<PolicySimulationRecord> SimulationResults
);
