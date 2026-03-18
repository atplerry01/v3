namespace Whycespace.RuntimeDispatcher.Dispatcher;

using Whycespace.CommandSystem.Core.Models;
using Whycespace.Contracts.Runtime;

public interface IRuntimeDispatcher
{
    Task<ExecutionResult> DispatchAsync(
        CommandEnvelope command,
        CancellationToken cancellationToken);
}
