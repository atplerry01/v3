using Whycespace.Engines.T0U.WhycePolicy;
using Whycespace.Systems.Upstream.WhycePolicy.Models;
using Whycespace.Systems.Upstream.WhycePolicy.Stores;

namespace Whycespace.WhycePolicy.Dsl.Tests;

public class PolicyImpactForecastTests
{
    private readonly PolicyRegistryStore _registryStore = new();
    private readonly PolicyDependencyStore _dependencyStore = new();
    private readonly PolicyImpactForecastEngine _engine;

    public PolicyImpactForecastTests()
    {
        _engine = new PolicyImpactForecastEngine(_registryStore, _dependencyStore);
    }

    private void RegisterPolicy(string id, string domain, List<PolicyCondition> conditions, List<PolicyAction> actions)
    {
        var definition = new PolicyDefinition(id, $"Policy {id}", 1, domain, conditions, actions, DateTime.UtcNow);
        var record = new PolicyRecord(id, 1, definition, PolicyStatus.Active, DateTime.UtcNow);
        _registryStore.Register(record);
    }

    private static PolicySimulationRequest Sim(string domain, string actorId, Dictionary<string, string> attrs)
    {
        return new PolicySimulationRequest(domain, actorId, attrs);
    }

    [Fact]
    public void Forecast_AllowOnlyPolicies_CountsAllowed()
    {
        RegisterPolicy("fc-allow", "identity",
            new List<PolicyCondition> { new("trust_score", "greater_than", "50") },
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) });

        var request = new PolicyImpactForecastRequest("identity", new List<PolicySimulationRequest>
        {
            Sim("identity", Guid.NewGuid().ToString(), new Dictionary<string, string> { ["trust_score"] = "75" }),
            Sim("identity", Guid.NewGuid().ToString(), new Dictionary<string, string> { ["trust_score"] = "90" })
        });

        var forecast = _engine.ForecastImpact(request);

        Assert.Equal(2, forecast.AllowedCount);
        Assert.Equal(0, forecast.DeniedCount);
        Assert.Equal(2, forecast.SimulatedContexts);
    }

    [Fact]
    public void Forecast_DenyOnlyPolicies_CountsDenied()
    {
        RegisterPolicy("fc-deny", "identity",
            new List<PolicyCondition> { new("trust_score", "less_than", "50") },
            new List<PolicyAction> { new("deny", new Dictionary<string, string>()) });

        var request = new PolicyImpactForecastRequest("identity", new List<PolicySimulationRequest>
        {
            Sim("identity", Guid.NewGuid().ToString(), new Dictionary<string, string> { ["trust_score"] = "30" }),
            Sim("identity", Guid.NewGuid().ToString(), new Dictionary<string, string> { ["trust_score"] = "20" })
        });

        var forecast = _engine.ForecastImpact(request);

        Assert.Equal(0, forecast.AllowedCount);
        Assert.Equal(2, forecast.DeniedCount);
    }

    [Fact]
    public void Forecast_MixedAllowDeny_CountsBoth()
    {
        RegisterPolicy("fc-mix", "identity",
            new List<PolicyCondition> { new("trust_score", "greater_than", "50") },
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) });

        var request = new PolicyImpactForecastRequest("identity", new List<PolicySimulationRequest>
        {
            Sim("identity", Guid.NewGuid().ToString(), new Dictionary<string, string> { ["trust_score"] = "75" }),
            Sim("identity", Guid.NewGuid().ToString(), new Dictionary<string, string> { ["trust_score"] = "30" })
        });

        var forecast = _engine.ForecastImpact(request);

        Assert.Equal(1, forecast.AllowedCount);
        Assert.Equal(0, forecast.DeniedCount);
        Assert.Equal(2, forecast.SimulatedContexts);
    }

    [Fact]
    public void Forecast_MultipleContexts_Aggregated()
    {
        RegisterPolicy("fc-agg-1", "identity",
            new List<PolicyCondition> { new("trust_score", "greater_than", "50") },
            new List<PolicyAction> { new("allow", new Dictionary<string, string>()) });

        RegisterPolicy("fc-agg-2", "identity",
            new List<PolicyCondition> { new("trust_score", "less_than", "100") },
            new List<PolicyAction> { new("log", new Dictionary<string, string>()) });

        var request = new PolicyImpactForecastRequest("identity", new List<PolicySimulationRequest>
        {
            Sim("identity", Guid.NewGuid().ToString(), new Dictionary<string, string> { ["trust_score"] = "75" }),
            Sim("identity", Guid.NewGuid().ToString(), new Dictionary<string, string> { ["trust_score"] = "80" }),
            Sim("identity", Guid.NewGuid().ToString(), new Dictionary<string, string> { ["trust_score"] = "90" })
        });

        var forecast = _engine.ForecastImpact(request);

        Assert.Equal(3, forecast.SimulatedContexts);
        Assert.Equal(3, forecast.AllowedCount);
        Assert.Equal(3, forecast.LoggedCount);
    }

    [Fact]
    public void Forecast_EmptySimulationList_ReturnsZeroCounts()
    {
        var request = new PolicyImpactForecastRequest("identity", new List<PolicySimulationRequest>());

        var forecast = _engine.ForecastImpact(request);

        Assert.Equal(0, forecast.SimulatedContexts);
        Assert.Equal(0, forecast.AllowedCount);
        Assert.Equal(0, forecast.DeniedCount);
        Assert.Equal(0, forecast.LoggedCount);
    }

    [Fact]
    public void Forecast_TimestampRecorded()
    {
        var before = DateTime.UtcNow;

        var request = new PolicyImpactForecastRequest("identity", new List<PolicySimulationRequest>());

        var forecast = _engine.ForecastImpact(request);

        Assert.True(forecast.GeneratedAt >= before);
        Assert.True(forecast.GeneratedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Forecast_SimulationEngineIntegration_Works()
    {
        RegisterPolicy("fc-int", "identity",
            new List<PolicyCondition> { new("trust_score", "less_than", "50") },
            new List<PolicyAction> { new("deny", new Dictionary<string, string>()) });

        var actorId = Guid.NewGuid().ToString();
        var request = new PolicyImpactForecastRequest("identity", new List<PolicySimulationRequest>
        {
            Sim("identity", actorId, new Dictionary<string, string> { ["trust_score"] = "30" })
        });

        var forecast = _engine.ForecastImpact(request);

        Assert.Equal(1, forecast.DeniedCount);
        Assert.Equal("identity", forecast.Domain);
    }
}
