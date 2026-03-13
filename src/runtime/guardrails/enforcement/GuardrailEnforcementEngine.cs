namespace Whycespace.ArchitectureGuardrails.Enforcement;

using global::System.Reflection;
using Whycespace.ArchitectureGuardrails.Architecture;
using Whycespace.ArchitectureGuardrails.Rules;
using Whycespace.ArchitectureGuardrails.Validation;
using Whycespace.Runtime.Registry;

public sealed record GuardrailReport(
    bool IsValid,
    IReadOnlyList<EngineValidationResult> EngineResults,
    IReadOnlyList<EventValidationResult> EventResults,
    BuildValidationResult BuildResult,
    IReadOnlyList<string> AllViolations
);

public sealed class GuardrailEnforcementEngine
{
    private readonly EngineArchitectureValidator _engineValidator;
    private readonly EventSchemaValidator _eventValidator;
    private readonly BuildDeterminismValidator _buildValidator;

    public GuardrailEnforcementEngine()
    {
        _engineValidator = new EngineArchitectureValidator();
        _eventValidator = new EventSchemaValidator();
        _buildValidator = new BuildDeterminismValidator();
    }

    public GuardrailReport Validate(Assembly engineAssembly, Assembly sharedAssembly)
    {
        var engineResults = _engineValidator.ValidateAllEngines(engineAssembly);
        var eventResults = _eventValidator.ValidateEventTypes(sharedAssembly);
        var buildResult = _buildValidator.Validate(engineAssembly);

        var allViolations = new List<string>();
        allViolations.AddRange(engineResults.SelectMany(r => r.Violations));
        allViolations.AddRange(eventResults.SelectMany(r => r.Violations));
        allViolations.AddRange(buildResult.Violations);

        return new GuardrailReport(
            allViolations.Count == 0,
            engineResults,
            eventResults,
            buildResult,
            allViolations
        );
    }

    public GuardrailReport ValidateWithWorkflows(
        Assembly engineAssembly,
        Assembly sharedAssembly,
        EngineRegistry registry,
        IEnumerable<Whycespace.Contracts.Workflows.WorkflowGraph> workflows)
    {
        var baseReport = Validate(engineAssembly, sharedAssembly);

        var workflowValidator = new WorkflowArchitectureValidator(registry);
        var workflowViolations = workflows
            .Select(w => workflowValidator.ValidateWorkflow(w))
            .SelectMany(r => r.Violations)
            .ToList();

        var allViolations = new List<string>(baseReport.AllViolations);
        allViolations.AddRange(workflowViolations);

        return baseReport with
        {
            IsValid = allViolations.Count == 0,
            AllViolations = allViolations
        };
    }

    public static IReadOnlyList<string> GetRuleNames() => ArchitectureRules.Names;

    public static IReadOnlyList<string> GetRuleDescriptions() => ArchitectureRules.All;
}
