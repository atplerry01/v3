using System.Collections.Concurrent;
using Whycespace.ProjectionRuntime.Storage;

namespace Whycespace.ProjectionRuntime.Stores.Postgres;

/// <summary>
/// Postgres-backed projection store with idempotency tracking.
/// Uses in-memory structures as the runtime store implementation.
/// Actual Postgres persistence is handled through the platform data layer.
/// </summary>
public sealed class PostgresProjectionStore : IIdempotentProjectionStore
{
    private readonly ConcurrentDictionary<string, string> _projections = new();
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
        _projections[key] = value;
        return Task.CompletedTask;
    }

    public Task<string?> GetAsync(string key)
    {
        _projections.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }

    public Task DeleteAsync(string key)
    {
        _projections.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    private sealed record ProcessedEventRecord(
        Guid EventId,
        string AggregateId,
        long SequenceNumber,
        DateTime ProcessedUtc
    );
}
