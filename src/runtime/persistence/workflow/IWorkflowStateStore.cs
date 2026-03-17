namespace Whycespace.Runtime.Persistence.Workflow;

using Whycespace.Engines.T1M.WSS.Workflows;

public interface IWorkflowStateStore
{
    void PersistWorkflowState(WorkflowStateRecord record);

    void UpdateStepState(string instanceId, string stepId, StepStatus status);

    WorkflowStateRecord? GetWorkflowState(string instanceId);

    IReadOnlyList<WorkflowStepState> ListPendingSteps(string instanceId);

    IReadOnlyList<WorkflowStepState> ListCompletedSteps(string instanceId);
}
