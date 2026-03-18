namespace Whycespace.Engines.T1M.WSS.Definition;

using Whycespace.Systems.Midstream.WSS.Models;

public interface IWorkflowValidationEngine
{
    WorkflowValidationResult ValidateWorkflowDefinition(WorkflowDefinition workflow);

    WorkflowValidationResult ValidateWorkflowTemplate(
        string templateId,
        IDictionary<string, string> parameters);

    WorkflowValidationResult ValidateWorkflowVersion(
        string workflowId,
        string version);

    WorkflowValidationResult ValidateCompleteWorkflow(WorkflowDefinition workflow);
}
