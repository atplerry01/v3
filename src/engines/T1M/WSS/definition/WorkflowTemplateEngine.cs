namespace Whycespace.Engines.T1M.WSS.Definition;

using global::System.Text.RegularExpressions;
using Whycespace.Contracts.Workflows;
using Whycespace.Engines.T1M.WSS.Graph;
using Whycespace.Engines.T1M.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Execution;
using Whycespace.Systems.Midstream.WSS.Policies;

public sealed class WorkflowTemplateEngine : IWorkflowTemplateEngine
{
    private static readonly Regex ParameterPattern = new(@"\$\{(\w+)\}", RegexOptions.Compiled);

    private readonly ITemplateStore? _templateStore;
    private readonly IWorkflowGraphEngine _graphEngine;

    public WorkflowTemplateEngine(IWorkflowGraphEngine graphEngine)
    {
        _graphEngine = graphEngine;
    }

    public WorkflowTemplateEngine(ITemplateStore templateStore, IWorkflowGraphEngine graphEngine)
    {
        _templateStore = templateStore;
        _graphEngine = graphEngine;
    }

    /// <summary>
    /// Abstraction for template storage while the persistence layer is migrated.
    /// </summary>
    public interface ITemplateStore
    {
        void Register(WorkflowTemplate template);
        WorkflowTemplate Get(string templateId);
        IReadOnlyCollection<WorkflowTemplate> GetAll();
    }

    public void RegisterTemplate(WorkflowTemplate template)
    {
        ValidateTemplate(template);
        _templateStore?.Register(template);
    }

    public WorkflowTemplate GetTemplate(string templateId)
    {
        return _templateStore?.Get(templateId)
            ?? throw new KeyNotFoundException($"Template '{templateId}' not found (store not configured).");
    }

    public IReadOnlyCollection<WorkflowTemplate> ListTemplates()
    {
        return _templateStore?.GetAll() ?? Array.Empty<WorkflowTemplate>();
    }

    public WorkflowDefinition GenerateWorkflowDefinition(
        string templateId,
        IDictionary<string, string> parameters)
    {
        var template = _templateStore?.Get(templateId)
            ?? throw new KeyNotFoundException($"Template '{templateId}' not found (store not configured).");

        ValidateParametersResolved(template, parameters);

        var steps = new List<WorkflowStep>();

        foreach (var templateStep in template.Steps)
        {
            var resolvedCommand = SubstituteParameters(templateStep.Command, parameters);
            var resolvedEngine = SubstituteParameters(templateStep.Engine, parameters);

            var nextSteps = template.Graph.Transitions.TryGetValue(templateStep.StepId, out var transitions)
                ? transitions.ToList()
                : new List<string>();

            steps.Add(new WorkflowStep(
                templateStep.StepId,
                resolvedCommand,
                resolvedEngine,
                nextSteps));
        }

        var workflowId = parameters.TryGetValue("workflowId", out var wfId)
            ? wfId
            : $"{template.TemplateId}-{Guid.NewGuid():N}";

        var resolvedName = SubstituteParameters(template.Name, parameters);

        return new WorkflowDefinition(
            workflowId,
            resolvedName,
            SubstituteParameters(template.Description, parameters),
            $"{template.Version}.0.0",
            steps,
            DateTimeOffset.UtcNow);
    }

    private void ValidateTemplate(WorkflowTemplate template)
    {
        if (string.IsNullOrWhiteSpace(template.TemplateId))
            throw new ArgumentException("TemplateId is required.");

        if (string.IsNullOrWhiteSpace(template.Name))
            throw new ArgumentException("Template Name is required.");

        if (template.Steps.Count == 0)
            throw new ArgumentException("Template must have at least one step.");

        var stepIds = new HashSet<string>();
        foreach (var step in template.Steps)
        {
            if (!stepIds.Add(step.StepId))
                throw new ArgumentException($"Duplicate step ID: '{step.StepId}'.");
        }

        var graphViolations = _graphEngine.ValidateGraph(template.Graph);
        if (graphViolations.Count > 0)
            throw new ArgumentException($"Invalid template graph: {string.Join("; ", graphViolations)}");
    }

    private static void ValidateParametersResolved(WorkflowTemplate template, IDictionary<string, string> parameters)
    {
        var missing = new HashSet<string>();

        foreach (var step in template.Steps)
        {
            CollectMissingParameters(step.Command, parameters, missing);
            CollectMissingParameters(step.Engine, parameters, missing);
            CollectMissingParameters(step.Description, parameters, missing);
        }

        CollectMissingParameters(template.Name, parameters, missing);
        CollectMissingParameters(template.Description, parameters, missing);

        if (missing.Count > 0)
            throw new ArgumentException($"Missing template parameters: {string.Join(", ", missing)}");
    }

    private static void CollectMissingParameters(string input, IDictionary<string, string> parameters, HashSet<string> missing)
    {
        foreach (Match match in ParameterPattern.Matches(input))
        {
            var paramName = match.Groups[1].Value;
            if (!parameters.ContainsKey(paramName))
                missing.Add(paramName);
        }
    }

    private static string SubstituteParameters(string input, IDictionary<string, string> parameters)
    {
        return ParameterPattern.Replace(input, match =>
        {
            var paramName = match.Groups[1].Value;
            return parameters.TryGetValue(paramName, out var value) ? value : match.Value;
        });
    }
}
