namespace Whycespace.Runtime.EventFabricGovernance;

public sealed class EventFabricGovernanceException : Exception
{
    public EventFabricGovernanceException(string message)
        : base(message) { }

    public EventFabricGovernanceException(string message, Exception innerException)
        : base(message, innerException) { }
}
