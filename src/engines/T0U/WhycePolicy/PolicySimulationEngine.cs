namespace Whycespace.Engines.T0U.WhycePolicy;

using Whycespace.System.Upstream.WhycePolicy.Models;
using Whycespace.System.Upstream.WhycePolicy.Stores;

public sealed class PolicySimulationEngine
{
    private readonly PolicyEvaluationEngine _evaluationEngine;

    public PolicySimulationEngine(PolicyRegistryStore registryStore, PolicyDependencyStore dependencyStore)
    {
        _evaluationEngine = new PolicyEvaluationEngine(registryStore, dependencyStore);
    }

    public PolicySimulationResult SimulatePolicyEvaluation(PolicySimulationRequest request)
    {
        var context = new PolicyContext(
            Guid.NewGuid(),
            Guid.Parse(request.ActorId),
            request.Domain,
            request.Attributes,
            DateTime.UtcNow
        );

        var decisions = _evaluationEngine.EvaluatePolicies(request.Domain, context);

        return new PolicySimulationResult(
            request.Domain,
            request.ActorId,
            decisions,
            DateTime.UtcNow
        );
    }
}
