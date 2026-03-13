namespace Whycespace.RuntimeDispatcher.Dispatcher;

using Whycespace.CommandSystem.Models;
using Whycespace.Contracts.Runtime;

public interface IRuntimeDispatcher
{
    Task<ExecutionResult> DispatchAsync(
        CommandEnvelope command,
        CancellationToken cancellationToken);
}
