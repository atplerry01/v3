namespace Whycespace.CommandSystem.Routing;

public sealed class CommandRoutingException : Exception
{
    public string? CommandId { get; }

    public CommandRoutingException(string message, string? commandId = null)
        : base(message)
    {
        CommandId = commandId;
    }

    public CommandRoutingException(string message, string? commandId, Exception innerException)
        : base(message, innerException)
    {
        CommandId = commandId;
    }
}
