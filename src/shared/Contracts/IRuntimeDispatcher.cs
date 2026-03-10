namespace Whycespace.Shared.Contracts;

public interface IRuntimeDispatcher
{
    Task<EngineResult> DispatchAsync(EngineInvocationEnvelope envelope);
}
