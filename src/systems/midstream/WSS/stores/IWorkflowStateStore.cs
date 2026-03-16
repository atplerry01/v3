namespace Whycespace.Systems.Midstream.WSS.Stores;

using Whycespace.Domain.Core.Workflows;

public interface IWorkflowStateStore
{
    void PersistWorkflowState(WorkflowStateRecord record);

    void UpdateStepState(string instanceId, string stepId, StepStatus status);

    WorkflowStateRecord? GetWorkflowState(string instanceId);

    IReadOnlyList<WorkflowStepState> ListPendingSteps(string instanceId);

    IReadOnlyList<WorkflowStepState> ListCompletedSteps(string instanceId);
}
