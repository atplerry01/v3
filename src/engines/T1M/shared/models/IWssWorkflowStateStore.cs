namespace Whycespace.Engines.T1M.Shared;

using Whycespace.Systems.Midstream.WSS.Models;

public interface IWssWorkflowStateStore
{
    void SaveState(WorkflowState state);
    WorkflowState GetState(string instanceId);
    WorkflowState UpdateState(string instanceId, string currentStep, Whycespace.Systems.Midstream.WSS.Models.WorkflowInstanceStatus status);
    WorkflowState AddCompletedStep(string instanceId, string stepId);
    void DeleteState(string instanceId);
    IReadOnlyList<WorkflowState> ListActiveStates();
}
