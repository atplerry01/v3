using Whycespace.ProjectionRebuild.Checkpoints;
using Whycespace.ProjectionRebuild.Models;

namespace Whycespace.ProjectionRebuild.Tests;

public class CheckpointStoreTests
{
    [Fact]
    public async Task SaveAndLoad_RoundTrips()
    {
        var store = new ProjectionCheckpointStore();
        var checkpoint = new ProjectionCheckpoint("test-projection", Guid.NewGuid(), DateTime.UtcNow);

        await store.SaveCheckpointAsync(checkpoint);
        var loaded = await store.LoadCheckpointAsync("test-projection");

        Assert.NotNull(loaded);
        Assert.Equal(checkpoint.ProjectionName, loaded!.ProjectionName);
        Assert.Equal(checkpoint.LastProcessedEventId, loaded.LastProcessedEventId);
    }

    [Fact]
    public async Task LoadCheckpointAsync_ReturnsNull_WhenNotFound()
    {
        var store = new ProjectionCheckpointStore();
        var result = await store.LoadCheckpointAsync("nonexistent");
        Assert.Null(result);
    }

    [Fact]
    public async Task ClearCheckpointAsync_RemovesCheckpoint()
    {
        var store = new ProjectionCheckpointStore();
        var checkpoint = new ProjectionCheckpoint("test-projection", Guid.NewGuid(), DateTime.UtcNow);

        await store.SaveCheckpointAsync(checkpoint);
        await store.ClearCheckpointAsync("test-projection");

        var result = await store.LoadCheckpointAsync("test-projection");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAll_ReturnsAllCheckpoints()
    {
        var store = new ProjectionCheckpointStore();
        await store.SaveCheckpointAsync(new ProjectionCheckpoint("a", Guid.NewGuid(), DateTime.UtcNow));
        await store.SaveCheckpointAsync(new ProjectionCheckpoint("b", Guid.NewGuid(), DateTime.UtcNow));

        var all = store.GetAll();
        Assert.Equal(2, all.Count);
    }
}
