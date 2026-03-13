using System.Collections.Concurrent;

namespace Whycespace.Projections.Storage;

public sealed class RedisProjectionStore : IProjectionStore
{
    private readonly ConcurrentDictionary<string, string> _store = new();

    public Task SetAsync(string key, string value)
    {
        _store[key] = value;
        return Task.CompletedTask;
    }

    public Task<string?> GetAsync(string key)
    {
        _store.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }

    public Task DeleteAsync(string key)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
