namespace Whycespace.Platform.RuntimeClient;

using Whycespace.Contracts.Engines;
using Whycespace.Shared.Envelopes;
using Whycespace.Contracts.Runtime;

public sealed class RuntimeClient : IEngineRuntimeDispatcher
{
    private readonly IEngineRuntimeDispatcher _dispatcher;

    public RuntimeClient(IEngineRuntimeDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public Task<EngineResult> DispatchAsync(EngineInvocationEnvelope envelope)
    {
        return _dispatcher.DispatchAsync(envelope);
    }
}
