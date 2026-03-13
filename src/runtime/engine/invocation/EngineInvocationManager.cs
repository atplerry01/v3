namespace Whycespace.EngineRuntime.Invocation;

using Whycespace.Contracts.Engines;

public sealed class EngineInvocationManager
{
    public async Task<EngineResult> InvokeAsync(
        IEngine engine,
        EngineContext context,
        CancellationToken cancellationToken = default)
    {
        return await engine.ExecuteAsync(context);
    }
}
