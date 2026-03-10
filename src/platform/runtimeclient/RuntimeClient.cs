namespace Whycespace.Platform.RuntimeClient;

using Whycespace.Shared.Contracts;

public sealed class RuntimeClient : IRuntimeDispatcher
{
    private readonly IRuntimeDispatcher _dispatcher;

    public RuntimeClient(IRuntimeDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public Task<EngineResult> DispatchAsync(EngineInvocationEnvelope envelope)
    {
        return _dispatcher.DispatchAsync(envelope);
    }
}
