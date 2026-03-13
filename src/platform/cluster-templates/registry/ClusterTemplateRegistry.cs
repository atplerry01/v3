namespace Whycespace.ClusterTemplatePlatform;

public sealed class ClusterTemplateRegistry
{
    private readonly Dictionary<string, ClusterTemplate> _templates = new();

    public void RegisterTemplate(ClusterTemplate template)
    {
        _templates[template.TemplateName] = template;
    }

    public ClusterTemplate GetTemplate(string templateName)
    {
        if (!_templates.TryGetValue(templateName, out var template))
            throw new InvalidOperationException($"Template '{templateName}' not found.");

        return template;
    }

    public IReadOnlyCollection<string> ListTemplates()
    {
        return _templates.Keys.ToList().AsReadOnly();
    }
}
