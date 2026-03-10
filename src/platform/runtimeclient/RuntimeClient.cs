namespace Whycespace.Platform.RuntimeClient;

using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Runtime;

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
