namespace Whycespace.ExecutionEngines.Tests;

using Whycespace.Engines.T2E.System.Providers;
using Whycespace.Contracts.Engines;

public sealed class ProviderRegistrationEngineTests
{
    private readonly ClusterProviderRegistrationEngine _engine = new();

    [Fact]
    public async Task ExecutesSuccessfully_WithValidInput()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "RegisterProvider",
            "partition-1", new Dictionary<string, object>
            {
                ["providerName"] = "AcmeDrivers",
                ["providerType"] = "DriverProvider"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("ProviderRegistered", result.Events[0].EventType);
        Assert.True(result.Output.ContainsKey("providerId"));
    }

    [Fact]
    public async Task ProducesDomainEvent_WithCorrectPayload()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "RegisterProvider",
            "partition-1", new Dictionary<string, object>
            {
                ["providerName"] = "CityProperties",
                ["providerType"] = "PropertyManager"
            });

        var result = await _engine.ExecuteAsync(context);

        var evt = result.Events[0];
        Assert.Equal("ProviderRegistered", evt.EventType);
        Assert.Equal("CityProperties", evt.Payload["providerName"]);
        Assert.Equal("PropertyManager", evt.Payload["providerType"]);
        Assert.Equal("whyce.providers.events", evt.Payload["topic"]);
    }

    [Fact]
    public async Task DeterministicExecution_SameStructure()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "RegisterProvider",
            "partition-1", new Dictionary<string, object>
            {
                ["providerName"] = "TestProvider",
                ["providerType"] = "EnergyOperator"
            });

        var result1 = await _engine.ExecuteAsync(context);
        var result2 = await _engine.ExecuteAsync(context);

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.Events.Count, result2.Events.Count);
        Assert.Equal(result1.Events[0].EventType, result2.Events[0].EventType);
    }

    [Fact]
    public async Task UnsupportedProviderType_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "RegisterProvider",
            "partition-1", new Dictionary<string, object>
            {
                ["providerName"] = "BadProvider",
                ["providerType"] = "UnknownType"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }
}
