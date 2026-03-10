namespace Whycespace.Runtime.Reliability;

public sealed class IdempotencyRegistry
{
    private readonly HashSet<Guid> _processedIds = new();

    public bool IsProcessed(Guid invocationId) => _processedIds.Contains(invocationId);

    public void MarkProcessed(Guid invocationId) => _processedIds.Add(invocationId);
}
