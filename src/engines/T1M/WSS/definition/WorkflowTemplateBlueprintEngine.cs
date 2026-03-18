namespace Whycespace.Engines.T1M.WSS.Definition;

using global::System.Security.Cryptography;
using global::System.Text;
using global::System.Text.Json;
using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;
using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Execution;
using Whycespace.Systems.Midstream.WSS.Policies;

[EngineManifest("WorkflowTemplateBlueprint", EngineTier.T1M, EngineKind.Decision,
    "WorkflowTemplateCommand", typeof(EngineEvent))]
public sealed class WorkflowTemplateBlueprintEngine : IEngine
{
    public string Name => "WorkflowTemplateBlueprint";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var command = ExtractCommand(context);
        if (command is null)
            return Task.FromResult(EngineResult.Fail("Invalid or missing WorkflowTemplateCommand data"));

        var validationErrors = Validate(command);
        if (validationErrors.Count > 0)
            return Task.FromResult(EngineResult.Fail(
                $"Template validation failed: {string.Join("; ", validationErrors)}"));

        var templateId = GenerateTemplateId(command);
        var createdAt = command.Timestamp;

        var result = new WorkflowTemplateResult(
            templateId,
            command.TemplateName,
            command.TemplateVersion,
            command.TemplateSteps,
            command.TemplateParameters,
            createdAt);

        var events = new[]
        {
            EngineEvent.Create("WorkflowTemplateCreated", Guid.Parse(templateId.PadRight(32, '0')[..32]
                .Insert(8, "-").Insert(13, "-").Insert(18, "-").Insert(23, "-")[..36]
                .Replace("--", "-0")),
                new Dictionary<string, object>
                {
                    ["templateId"] = templateId,
                    ["templateName"] = command.TemplateName,
                    ["templateVersion"] = command.TemplateVersion,
                    ["stepCount"] = command.TemplateSteps.Count,
                    ["parameterCount"] = command.TemplateParameters.Count,
                    ["requestedBy"] = command.RequestedBy
                })
        };

        var output = new Dictionary<string, object>
        {
            ["templateId"] = result.TemplateId,
            ["templateName"] = result.TemplateName,
            ["templateVersion"] = result.TemplateVersion,
            ["templateSteps"] = result.TemplateSteps,
            ["templateParameters"] = result.TemplateParameters,
            ["createdAt"] = result.CreatedAt
        };

        return Task.FromResult(EngineResult.Ok(events, output));
    }

    private static WorkflowTemplateCommand? ExtractCommand(EngineContext context)
    {
        var data = context.Data;

        var templateName = data.GetValueOrDefault("templateName") as string;
        var templateDescription = data.GetValueOrDefault("templateDescription") as string;
        var templateVersion = data.GetValueOrDefault("templateVersion");
        var templateSteps = data.GetValueOrDefault("templateSteps") as IReadOnlyList<WorkflowTemplateCommandStep>;
        var templateParameters = data.GetValueOrDefault("templateParameters") as IReadOnlyList<WorkflowTemplateParameter>;
        var requestedBy = data.GetValueOrDefault("requestedBy") as string;
        var timestamp = data.GetValueOrDefault("timestamp");

        if (templateName is null || templateDescription is null || templateSteps is null ||
            templateParameters is null || requestedBy is null)
            return null;

        var version = templateVersion switch
        {
            int v => v,
            long v => (int)v,
            string s when int.TryParse(s, out var v) => v,
            _ => -1
        };

        if (version < 0)
            return null;

        var ts = timestamp switch
        {
            DateTimeOffset dto => dto,
            string s when DateTimeOffset.TryParse(s, out var dto) => dto,
            _ => DateTimeOffset.UtcNow
        };

        return new WorkflowTemplateCommand(
            templateName,
            templateDescription,
            version,
            templateSteps,
            templateParameters,
            requestedBy,
            ts);
    }

    internal static IReadOnlyList<string> Validate(WorkflowTemplateCommand command)
    {
        var errors = new List<string>();

        // 1. Validate template name
        if (string.IsNullOrWhiteSpace(command.TemplateName))
            errors.Add("TemplateName is required");

        // 2. Validate template version
        if (command.TemplateVersion < 1)
            errors.Add("TemplateVersion must be >= 1");

        // 3. Validate template parameters
        foreach (var param in command.TemplateParameters)
        {
            if (string.IsNullOrWhiteSpace(param.ParameterName))
                errors.Add("Parameter name is required");

            if (string.IsNullOrWhiteSpace(param.ParameterType))
                errors.Add($"Parameter '{param.ParameterName}' must have a type");
        }

        // 4. Validate template steps
        if (command.TemplateSteps.Count == 0)
        {
            errors.Add("Template must have at least one step");
            return errors;
        }

        var stepIds = new HashSet<string>();
        foreach (var step in command.TemplateSteps)
        {
            if (string.IsNullOrWhiteSpace(step.StepId))
                errors.Add("Step ID is required");

            if (string.IsNullOrWhiteSpace(step.StepName))
                errors.Add($"Step '{step.StepId}' must have a name");

            if (string.IsNullOrWhiteSpace(step.EngineName))
                errors.Add($"Step '{step.StepId}' must reference an engine");

            if (step.Timeout <= TimeSpan.Zero)
                errors.Add($"Step '{step.StepId}' must have a positive timeout");

            if (!stepIds.Add(step.StepId))
                errors.Add($"Duplicate step ID: '{step.StepId}'");
        }

        // 5. Validate dependency structure
        ValidateDependencies(command.TemplateSteps, stepIds, errors);

        // 6. Validate parameter bindings
        ValidateParameterBindings(command, errors);

        return errors;
    }

    private static void ValidateDependencies(
        IReadOnlyList<WorkflowTemplateCommandStep> steps,
        HashSet<string> stepIds,
        List<string> errors)
    {
        // Validate all dependency references exist
        foreach (var step in steps)
        {
            foreach (var dep in step.Dependencies)
            {
                if (!stepIds.Contains(dep))
                    errors.Add($"Step '{step.StepId}' depends on non-existent step '{dep}'");

                if (dep == step.StepId)
                    errors.Add($"Step '{step.StepId}' cannot depend on itself");
            }
        }

        // Detect circular dependencies using topological sort (Kahn's algorithm)
        var inDegree = new Dictionary<string, int>();
        var adjacency = new Dictionary<string, List<string>>();

        foreach (var step in steps)
        {
            inDegree[step.StepId] = 0;
            adjacency[step.StepId] = new List<string>();
        }

        foreach (var step in steps)
        {
            foreach (var dep in step.Dependencies)
            {
                if (!adjacency.ContainsKey(dep)) continue;
                adjacency[dep].Add(step.StepId);
                inDegree[step.StepId]++;
            }
        }

        var queue = new Queue<string>();
        foreach (var (stepId, degree) in inDegree)
        {
            if (degree == 0)
                queue.Enqueue(stepId);
        }

        var sorted = 0;
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            sorted++;

            foreach (var dependent in adjacency[current])
            {
                inDegree[dependent]--;
                if (inDegree[dependent] == 0)
                    queue.Enqueue(dependent);
            }
        }

        if (sorted != steps.Count)
            errors.Add("Circular dependency detected in template steps");
    }

    private static void ValidateParameterBindings(
        WorkflowTemplateCommand command,
        List<string> errors)
    {
        var declaredParameters = new HashSet<string>(
            command.TemplateParameters.Select(p => p.ParameterName));

        foreach (var step in command.TemplateSteps)
        {
            foreach (var (bindingKey, bindingValue) in step.ParameterBindings)
            {
                if (!declaredParameters.Contains(bindingValue) &&
                    !declaredParameters.Contains(bindingKey))
                {
                    // Binding references a parameter not declared in the template
                    // Only flag if the value looks like a parameter reference (not a literal)
                    if (bindingValue.StartsWith("$"))
                    {
                        var paramRef = bindingValue.TrimStart('$').Trim('{', '}');
                        if (!declaredParameters.Contains(paramRef))
                            errors.Add($"Step '{step.StepId}' binding '{bindingKey}' references undeclared parameter '{paramRef}'");
                    }
                }
            }
        }
    }

    internal static string GenerateTemplateId(WorkflowTemplateCommand command)
    {
        var structure = new
        {
            command.TemplateName,
            command.TemplateVersion,
            Steps = command.TemplateSteps.Select(s => new
            {
                s.StepId,
                s.StepName,
                s.EngineName,
                s.Dependencies
            }).ToArray()
        };

        var json = JsonSerializer.Serialize(structure, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
