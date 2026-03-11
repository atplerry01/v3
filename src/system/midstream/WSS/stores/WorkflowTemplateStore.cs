namespace Whycespace.System.Midstream.WSS.Stores;

using global::System.Collections.Concurrent;
using Whycespace.System.Midstream.WSS.Models;

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
