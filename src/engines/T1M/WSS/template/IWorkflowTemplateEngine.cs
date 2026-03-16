namespace Whycespace.Engines.T1M.WSS.Template;

using Whycespace.Systems.Midstream.WSS.Models;

public interface IWorkflowTemplateEngine
{
    void RegisterTemplate(WorkflowTemplate template);

    WorkflowTemplate GetTemplate(string templateId);

    IReadOnlyCollection<WorkflowTemplate> ListTemplates();

    WorkflowDefinition GenerateWorkflowDefinition(
        string templateId,
        IDictionary<string, string> parameters);
}
