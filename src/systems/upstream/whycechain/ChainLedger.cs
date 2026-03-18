namespace Whycespace.Systems.Upstream.WhyceChain;

public sealed record ChainEntry(
    Guid EntryId,
    string EntryType,
    string Hash,
    string PreviousHash,
    IReadOnlyDictionary<string, object> Data,
    DateTimeOffset Timestamp
);

public sealed class ChainLedger
{
    private readonly List<ChainEntry> _entries = new();

    public ChainEntry Append(string entryType, IReadOnlyDictionary<string, object> data)
    {
        var previousHash = _entries.Count > 0 ? _entries[^1].Hash : "genesis";
        var hash = ComputeHash(entryType, previousHash, data);

        var entry = new ChainEntry(Guid.NewGuid(), entryType, hash, previousHash, data, DateTimeOffset.UtcNow);
        _entries.Add(entry);
        return entry;
    }

    public IReadOnlyList<ChainEntry> GetEntries() => _entries;

    public bool Verify()
    {
        for (var i = 1; i < _entries.Count; i++)
        {
            if (_entries[i].PreviousHash != _entries[i - 1].Hash)
                return false;
        }
        return true;
    }

    private static string ComputeHash(string entryType, string previousHash, IReadOnlyDictionary<string, object> data)
    {
        var input = $"{entryType}:{previousHash}:{data.Count}";
        return Convert.ToBase64String(
            global::System.Security.Cryptography.SHA256.HashData(
                global::System.Text.Encoding.UTF8.GetBytes(input)));
    }
}
