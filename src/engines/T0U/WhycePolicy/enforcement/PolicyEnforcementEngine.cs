namespace Whycespace.Engines.T0U.WhycePolicy.Enforcement;

using Whycespace.Engines.T0U.WhycePolicy.Evaluation;
using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

public sealed class PolicyEnforcementEngine
{
    private readonly PolicyEvaluationEngine _evaluationEngine;
    private readonly PolicyContextEngine _contextEngine;
    private readonly PolicyDecisionCacheEngine _cacheEngine;

    public PolicyEnforcementEngine(
        PolicyEvaluationEngine evaluationEngine,
        PolicyContextEngine contextEngine,
        PolicyDecisionCacheEngine cacheEngine)
    {
        _evaluationEngine = evaluationEngine;
        _contextEngine = contextEngine;
        _cacheEngine = cacheEngine;
    }

    public PolicyEnforcementResult EnforcePolicy(PolicyEnforcementRequest request)
    {
        var cacheKey = _cacheEngine.GenerateCacheKey(request.Domain, request.ActorId, request.Attributes);

        var cached = _cacheEngine.GetCachedDecision(cacheKey);
        if (cached is not null)
        {
            return BuildResult(cached);
        }

        var context = _contextEngine.BuildContext(
            Guid.Parse(request.ActorId),
            request.Domain,
            request.Attributes);

        var decisions = _evaluationEngine.EvaluatePolicies(request.Domain, context);

        _cacheEngine.StoreDecision(cacheKey, decisions);

        return BuildResult(decisions);
    }

    private static PolicyEnforcementResult BuildResult(IReadOnlyList<PolicyDecision> decisions)
    {
        var denied = decisions.Any(d => !d.Allowed && d.Action == "deny");

        var allowed = !denied;
        var reason = denied
            ? string.Join("; ", decisions.Where(d => !d.Allowed).Select(d => d.Reason))
            : "All policies passed";

        return new PolicyEnforcementResult(allowed, reason, decisions, DateTime.UtcNow);
    }
}
