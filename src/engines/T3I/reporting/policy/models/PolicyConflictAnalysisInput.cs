using Whycespace.Engines.T0U.WhycePolicy.Simulation.Engines;
using Whycespace.Engines.T0U.WhycePolicy.Simulation.Models;
using Whycespace.Engines.T0U.WhycePolicy.Simulation.Forecasting;
namespace Whycespace.Engines.T3I.Reporting.Policy.Models;

using Whycespace.Engines.T0U.WhycePolicy.Enforcement.Engines;
using Whycespace.Engines.T0U.WhycePolicy.Enforcement.Safeguards;
using Whycespace.Engines.T0U.WhycePolicy.Enforcement.Authority;
using Whycespace.Engines.T0U.WhycePolicy.Governance.Conflict;
using Whycespace.Engines.T0U.WhycePolicy.Governance.Dependency;
using Whycespace.Engines.T0U.WhycePolicy.Governance.DomainBinding;
using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyConflictAnalysisInput(
    IReadOnlyList<PolicyDefinition> Policies,
    IReadOnlyList<PolicyConflictRecord> DetectedConflicts,
    IReadOnlyList<PolicySimulationRecord> SimulationResults
);
