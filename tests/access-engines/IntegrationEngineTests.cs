namespace Whycespace.AccessEngines.Tests;

using Whycespace.Engines.T4A_Access;
using Whycespace.Contracts.Engines;

public sealed class IntegrationEngineTests
{
    private readonly IntegrationEngine _engine = new();

    [Fact]
    public async Task HandlesPaymentIntegration_Successfully()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Integration",
            "partition-1", new Dictionary<string, object>
            {
                ["provider"] = "stripe",
                ["integrationAction"] = "charge.create",
                ["callerId"] = "system",
                ["amount"] = 100.00m,
                ["currency"] = "GBP"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("IntegrationPaymentRequested", result.Events[0].EventType);
        Assert.Equal(true, result.Output["dispatched"]);
    }

    [Fact]
    public async Task Fails_WhenUnknownProvider()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Integration",
            "partition-1", new Dictionary<string, object>
            {
                ["provider"] = "unknown-provider",
                ["integrationAction"] = "some.action"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Fails_WhenInvalidActionForProvider()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Integration",
            "partition-1", new Dictionary<string, object>
            {
                ["provider"] = "sms",
                ["integrationAction"] = "invalid.action"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task HandlesSmsIntegration_WithRecipient()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Integration",
            "partition-1", new Dictionary<string, object>
            {
                ["provider"] = "sms",
                ["integrationAction"] = "sms.send",
                ["recipient"] = "+441234567890",
                ["message"] = "Your ride is arriving"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("IntegrationSmsRequested", result.Events[0].EventType);
    }
}
