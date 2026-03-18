namespace Whycespace.Systems.Upstream.WhyceChain.Stores;

using global::System.Collections.Concurrent;

public sealed class ChainIndexStore
{
    private readonly ConcurrentDictionary<string, List<string>> _byEventType = new();
    private readonly ConcurrentDictionary<long, List<string>> _byBlockNumber = new();
    private readonly ConcurrentDictionary<long, List<string>> _byTimestampBucket = new();

    public void IndexEntry(string entryId, string eventType, long blockNumber, DateTimeOffset timestamp)
    {
        var bucket = timestamp.ToUnixTimeSeconds() / 3600;

        _byEventType.AddOrUpdate(eventType, [entryId], (_, list) => { list.Add(entryId); return list; });
        _byBlockNumber.AddOrUpdate(blockNumber, [entryId], (_, list) => { list.Add(entryId); return list; });
        _byTimestampBucket.AddOrUpdate(bucket, [entryId], (_, list) => { list.Add(entryId); return list; });
    }

    public IReadOnlyList<string> GetByEventType(string eventType)
    {
        return _byEventType.TryGetValue(eventType, out var list) ? list : [];
    }

    public IReadOnlyList<string> GetByBlockNumber(long blockNumber)
    {
        return _byBlockNumber.TryGetValue(blockNumber, out var list) ? list : [];
    }

    public IReadOnlyList<string> GetByTimestampRange(DateTimeOffset from, DateTimeOffset to)
    {
        var fromBucket = from.ToUnixTimeSeconds() / 3600;
        var toBucket = to.ToUnixTimeSeconds() / 3600;

        return _byTimestampBucket
            .Where(kvp => kvp.Key >= fromBucket && kvp.Key <= toBucket)
            .SelectMany(kvp => kvp.Value)
            .ToList();
    }
}
