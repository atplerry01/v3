namespace Whycespace.Engines.T1M.WSS.Definition;

using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Execution;
using Whycespace.Systems.Midstream.WSS.Policies;

public interface IWorkflowVersioningEngine
{
    WorkflowDefinition RegisterWorkflowVersion(WorkflowDefinition workflow);

    WorkflowDefinition GetWorkflowVersion(string workflowId, string version);

    WorkflowDefinition GetLatestWorkflow(string workflowId);

    IReadOnlyList<WorkflowDefinition> ListWorkflowVersions(string workflowId);

    bool WorkflowVersionExists(string workflowId, string version);

    WorkflowVersionResult CreateVersion(WorkflowVersionCommand command, IReadOnlyList<WorkflowDefinition> existingVersions);
}
