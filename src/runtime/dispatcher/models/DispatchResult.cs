namespace Whycespace.RuntimeDispatcher.Models;

using Whycespace.Contracts.Events;

public sealed record DispatchResult(
    bool Success,
    string WorkflowName,
    IReadOnlyCollection<IEvent> Events
);
