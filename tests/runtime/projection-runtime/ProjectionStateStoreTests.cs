using Whycespace.ProjectionRuntime.Models;
using Whycespace.ProjectionRuntime.Storage;

namespace Whycespace.ProjectionRuntime.Tests;

public sealed class ProjectionStateStoreTests
{
    [Fact]
    public void Save_StoresRecord()
    {
        var store = new ProjectionStateStore();
        var record = new ProjectionRecord("SPVCapitalProjection", "entity-1", new { Amount = 100 });

        store.Save(record);

        var result = store.Get("SPVCapitalProjection", "entity-1");
        Assert.NotNull(result);
        Assert.Equal("SPVCapitalProjection", result.ProjectionName);
        Assert.Equal("entity-1", result.EntityId);
    }

    [Fact]
    public void Get_ReturnsNull_WhenNotFound()
    {
        var store = new ProjectionStateStore();

        var result = store.Get("Unknown", "entity-1");

        Assert.Null(result);
    }

    [Fact]
    public void GetAll_ReturnsAllRecords()
    {
        var store = new ProjectionStateStore();
        store.Save(new ProjectionRecord("Projection1", "entity-1", new { }));
        store.Save(new ProjectionRecord("Projection2", "entity-2", new { }));

        var results = store.GetAll();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void Save_OverwritesExistingRecord()
    {
        var store = new ProjectionStateStore();
        store.Save(new ProjectionRecord("Projection1", "entity-1", new { Amount = 100 }));
        store.Save(new ProjectionRecord("Projection1", "entity-1", new { Amount = 200 }));

        var results = store.GetAll();
        Assert.Single(results);
    }
}
