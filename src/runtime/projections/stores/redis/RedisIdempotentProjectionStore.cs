using System.Collections.Concurrent;
using Whycespace.Projections.Contracts;

namespace Whycespace.Projections.Stores.Redis;

public sealed class RedisIdempotentProjectionStore : IIdempotentProjectionStore
{
    private readonly ConcurrentDictionary<string, string> _store = new();
    private readonly ConcurrentDictionary<Guid, ProcessedEventRecord> _processedEvents = new();

    public Task<bool> HasProcessedAsync(Guid eventId)
    {
        return Task.FromResult(_processedEvents.ContainsKey(eventId));
    }

    public Task MarkProcessedAsync(Guid eventId, string aggregateId, long sequenceNumber)
    {
        _processedEvents[eventId] = new ProcessedEventRecord(eventId, aggregateId, sequenceNumber, DateTime.UtcNow);
        return Task.CompletedTask;
    }

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

    private sealed record ProcessedEventRecord(
        Guid EventId,
        string AggregateId,
        long SequenceNumber,
        DateTime ProcessedUtc
    );
}
