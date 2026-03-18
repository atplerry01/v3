namespace Whycespace.Runtime.Persistence.Workflow;

using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Execution;
using Whycespace.Systems.Midstream.WSS.Policies;

public interface IWssWorkflowStateStore
{
    void SaveState(WorkflowState state);
    WorkflowState GetState(string instanceId);
    WorkflowState UpdateState(string instanceId, string currentStep, WorkflowInstanceStatus status);
    WorkflowState AddCompletedStep(string instanceId, string stepId);
    void DeleteState(string instanceId);
    IReadOnlyList<WorkflowState> ListActiveStates();
}
