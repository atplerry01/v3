namespace Whycespace.Runtime.Persistence.Abstractions;

using Whycespace.Engines.T1M.Shared;
using Whycespace.Runtime.Persistence.Workflow;

public interface IWorkflowStateStore
{
    void PersistWorkflowState(WorkflowStateRecord record);

    void UpdateStepState(string instanceId, string stepId, StepStatus status);

    WorkflowStateRecord? GetWorkflowState(string instanceId);

    IReadOnlyList<WorkflowStepState> ListPendingSteps(string instanceId);

    IReadOnlyList<WorkflowStepState> ListCompletedSteps(string instanceId);
}
