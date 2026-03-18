namespace Whycespace.Engines.T1M.Shared;

/// <summary>
/// Domain model representing a timeout policy for a workflow step or workflow instance.
/// Defines the allowed execution duration and the timeout strategy.
/// </summary>
public sealed record WorkflowTimeoutPolicy(
    TimeSpan TimeoutDuration,
    TimeoutStrategy TimeoutStrategy
);

/// <summary>
/// Determines whether the timeout applies to an individual step or the entire workflow.
/// </summary>
public enum TimeoutStrategy
{
    StepTimeout,
    WorkflowTimeout
}