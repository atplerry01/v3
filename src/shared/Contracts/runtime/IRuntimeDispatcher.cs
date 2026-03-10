namespace Whycespace.Contracts.Runtime;

using Whycespace.Contracts.Engines;

public interface IRuntimeDispatcher
{
    Task<EngineResult> DispatchAsync(EngineInvocationEnvelope envelope);
}
