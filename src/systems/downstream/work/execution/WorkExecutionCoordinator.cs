using Whycespace.Systems.Downstream.Work.Tasks;
using Whycespace.Systems.Downstream.Work.Events;
using Whycespace.Systems.Downstream.Work.Shared.Policy;

namespace Whycespace.Systems.Downstream.Work.Execution;

public sealed class WorkExecutionCoordinator
{
    private readonly TaskRegistry _taskRegistry;
    private readonly WorkPolicyAdapter _policyAdapter;
    private readonly WorkEventAdapter _eventAdapter;

    public WorkExecutionCoordinator(
        TaskRegistry taskRegistry,
        WorkPolicyAdapter policyAdapter,
        WorkEventAdapter eventAdapter)
    {
        _taskRegistry = taskRegistry;
        _policyAdapter = policyAdapter;
        _eventAdapter = eventAdapter;
    }

    public async Task<TaskAssignmentResult> AssignTaskAsync(TaskDefinition task, string workerId, string clusterId)
    {
        var decision = await _policyAdapter.EvaluateTaskAssignmentAsync(workerId, task.TaskType, clusterId);

        if (!decision.IsPermitted)
        {
            await _eventAdapter.PublishExecutionFailedAsync(task.TaskId, clusterId, workerId, task.TaskType, "Policy denied");
            return TaskAssignmentResult.Denied(WorkPolicyDecisionMapper.Map(decision).DenialReason ?? "Policy denied");
        }

        var assignment = new TaskAssignment(
            Guid.NewGuid(),
            task.TaskId,
            workerId,
            "Assigned",
            DateTimeOffset.UtcNow
        );

        await _eventAdapter.PublishExecutionStartedAsync(task.TaskId, clusterId, task.SubClusterId, workerId, task.TaskType);

        return TaskAssignmentResult.Assigned(assignment);
    }

    public IReadOnlyList<TaskDefinition> GetAvailableTasks(string clusterId)
        => _taskRegistry.GetTasksByCluster(clusterId);
}

public sealed record TaskAssignmentResult(bool IsAssigned, TaskAssignment? Assignment, string? DenialReason = null)
{
    public static TaskAssignmentResult Assigned(TaskAssignment assignment) => new(true, assignment);
    public static TaskAssignmentResult Denied(string reason) => new(false, null, reason);
}
