namespace Whycespace.Platform.Gateway.WhyceApiGateway;

using Whycespace.Engines.T0U.WhycePolicy;
using Whycespace.System.Upstream.WhycePolicy.Models;
using Whycespace.System.Upstream.WhycePolicy.Stores;

public sealed class PolicyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly PolicyRegistryStore _registryStore;
    private readonly PolicyDependencyStore _dependencyStore;
    private readonly PolicyContextStore _contextStore;
    private readonly PolicyDecisionCacheStore _cacheStore;

    public PolicyMiddleware(
        RequestDelegate next,
        PolicyRegistryStore registryStore,
        PolicyDependencyStore dependencyStore,
        PolicyContextStore contextStore,
        PolicyDecisionCacheStore cacheStore)
    {
        _next = next;
        _registryStore = registryStore;
        _dependencyStore = dependencyStore;
        _contextStore = contextStore;
        _cacheStore = cacheStore;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var actorId = context.Request.Headers["X-Actor-Id"].FirstOrDefault();
        var domain = context.Request.Headers["X-Policy-Domain"].FirstOrDefault();

        if (string.IsNullOrEmpty(actorId) || string.IsNullOrEmpty(domain))
        {
            await _next(context);
            return;
        }

        var attributes = new Dictionary<string, string>();
        foreach (var header in context.Request.Headers)
        {
            if (header.Key.StartsWith("X-Policy-Attr-", StringComparison.OrdinalIgnoreCase))
            {
                var key = header.Key["X-Policy-Attr-".Length..].ToLowerInvariant();
                attributes[key] = header.Value.ToString();
            }
        }

        var request = new PolicyEnforcementRequest(
            actorId,
            domain,
            context.Request.Path.Value ?? string.Empty,
            attributes);

        var evaluationEngine = new PolicyEvaluationEngine(_registryStore, _dependencyStore);
        var contextEngine = new PolicyContextEngine(_contextStore);
        var cacheEngine = new PolicyDecisionCacheEngine(_cacheStore);
        var enforcementEngine = new PolicyEnforcementEngine(evaluationEngine, contextEngine, cacheEngine);

        var result = enforcementEngine.EnforcePolicy(request);

        if (!result.Allowed)
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                allowed = result.Allowed,
                reason = result.Reason,
                evaluatedAt = result.EvaluatedAt
            });
            return;
        }

        await _next(context);
    }
}
