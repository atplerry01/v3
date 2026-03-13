using Whycespace.Projections.Queries;
using Whycespace.Projections.Storage;

namespace Whycespace.Projections.Tests;

public sealed class ProjectionQueryServiceTests
{
    [Fact]
    public async Task GetAsync_ReturnsStoredValue()
    {
        var store = new RedisProjectionStore();
        await store.SetAsync("driver:123", "{\"lat\":51.5}");

        var queryService = new ProjectionQueryService(store);

        var result = await queryService.GetAsync("driver:123");

        Assert.Equal("{\"lat\":51.5}", result);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenKeyNotFound()
    {
        var store = new RedisProjectionStore();
        var queryService = new ProjectionQueryService(store);

        var result = await queryService.GetAsync("nonexistent");

        Assert.Null(result);
    }
}
