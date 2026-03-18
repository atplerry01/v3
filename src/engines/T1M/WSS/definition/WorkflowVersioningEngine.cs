namespace Whycespace.Engines.T1M.WSS.Definition;

using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Workflows;
using Whycespace.Engines.T1M.Shared;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;
using SystemWorkflowDefinition = Whycespace.Systems.Midstream.WSS.Models.WorkflowDefinition;

[EngineManifest("WorkflowVersioning", EngineTier.T1M, EngineKind.Decision,
    "WorkflowVersionCommand", typeof(EngineEvent))]
public sealed class WorkflowVersioningEngine : IEngine, IWorkflowVersioningEngine
{
    private readonly IVersionStore? _store;

    public string Name => "WorkflowVersioning";

    public WorkflowVersioningEngine() { }

    public WorkflowVersioningEngine(IVersionStore store)
    {
        _store = store;
    }

    /// <summary>
    /// Abstraction for version storage while the persistence layer is migrated.
    /// </summary>
    public interface IVersionStore
    {
        void Store(SystemWorkflowDefinition workflow);
        SystemWorkflowDefinition? Get(string workflowId, string version);
        SystemWorkflowDefinition? GetLatest(string workflowId);
        IReadOnlyList<SystemWorkflowDefinition> GetVersions(string workflowId);
        bool VersionExists(string workflowId, string version);
    }

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var action = context.Data.GetValueOrDefault("action") as string;

        return action switch
        {
            "createVersion" => Task.FromResult(HandleCreateVersion(context)),
            "getVersion" => Task.FromResult(HandleGetVersion(context)),
            "getLatest" => Task.FromResult(HandleGetLatest(context)),
            "listVersions" => Task.FromResult(HandleListVersions(context)),
            _ => Task.FromResult(EngineResult.Fail(
                $"Unknown action '{action}'. Expected: createVersion, getVersion, getLatest, listVersions"))
        };
    }

    // --- IEngine action handlers ---

    private EngineResult HandleCreateVersion(EngineContext context)
    {
        var workflowName = context.Data.GetValueOrDefault("workflowName") as string;
        var baseVersion = context.Data.GetValueOrDefault("baseVersion") as string;
        var changeDescription = context.Data.GetValueOrDefault("changeDescription") as string;
        var requestedBy = context.Data.GetValueOrDefault("requestedBy") as string;
        var newDefinition = context.Data.GetValueOrDefault("newDefinition") as IReadOnlyList<WorkflowStep>;

        if (string.IsNullOrWhiteSpace(workflowName))
            return EngineResult.Fail("Missing workflowName");

        if (string.IsNullOrWhiteSpace(baseVersion))
            return EngineResult.Fail("Missing baseVersion");

        if (!IsValidSemanticVersion(baseVersion))
            return EngineResult.Fail($"Invalid base version format: '{baseVersion}'");

        if (newDefinition is null || newDefinition.Count == 0)
            return EngineResult.Fail("Missing or empty newDefinition");

        var command = new WorkflowVersionCommand(
            Guid.NewGuid(),
            workflowName,
            baseVersion,
            newDefinition,
            changeDescription ?? "",
            requestedBy ?? "system",
            DateTimeOffset.UtcNow);

        var existingVersions = _store?.GetVersions(workflowName) ?? Array.Empty<SystemWorkflowDefinition>();
        var result = CreateVersion(command, existingVersions);

        if (!result.Success)
            return EngineResult.Fail(result.Message);

        var evt = EngineEvent.Create(
            "WorkflowVersionCreated",
            Guid.NewGuid(),
            new Dictionary<string, object>
            {
                ["workflowName"] = result.WorkflowName,
                ["newVersion"] = result.NewVersion,
                ["baseVersion"] = result.BaseVersion,
                ["compatibilityLevel"] = result.CompatibilityLevel.ToString(),
                ["changeDescription"] = result.ChangeDescription
            });

        return EngineResult.Ok(new[] { evt }, new Dictionary<string, object>
        {
            ["workflowId"] = result.WorkflowId,
            ["workflowName"] = result.WorkflowName,
            ["newVersion"] = result.NewVersion,
            ["baseVersion"] = result.BaseVersion,
            ["compatibilityLevel"] = result.CompatibilityLevel.ToString(),
            ["changeDescription"] = result.ChangeDescription,
            ["message"] = result.Message
        });
    }

    private EngineResult HandleGetVersion(EngineContext context)
    {
        var workflowId = context.Data.GetValueOrDefault("workflowId") as string;
        var version = context.Data.GetValueOrDefault("version") as string;

        if (string.IsNullOrWhiteSpace(workflowId))
            return EngineResult.Fail("Missing workflowId");

        if (string.IsNullOrWhiteSpace(version))
            return EngineResult.Fail("Missing version");

        var workflow = _store?.Get(workflowId, version);
        if (workflow is null)
            return EngineResult.Fail($"Version '{version}' not found for workflow '{workflowId}'");

        return EngineResult.Ok(Array.Empty<EngineEvent>(), new Dictionary<string, object>
        {
            ["workflowId"] = workflow.WorkflowId,
            ["workflowName"] = workflow.Name,
            ["version"] = workflow.Version
        });
    }

    private EngineResult HandleGetLatest(EngineContext context)
    {
        var workflowId = context.Data.GetValueOrDefault("workflowId") as string;

        if (string.IsNullOrWhiteSpace(workflowId))
            return EngineResult.Fail("Missing workflowId");

        var workflow = _store?.GetLatest(workflowId);
        if (workflow is null)
            return EngineResult.Fail($"No versions found for workflow '{workflowId}'");

        return EngineResult.Ok(Array.Empty<EngineEvent>(), new Dictionary<string, object>
        {
            ["workflowId"] = workflow.WorkflowId,
            ["workflowName"] = workflow.Name,
            ["version"] = workflow.Version
        });
    }

    private EngineResult HandleListVersions(EngineContext context)
    {
        var workflowId = context.Data.GetValueOrDefault("workflowId") as string;

        if (string.IsNullOrWhiteSpace(workflowId))
            return EngineResult.Fail("Missing workflowId");

        var versions = _store?.GetVersions(workflowId) ?? Array.Empty<SystemWorkflowDefinition>();

        return EngineResult.Ok(Array.Empty<EngineEvent>(), new Dictionary<string, object>
        {
            ["workflowId"] = workflowId,
            ["versions"] = versions.Select(v => v.Version).ToList(),
            ["count"] = versions.Count
        });
    }

    // --- IWorkflowVersioningEngine facade methods ---

    public SystemWorkflowDefinition RegisterWorkflowVersion(SystemWorkflowDefinition workflow)
    {
        if (string.IsNullOrWhiteSpace(workflow.WorkflowId))
            throw new ArgumentException("WorkflowId must not be empty.");

        if (string.IsNullOrWhiteSpace(workflow.Version))
            throw new ArgumentException("Version must not be empty.");

        if (!IsValidSemanticVersion(workflow.Version))
            throw new ArgumentException($"Invalid semantic version format: '{workflow.Version}'. Expected Major.Minor.Patch (e.g. 1.0.0).");

        _store?.Store(workflow);
        return workflow;
    }

    public SystemWorkflowDefinition GetWorkflowVersion(string workflowId, string version)
    {
        return _store?.Get(workflowId, version)
            ?? throw new KeyNotFoundException($"Version '{version}' not found for workflow: '{workflowId}'");
    }

    public SystemWorkflowDefinition GetLatestWorkflow(string workflowId)
    {
        return _store?.GetLatest(workflowId)
            ?? throw new KeyNotFoundException($"No versions found for workflow: '{workflowId}'");
    }

    public IReadOnlyList<SystemWorkflowDefinition> ListWorkflowVersions(string workflowId)
    {
        return _store?.GetVersions(workflowId) ?? Array.Empty<SystemWorkflowDefinition>();
    }

    public bool WorkflowVersionExists(string workflowId, string version)
    {
        return _store?.VersionExists(workflowId, version) ?? false;
    }

    // --- Version creation logic (stateless, deterministic) ---

    public WorkflowVersionResult CreateVersion(
        WorkflowVersionCommand command,
        IReadOnlyList<SystemWorkflowDefinition> existingVersions)
    {
        var baseDefinition = existingVersions
            .FirstOrDefault(v => v.Version == command.BaseVersion);

        if (baseDefinition is null && existingVersions.Count > 0)
        {
            return new WorkflowVersionResult(
                Success: false,
                WorkflowId: "",
                WorkflowName: command.WorkflowName,
                NewVersion: "",
                BaseVersion: command.BaseVersion,
                CompatibilityLevel: CompatibilityLevel.Compatible,
                ChangeDescription: command.ChangeDescription,
                CreatedAt: command.Timestamp,
                Message: $"Base version '{command.BaseVersion}' not found");
        }

        var compatibility = DetermineCompatibility(
            baseDefinition?.Steps,
            command.NewDefinition);

        var newVersion = IncrementVersion(command.BaseVersion, compatibility);

        var workflowId = baseDefinition?.WorkflowId ?? command.WorkflowName;

        return new WorkflowVersionResult(
            Success: true,
            WorkflowId: workflowId,
            WorkflowName: command.WorkflowName,
            NewVersion: newVersion,
            BaseVersion: command.BaseVersion,
            CompatibilityLevel: compatibility,
            ChangeDescription: command.ChangeDescription,
            CreatedAt: command.Timestamp,
            Message: $"Version {newVersion} created ({compatibility})");
    }

    internal static CompatibilityLevel DetermineCompatibility(
        IReadOnlyList<WorkflowStep>? baseSteps,
        IReadOnlyList<WorkflowStep> newSteps)
    {
        if (baseSteps is null || baseSteps.Count == 0)
            return CompatibilityLevel.Compatible;

        var baseStepIds = new HashSet<string>(baseSteps.Select(s => s.StepId));
        var newStepIds = new HashSet<string>(newSteps.Select(s => s.StepId));

        var removedSteps = baseStepIds.Except(newStepIds).ToList();

        // Removed steps = breaking change
        if (removedSteps.Count > 0)
            return CompatibilityLevel.Breaking;

        // Check for modified steps (engine or transition changes)
        var baseStepMap = baseSteps.ToDictionary(s => s.StepId);
        foreach (var newStep in newSteps)
        {
            if (!baseStepMap.TryGetValue(newStep.StepId, out var baseStep))
                continue;

            // Engine changed = breaking
            if (baseStep.EngineName != newStep.EngineName)
                return CompatibilityLevel.Breaking;

            // Removed transitions from existing step = breaking
            var removedTransitions = baseStep.NextSteps.Except(newStep.NextSteps).ToList();
            if (removedTransitions.Count > 0)
                return CompatibilityLevel.Breaking;
        }

        // New steps added = backward compatible (minor)
        var addedSteps = newStepIds.Except(baseStepIds).ToList();
        if (addedSteps.Count > 0)
            return CompatibilityLevel.BackwardCompatible;

        // Only metadata/transition additions on existing steps = compatible (patch)
        var hasTransitionAdditions = newSteps.Any(ns =>
        {
            if (!baseStepMap.TryGetValue(ns.StepId, out var bs))
                return false;
            return ns.NextSteps.Except(bs.NextSteps).Any();
        });

        if (hasTransitionAdditions)
            return CompatibilityLevel.BackwardCompatible;

        return CompatibilityLevel.Compatible;
    }

    internal static string IncrementVersion(string baseVersion, CompatibilityLevel compatibility)
    {
        var parts = baseVersion.Split('.');
        var major = int.Parse(parts[0]);
        var minor = int.Parse(parts[1]);
        var patch = int.Parse(parts[2]);

        return compatibility switch
        {
            CompatibilityLevel.Breaking => $"{major + 1}.0.0",
            CompatibilityLevel.BackwardCompatible => $"{major}.{minor + 1}.0",
            CompatibilityLevel.Compatible => $"{major}.{minor}.{patch + 1}",
            _ => $"{major}.{minor}.{patch + 1}"
        };
    }

    internal static bool IsValidSemanticVersion(string version)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(version, @"^\d+\.\d+\.\d+$");
    }
}
