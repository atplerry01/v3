using System.Collections.Concurrent;

namespace Whycespace.Runtime.CommandRegistry;

public sealed class CommandIdempotencyPolicy
{
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _processedCommands = new();

    public void EnsureCommandNotProcessed(Guid commandId)
    {
        if (_processedCommands.ContainsKey(commandId))
            throw new CommandRegistryException(
                $"Command '{commandId}' has already been processed.",
                commandId.ToString());
    }

    public bool TryMarkProcessed(Guid commandId)
    {
        return _processedCommands.TryAdd(commandId, DateTimeOffset.UtcNow);
    }

    public void MarkProcessed(Guid commandId)
    {
        if (!TryMarkProcessed(commandId))
            throw new CommandRegistryException(
                $"Command '{commandId}' has already been processed.",
                commandId.ToString());
    }

    public bool HasBeenProcessed(Guid commandId)
    {
        return _processedCommands.ContainsKey(commandId);
    }

    public int ProcessedCount => _processedCommands.Count;
}
