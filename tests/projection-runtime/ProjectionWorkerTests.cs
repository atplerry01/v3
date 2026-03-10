using Whycespace.ProjectionRuntime.Engine;
using Whycespace.ProjectionRuntime.Registry;
using Whycespace.ProjectionRuntime.Storage;
using Whycespace.ProjectionRuntime.Workers;

namespace Whycespace.ProjectionRuntime.Tests;

public sealed class ProjectionWorkerTests
{
    [Fact]
    public void Handle_DelegatesToEngine()
    {
        var registry = new ProjectionRegistry();
        var store = new ProjectionStateStore();
        var engine = new ProjectionEngine(registry, store);
        var worker = new ProjectionWorker(engine);

        registry.Register("CapitalContributionRecorded", "SPVCapitalProjection");

        worker.Handle("CapitalContributionRecorded", "entity-1", new { Amount = 500 });

        var record = store.Get("SPVCapitalProjection", "entity-1");
        Assert.NotNull(record);
        Assert.Equal("SPVCapitalProjection", record.ProjectionName);
    }
}
