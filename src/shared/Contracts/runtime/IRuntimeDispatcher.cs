namespace Whycespace.Contracts.Runtime;

using Whycespace.Contracts.Engines;

public interface IEngineRuntimeDispatcher
{
    Task<EngineResult> DispatchAsync(EngineInvocationEnvelope envelope);
}
