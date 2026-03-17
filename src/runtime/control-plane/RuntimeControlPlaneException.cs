namespace Whycespace.Runtime.ControlPlane;

public sealed class RuntimeControlPlaneException : Exception
{
    public RuntimeControlPlaneException(string message)
        : base(message) { }

    public RuntimeControlPlaneException(string message, Exception innerException)
        : base(message, innerException) { }
}
