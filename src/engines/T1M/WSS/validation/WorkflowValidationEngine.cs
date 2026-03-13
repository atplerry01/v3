namespace Whycespace.Engines.T1M.WSS.Validation;

using Whycespace.Contracts.Workflows;
using Whycespace.Engines.T1M.WSS.Definition;
using Whycespace.Engines.T1M.WSS.Graph;
using Whycespace.Engines.T1M.WSS.Stores;
using Whycespace.System.Midstream.WSS.Models;

public sealed class WorkflowValidationOrchestrator : IWorkflowValidationEngine
{
    private readonly WorkflowDefinitionEngine _definitionEngine;
    private readonly WorkflowGraphEngine _graphEngine;
    private readonly WorkflowTemplateEngine _templateEngine;
    private readonly WorkflowVersioningEngine _versioningEngine;

    public WorkflowValidationOrchestrator(
        WorkflowDefinitionEngine definitionEngine,
        WorkflowGraphEngine graphEngine,
        WorkflowTemplateEngine templateEngine,
        WorkflowVersioningEngine versioningEngine)
    {
        _definitionEngine = definitionEngine;
        _graphEngine = graphEngine;
        _templateEngine = templateEngine;
        _versioningEngine = versioningEngine;
    }

    public WorkflowValidationResult ValidateWorkflowDefinition(WorkflowDefinition workflow)
    {
        var errors = new List<WorkflowValidationError>();
        var warnings = new List<WorkflowValidationError>();

        // Definition validation
        var definitionViolations = _definitionEngine.ValidateWorkflowDefinition(workflow);
        foreach (var violation in definitionViolations)
        {
            errors.Add(new WorkflowValidationError(
                ClassifyDefinitionViolation(violation),
                violation,
                "Definition"));
        }

        if (workflow.Steps.Count == 0)
            return WorkflowValidationResult.Create(errors, warnings);

        // Graph validation
        var graphModel = BuildGraphFromSteps(workflow);
        var graphViolations = _graphEngine.ValidateGraph(graphModel);
        foreach (var violation in graphViolations)
        {
            errors.Add(new WorkflowValidationError(
                ClassifyGraphViolation(violation),
                violation,
                "Graph",
                ExtractStepId(violation)));
        }

        return WorkflowValidationResult.Create(errors, warnings);
    }

    public WorkflowValidationResult ValidateWorkflowTemplate(
        string templateId,
        IDictionary<string, string> parameters)
    {
        var errors = new List<WorkflowValidationError>();

        // Verify template exists
        WorkflowTemplate template;
        try
        {
            template = _templateEngine.GetTemplate(templateId);
        }
        catch (KeyNotFoundException)
        {
            errors.Add(new WorkflowValidationError(
                "TEMPLATE_NOT_FOUND",
                $"Template '{templateId}' does not exist.",
                "Template"));
            return WorkflowValidationResult.Invalid(errors);
        }

        // Verify parameters
        var missingParams = FindMissingTemplateParameters(template, parameters);
        foreach (var param in missingParams)
        {
            errors.Add(new WorkflowValidationError(
                "INVALID_TEMPLATE_PARAMETER",
                $"Missing required template parameter: '{param}'.",
                "Template"));
        }

        if (errors.Count > 0)
            return WorkflowValidationResult.Invalid(errors);

        // Generate definition and validate it
        try
        {
            var definition = _templateEngine.GenerateWorkflowDefinition(templateId, parameters);
            return ValidateWorkflowDefinition(definition);
        }
        catch (ArgumentException ex)
        {
            errors.Add(new WorkflowValidationError(
                "INVALID_TEMPLATE_PARAMETER",
                ex.Message,
                "Template"));
            return WorkflowValidationResult.Invalid(errors);
        }
    }

    public WorkflowValidationResult ValidateWorkflowVersion(
        string workflowId,
        string version)
    {
        var errors = new List<WorkflowValidationError>();

        if (string.IsNullOrWhiteSpace(workflowId))
        {
            errors.Add(new WorkflowValidationError(
                "INVALID_VERSION",
                "WorkflowId must not be empty.",
                "Version"));
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            errors.Add(new WorkflowValidationError(
                "INVALID_VERSION",
                "Version must not be empty.",
                "Version"));
            return WorkflowValidationResult.Invalid(errors);
        }

        if (!WorkflowVersionStore.IsValidSemanticVersion(version))
        {
            errors.Add(new WorkflowValidationError(
                "INVALID_VERSION",
                $"Invalid semantic version format: '{version}'. Expected Major.Minor.Patch (e.g. 1.0.0).",
                "Version"));
        }

        if (errors.Count > 0)
            return WorkflowValidationResult.Invalid(errors);

        // Check version existence
        if (_versioningEngine.WorkflowVersionExists(workflowId, version))
        {
            errors.Add(new WorkflowValidationError(
                "INVALID_VERSION",
                $"Version '{version}' already exists for workflow '{workflowId}'.",
                "Version"));
        }

        return errors.Count > 0
            ? WorkflowValidationResult.Invalid(errors)
            : WorkflowValidationResult.Valid();
    }

    public WorkflowValidationResult ValidateCompleteWorkflow(WorkflowDefinition workflow)
    {
        var definitionResult = ValidateWorkflowDefinition(workflow);
        var versionResult = ValidateWorkflowVersion(workflow.WorkflowId, workflow.Version);

        return WorkflowValidationResult.Combine(definitionResult, versionResult);
    }

    private static Whycespace.System.Midstream.WSS.Models.WorkflowGraph BuildGraphFromSteps(WorkflowDefinition workflow)
    {
        var transitions = new Dictionary<string, IReadOnlyList<string>>();
        foreach (var step in workflow.Steps)
        {
            transitions[step.StepId] = step.NextSteps;
        }
        return new Whycespace.System.Midstream.WSS.Models.WorkflowGraph(workflow.WorkflowId, transitions);
    }

    private static string ClassifyDefinitionViolation(string violation)
    {
        if (violation.Contains("Duplicate step ID"))
            return "DUPLICATE_STEP_ID";
        if (violation.Contains("does not exist"))
            return "MISSING_STEP_REFERENCE";
        if (violation.Contains("Circular dependency"))
            return "CIRCULAR_DEPENDENCY";
        if (violation.Contains("at least one step"))
            return "EMPTY_WORKFLOW";
        return "INVALID_DEFINITION";
    }

    private static string ClassifyGraphViolation(string violation)
    {
        if (violation.Contains("Circular dependency"))
            return "CIRCULAR_DEPENDENCY";
        if (violation.Contains("undefined node") || violation.Contains("does not exist"))
            return "MISSING_STEP_REFERENCE";
        if (violation.Contains("Orphan") || violation.Contains("unreachable"))
            return "INVALID_GRAPH";
        if (violation.Contains("no start"))
            return "INVALID_GRAPH";
        return "INVALID_GRAPH";
    }

    private static string? ExtractStepId(string violation)
    {
        if (violation.Contains("Step '"))
        {
            var start = violation.IndexOf("Step '") + 6;
            var end = violation.IndexOf("'", start);
            if (end > start)
                return violation[start..end];
        }
        return null;
    }

    private static IReadOnlyList<string> FindMissingTemplateParameters(
        WorkflowTemplate template,
        IDictionary<string, string> parameters)
    {
        var paramPattern = new global::System.Text.RegularExpressions.Regex(@"\$\{(\w+)\}");
        var required = new HashSet<string>();

        void Collect(string input)
        {
            foreach (global::System.Text.RegularExpressions.Match match in paramPattern.Matches(input))
                required.Add(match.Groups[1].Value);
        }

        Collect(template.Name);
        Collect(template.Description);

        foreach (var step in template.Steps)
        {
            Collect(step.Command);
            Collect(step.Engine);
            Collect(step.Description);
        }

        return required.Where(p => !parameters.ContainsKey(p)).ToList();
    }
}
