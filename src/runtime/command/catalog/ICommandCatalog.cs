namespace Whycespace.CommandSystem.Catalog;

public interface ICommandCatalog
{
    void Register(string commandType, Type handlerType);
    Type? Resolve(string commandType);
    IReadOnlyCollection<string> GetRegisteredCommands();
}
