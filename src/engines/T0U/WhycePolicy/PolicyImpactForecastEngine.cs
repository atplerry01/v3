namespace Whycespace.Engines.T0U.WhycePolicy;

using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

public sealed class PolicyImpactForecastEngine
{
    private readonly PolicySimulationEngine _simulationEngine;

    public PolicyImpactForecastEngine(PolicyRegistryStore registryStore, PolicyDependencyStore dependencyStore)
    {
        _simulationEngine = new PolicySimulationEngine(registryStore, dependencyStore);
    }

    public PolicyImpactForecast ForecastImpact(PolicyImpactForecastRequest request)
    {
        var allowedCount = 0;
        var deniedCount = 0;
        var loggedCount = 0;

        foreach (var simRequest in request.SimulationContexts)
        {
            var result = _simulationEngine.SimulatePolicyEvaluation(simRequest);

            foreach (var decision in result.Decisions)
            {
                switch (decision.Action)
                {
                    case "allow":
                        allowedCount++;
                        break;
                    case "deny":
                        deniedCount++;
                        break;
                    case "skip":
                        break;
                    default:
                        loggedCount++;
                        break;
                }
            }
        }

        return new PolicyImpactForecast(
            request.Domain,
            request.Domain,
            request.SimulationContexts.Count,
            allowedCount,
            deniedCount,
            loggedCount,
            DateTime.UtcNow
        );
    }
}
