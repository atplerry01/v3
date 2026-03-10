namespace Whycespace.CommandSystem.Catalog;

using System.Collections.Concurrent;

public sealed class CommandCatalog : ICommandCatalog
{
    private readonly ConcurrentDictionary<string, Type> _commands = new();

    public void Register(string commandType, Type handlerType)
    {
        _commands[commandType] = handlerType;
    }

    public Type? Resolve(string commandType)
    {
        _commands.TryGetValue(commandType, out var handlerType);
        return handlerType;
    }

    public IReadOnlyCollection<string> GetRegisteredCommands()
    {
        return _commands.Keys.ToList().AsReadOnly();
    }
}
