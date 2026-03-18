namespace Whycespace.Runtime.Persistence.Workflow;

using global::System.Collections.Concurrent;
using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Execution;
using Whycespace.Systems.Midstream.WSS.Policies;

public sealed class WorkflowTemplateStore
{
    private readonly ConcurrentDictionary<string, WorkflowTemplate> _templates = new();

    public void Register(WorkflowTemplate template)
    {
        if (!_templates.TryAdd(template.TemplateId, template))
            throw new InvalidOperationException($"Duplicate template: {template.TemplateId}");
    }

    public WorkflowTemplate Get(string templateId)
    {
        if (!_templates.TryGetValue(templateId, out var template))
            throw new KeyNotFoundException($"Workflow template not found: {templateId}");
        return template;
    }

    public IReadOnlyCollection<WorkflowTemplate> GetAll()
    {
        return _templates.Values.ToList();
    }
}
