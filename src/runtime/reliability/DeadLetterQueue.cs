namespace Whycespace.Runtime.Reliability;

using Whycespace.Contracts.Engines;

public sealed record DeadLetterEntry(
    EngineInvocationEnvelope Envelope,
    string Reason,
    DateTimeOffset FailedAt
);

public sealed class DeadLetterQueue
{
    private readonly List<DeadLetterEntry> _entries = new();

    public void Enqueue(EngineInvocationEnvelope envelope, string reason)
    {
        _entries.Add(new DeadLetterEntry(envelope, reason, DateTimeOffset.UtcNow));
    }

    public IReadOnlyList<DeadLetterEntry> GetEntries() => _entries;

    public int Count => _entries.Count;
}
