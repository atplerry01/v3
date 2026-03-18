namespace Whycespace.AtlasSimulation.Tests;

using Whycespace.Domain.Economic.Events;
using Whycespace.Simulation;

public sealed class SimulationEventGeneratorTests
{
    [Fact]
    public void GenerateCapitalContribution_CreatesValidEnvelope()
    {
        var spvId = Guid.NewGuid();
        var envelope = SimulationEventGenerator.GenerateCapitalContribution(spvId, 5_000m);

        Assert.Equal("whyce.economic.capital-contribution-recorded", envelope.EventType);
        Assert.Equal("whyce.economic.capital", envelope.Topic);
        Assert.NotEqual(Guid.Empty, envelope.EventId);
        Assert.IsType<CapitalContributionRecordedEvent>(envelope.Payload);

        var payload = (CapitalContributionRecordedEvent)envelope.Payload;
        Assert.Equal(spvId, payload.SpvId);
        Assert.Equal(5_000m, payload.Amount);
    }

    [Fact]
    public void GenerateCapitalDistribution_CreatesValidEnvelope()
    {
        var envelope = SimulationEventGenerator.GenerateCapitalDistribution(amount: 2_500m);

        Assert.Equal("whyce.economic.capital-distributed", envelope.EventType);
        Assert.IsType<CapitalDistributedEvent>(envelope.Payload);
        Assert.Equal(2_500m, ((CapitalDistributedEvent)envelope.Payload).TotalAmount);
    }

    [Fact]
    public void GenerateRevenueRecorded_CreatesValidEnvelope()
    {
        var envelope = SimulationEventGenerator.GenerateRevenueRecorded(amount: 1_000m);

        Assert.Equal("whyce.economic.revenue-recorded", envelope.EventType);
        Assert.IsType<RevenueRecordedEvent>(envelope.Payload);
        Assert.Equal(1_000m, ((RevenueRecordedEvent)envelope.Payload).Amount);
    }

    [Fact]
    public void GenerateIdentityRegistered_CreatesValidEnvelope()
    {
        var id = Guid.NewGuid();
        var envelope = SimulationEventGenerator.GenerateIdentityRegistered(id);

        Assert.Equal("whyce.identity.registered", envelope.EventType);
        var payload = Assert.IsType<Dictionary<string, object>>(envelope.Payload);
        Assert.Equal(id, payload["IdentityId"]);
        Assert.True(payload.ContainsKey("DisplayName"));
        Assert.True(payload.ContainsKey("Email"));
    }

    [Fact]
    public void GenerateTaskAssigned_CreatesValidEnvelope()
    {
        var workerId = Guid.NewGuid();
        var envelope = SimulationEventGenerator.GenerateTaskAssigned(workerId);

        Assert.Equal("whyce.heos.task-assigned", envelope.EventType);
        var payload = Assert.IsType<Dictionary<string, object>>(envelope.Payload);
        Assert.Equal(workerId, payload["WorkerId"]);
    }

    [Fact]
    public void GenerateEconomicLifecycle_StartsWithContribution()
    {
        var spvId = Guid.NewGuid();
        var events = SimulationEventGenerator.GenerateEconomicLifecycle(spvId, 5);

        Assert.Equal(5, events.Count);
        Assert.Equal("whyce.economic.capital-contribution-recorded", events[0].EventType);
    }

    [Fact]
    public void GenerateRandom_ProducesValidEvents()
    {
        for (var i = 0; i < 50; i++)
        {
            var envelope = SimulationEventGenerator.GenerateRandom();
            Assert.NotEqual(Guid.Empty, envelope.EventId);
            Assert.False(string.IsNullOrEmpty(envelope.EventType));
            Assert.False(string.IsNullOrEmpty(envelope.Topic));
            Assert.NotNull(envelope.Payload);
        }
    }

    [Fact]
    public void GenerateRandom_ProducesUniqueEventIds()
    {
        var ids = Enumerable.Range(0, 100)
            .Select(_ => SimulationEventGenerator.GenerateRandom().EventId)
            .ToHashSet();

        Assert.Equal(100, ids.Count);
    }
}
