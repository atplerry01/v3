namespace Whycespace.Engines.T1M.WSS.Definition;

using Whycespace.System.Midstream.WSS.Models;
using Whycespace.System.Midstream.WSS.Stores;

public sealed class WorkflowTemplateEngine
{
    private readonly WorkflowTemplateStore _templateStore;
    private readonly WorkflowDefinitionStore _definitionStore;

    public WorkflowTemplateEngine(WorkflowTemplateStore templateStore, WorkflowDefinitionStore definitionStore)
    {
        _templateStore = templateStore;
        _definitionStore = definitionStore;
    }

    public WorkflowTemplate CreateTemplate(string templateId, string workflowDefinitionId, IReadOnlyDictionary<string, string> parameters)
    {
        _definitionStore.Get(workflowDefinitionId);

        var template = new WorkflowTemplate(
            templateId,
            workflowDefinitionId,
            parameters,
            DateTimeOffset.UtcNow);

        _templateStore.Register(template);
        return template;
    }

    public WorkflowTemplate GetTemplate(string templateId)
    {
        return _templateStore.Get(templateId);
    }

    public IReadOnlyCollection<WorkflowTemplate> ListTemplates()
    {
        return _templateStore.GetAll();
    }
}
