namespace Whycespace.CommandSystem.Routing;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class CommandRouteAttribute : Attribute
{
    public string CommandId { get; }
    public string EngineId { get; }

    public CommandRouteAttribute(string commandId, string engineId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);
        ArgumentException.ThrowIfNullOrWhiteSpace(engineId);

        CommandId = commandId;
        EngineId = engineId;
    }
}
