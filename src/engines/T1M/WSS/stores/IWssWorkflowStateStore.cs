namespace Whycespace.Engines.T1M.WSS.Stores;

using Whycespace.System.Midstream.WSS.Models;

public interface IWssWorkflowStateStore
{
    void SaveState(WorkflowState state);
    WorkflowState GetState(string instanceId);
    WorkflowState UpdateState(string instanceId, string currentStep, WorkflowInstanceStatus status);
    WorkflowState AddCompletedStep(string instanceId, string stepId);
    void DeleteState(string instanceId);
    IReadOnlyList<WorkflowState> ListActiveStates();
}
