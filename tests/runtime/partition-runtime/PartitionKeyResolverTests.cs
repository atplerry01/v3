namespace Whycespace.PartitionRuntime.Tests;

using Whycespace.CommandSystem.Core.Models;
using Whycespace.PartitionRuntime.Resolver;

public class PartitionKeyResolverTests
{
    private readonly PartitionKeyResolver _resolver = new();

    [Fact]
    public void Resolve_WithRiderId_ReturnsRiderPartitionKey()
    {
        var envelope = new CommandEnvelope(
            Guid.NewGuid(),
            "RequestRideCommand",
            new Dictionary<string, object> { ["RiderId"] = "rider-42" },
            DateTimeOffset.UtcNow);

        var key = _resolver.Resolve(envelope);

        Assert.Equal("rider-42", key.Value);
    }

    [Fact]
    public void Resolve_WithPropertyId_ReturnsPropertyPartitionKey()
    {
        var envelope = new CommandEnvelope(
            Guid.NewGuid(),
            "ListPropertyCommand",
            new Dictionary<string, object> { ["PropertyId"] = "prop-99" },
            DateTimeOffset.UtcNow);

        var key = _resolver.Resolve(envelope);

        Assert.Equal("prop-99", key.Value);
    }

    [Fact]
    public void Resolve_WithSPVId_ReturnsSPVPartitionKey()
    {
        var envelope = new CommandEnvelope(
            Guid.NewGuid(),
            "AllocateCapitalCommand",
            new Dictionary<string, object> { ["SPVId"] = "spv-7" },
            DateTimeOffset.UtcNow);

        var key = _resolver.Resolve(envelope);

        Assert.Equal("spv-7", key.Value);
    }

    [Fact]
    public void Resolve_WithNoAggregateKey_FallsBackToCommandId()
    {
        var commandId = Guid.NewGuid();
        var envelope = new CommandEnvelope(
            commandId,
            "UnknownCommand",
            new Dictionary<string, object> { ["userId"] = "user-1" },
            DateTimeOffset.UtcNow);

        var key = _resolver.Resolve(envelope);

        Assert.Equal(commandId.ToString(), key.Value);
    }
}
