namespace Whycespace.CommandSystem.Registry;

public sealed class CommandRegistryException : Exception
{
    public string? CommandId { get; }
    public IReadOnlyList<string> ValidationErrors { get; }

    public CommandRegistryException(string message, string? commandId = null)
        : base(message)
    {
        CommandId = commandId;
        ValidationErrors = [];
    }

    public CommandRegistryException(string commandId, IReadOnlyCollection<string> errors)
        : base($"Command '{commandId}' failed validation with {errors.Count} error(s): {string.Join("; ", errors)}")
    {
        CommandId = commandId;
        ValidationErrors = errors.ToList().AsReadOnly();
    }

    public CommandRegistryException(string message, Exception innerException)
        : base(message, innerException)
    {
        ValidationErrors = [];
    }
}
