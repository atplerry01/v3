namespace Whycespace.Engines.T0U.WhycePolicy.Simulation.Models;

using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicySimulationRecord(
    PolicyContext Context,
    List<PolicyDecision> Decisions,
    PolicyDecision FinalDecision
);
