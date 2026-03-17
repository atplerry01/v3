namespace Whycespace.Runtime.Observability.Core;

public sealed class RuntimeObservabilityException : Exception
{
    public RuntimeObservabilityException(string message)
        : base(message) { }

    public RuntimeObservabilityException(string message, Exception innerException)
        : base(message, innerException) { }
}
