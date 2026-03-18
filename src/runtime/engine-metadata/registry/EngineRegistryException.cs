namespace Whycespace.Runtime.EngineMetadata.Registry;

public sealed class EngineRegistryException : Exception
{
    public EngineRegistryException(string message)
        : base(message) { }

    public EngineRegistryException(string message, Exception innerException)
        : base(message, innerException) { }
}
