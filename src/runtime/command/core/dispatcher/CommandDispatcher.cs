namespace Whycespace.CommandSystem.Core.Dispatcher;

using Whycespace.CommandSystem.Core.Models;
using Whycespace.Contracts.Runtime;

public sealed class CommandDispatcher
{
    private readonly Func<CommandEnvelope, CancellationToken, Task<ExecutionResult>> _dispatchAsync;

    public CommandDispatcher(
        Func<CommandEnvelope, CancellationToken, Task<ExecutionResult>> dispatchAsync)
    {
        _dispatchAsync = dispatchAsync;
    }

    public async Task<ExecutionResult> DispatchAsync(
        CommandEnvelope command,
        CancellationToken cancellationToken = default)
    {
        return await _dispatchAsync(command, cancellationToken);
    }
}
