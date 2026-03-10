namespace Whycespace.CommandSystem.Dispatcher;

using Whycespace.CommandSystem.Idempotency;
using Whycespace.CommandSystem.Models;
using Whycespace.CommandSystem.Routing;
using Whycespace.CommandSystem.Validation;
using Whycespace.Contracts.Runtime;

public sealed class CommandDispatcher
{
    private readonly ICommandValidator _validator;
    private readonly IIdempotencyRegistry _idempotency;
    private readonly ICommandRouter _router;

    public CommandDispatcher(
        ICommandValidator validator,
        IIdempotencyRegistry idempotency,
        ICommandRouter router)
    {
        _validator = validator;
        _idempotency = idempotency;
        _router = router;
    }

    public WorkflowExecutionRequest Dispatch(CommandEnvelope command)
    {
        // 1. Validate
        _validator.Validate(command);

        // 2. Check idempotency
        if (_idempotency.Exists(command.CommandId))
            throw new InvalidOperationException($"Duplicate command: {command.CommandId}");

        // 3. Register command
        _idempotency.Register(command.CommandId);

        // 4. Resolve workflow
        var workflowName = _router.ResolveWorkflow(command.CommandType)
            ?? throw new InvalidOperationException($"No workflow mapped for command type: {command.CommandType}");

        // 5. Return WorkflowExecutionRequest
        return new WorkflowExecutionRequest(
            WorkflowName: workflowName,
            Context: command.Payload,
            CorrelationId: command.CommandId.ToString(),
            ScheduledAt: null
        );
    }
}
