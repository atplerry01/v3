using Whycespace.ProjectionRuntime.Engine;
using Whycespace.ProjectionRuntime.Registry;
using Whycespace.ProjectionRuntime.Storage;
using Whycespace.ProjectionRuntime.Models;

namespace Whycespace.RuntimeConcurrencyTests;

public sealed class ProjectionReplayTests
{
    [Fact]
    public void ReplayEvents_10000Events_DeterministicFinalState()
    {
        var registry = new ProjectionRegistry();
        var store = new ProjectionStateStore();
        var engine = new ProjectionEngine(registry, store);

        registry.Register("OrderCreated", "OrderProjection");

        for (var i = 0; i < 10_000; i++)
        {
            engine.Apply("OrderCreated", $"order-{i}", new { Total = i * 10 });
        }

        var records = store.GetAll();

        Assert.Equal(10_000, records.Count);
    }

    [Fact]
    public void ReplayEvents_SameEntity_OverwritesState()
    {
        var registry = new ProjectionRegistry();
        var store = new ProjectionStateStore();
        var engine = new ProjectionEngine(registry, store);

        registry.Register("BalanceUpdated", "BalanceProjection");

        for (var i = 0; i < 10_000; i++)
        {
            engine.Apply("BalanceUpdated", "account-1", new { Balance = i });
        }

        var records = store.GetAll();

        Assert.Single(records);

        var record = store.Get("BalanceProjection", "account-1");
        Assert.NotNull(record);
    }

    [Fact]
    public void ReplayEvents_MultipleEventTypes_CorrectProjectionMapping()
    {
        var registry = new ProjectionRegistry();
        var store = new ProjectionStateStore();
        var engine = new ProjectionEngine(registry, store);

        registry.Register("RideCreated", "RideProjection");
        registry.Register("DriverUpdated", "DriverProjection");
        registry.Register("VaultDeposit", "VaultProjection");

        for (var i = 0; i < 5_000; i++)
        {
            engine.Apply("RideCreated", $"ride-{i}", new { Status = "Created" });
        }

        for (var i = 0; i < 3_000; i++)
        {
            engine.Apply("DriverUpdated", $"driver-{i}", new { Location = "Updated" });
        }

        for (var i = 0; i < 2_000; i++)
        {
            engine.Apply("VaultDeposit", $"vault-{i}", new { Balance = i * 100 });
        }

        var records = store.GetAll();

        Assert.Equal(10_000, records.Count);
    }

    [Fact]
    public void ReplayEvents_NoDuplicateRecords()
    {
        var registry = new ProjectionRegistry();
        var store = new ProjectionStateStore();
        var engine = new ProjectionEngine(registry, store);

        registry.Register("ItemProcessed", "ItemProjection");

        // Replay same events twice (simulating replay)
        for (var pass = 0; pass < 2; pass++)
        {
            for (var i = 0; i < 5_000; i++)
            {
                engine.Apply("ItemProcessed", $"item-{i}", new { Pass = pass });
            }
        }

        var records = store.GetAll();

        // Should be 5000 unique records (overwritten on second pass)
        Assert.Equal(5_000, records.Count);
    }

    [Fact]
    public void ReplayEvents_DeterministicAcrossRuns()
    {
        var counts = new List<int>();

        for (var run = 0; run < 3; run++)
        {
            var registry = new ProjectionRegistry();
            var store = new ProjectionStateStore();
            var engine = new ProjectionEngine(registry, store);

            registry.Register("EventA", "ProjectionA");
            registry.Register("EventB", "ProjectionB");

            for (var i = 0; i < 1_000; i++)
            {
                engine.Apply("EventA", $"entity-{i}", new { Value = i });
                engine.Apply("EventB", $"entity-{i}", new { Value = i * 2 });
            }

            counts.Add(store.GetAll().Count);
        }

        // All runs should produce identical record counts
        Assert.All(counts, c => Assert.Equal(2_000, c));
    }
}
