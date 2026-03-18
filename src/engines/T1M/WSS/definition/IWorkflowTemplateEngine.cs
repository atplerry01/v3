namespace Whycespace.Engines.T1M.WSS.Definition;

using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Execution;
using Whycespace.Systems.Midstream.WSS.Policies;

public interface IWorkflowTemplateEngine
{
    void RegisterTemplate(WorkflowTemplate template);

    WorkflowTemplate GetTemplate(string templateId);

    IReadOnlyCollection<WorkflowTemplate> ListTemplates();

    WorkflowDefinition GenerateWorkflowDefinition(
        string templateId,
        IDictionary<string, string> parameters);
}
