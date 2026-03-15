using Whycespace.ProjectionRuntime.Runtime;
using Whycespace.ProjectionRuntime.Registry;
using Whycespace.ProjectionRuntime.Storage;

namespace Whycespace.ProjectionRuntime.Tests;

public sealed class ProjectionEngineTests
{
    [Fact]
    public void Apply_CreatesRecordInStore()
    {
        var registry = new ProjectionRegistry();
        var store = new ProjectionStateStore();
        var engine = new ProjectionEngine(registry, store);

        registry.Register("CapitalContributionRecorded", "SPVCapitalProjection");

        engine.Apply("CapitalContributionRecorded", "entity-1", new { Amount = 500 });

        var record = store.Get("SPVCapitalProjection", "entity-1");
        Assert.NotNull(record);
        Assert.Equal("SPVCapitalProjection", record.ProjectionName);
        Assert.Equal("entity-1", record.EntityId);
    }

    [Fact]
    public void Apply_UpdatesExistingRecord()
    {
        var registry = new ProjectionRegistry();
        var store = new ProjectionStateStore();
        var engine = new ProjectionEngine(registry, store);

        registry.Register("RevenueRecorded", "RevenueProjection");

        engine.Apply("RevenueRecorded", "entity-1", new { Amount = 100 });
        engine.Apply("RevenueRecorded", "entity-1", new { Amount = 200 });

        var all = store.GetAll();
        Assert.Single(all);
    }
}
