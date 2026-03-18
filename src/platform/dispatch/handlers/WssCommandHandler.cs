using Whycespace.Contracts.Runtime;
using Whycespace.Contracts.Workflows;
using Whycespace.Engines.T1M.WSS.Definition;
using Whycespace.Engines.T1M.WSS.Graph;
using Whycespace.Engines.T1M.WSS.Registry;
using Whycespace.Engines.T1M.WSS.Step;
using Whycespace.Systems.Midstream.WSS.Events;
using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Execution;
using Whycespace.Systems.Midstream.WSS.Policies;
using Whycespace.Runtime.Persistence.Workflow;
using WssWorkflowEventRouter = Whycespace.Engines.T1M.Orchestration.Dispatcher.WorkflowEventRouter;
using Whycespace.Engines.T1M.Orchestration.Dispatcher;
using Whycespace.Engines.T1M.Orchestration.Resilience;
using Whycespace.Engines.T1M.Orchestration.Scheduling;
using WorkflowGraph = Whycespace.Systems.Midstream.WSS.Models.WorkflowGraph;
using WorkflowState = Whycespace.Systems.Midstream.WSS.Execution.WorkflowState;
using WorkflowDefinition = Whycespace.Systems.Midstream.WSS.Definition.WorkflowDefinition;

namespace Whycespace.Platform.Dispatch.Handlers;

public sealed class WssCommandHandler
{
    private readonly WorkflowDefinitionStore _workflowDefinitionStore;
    private readonly WorkflowTemplateStore _workflowTemplateStore;
    private readonly WorkflowRegistryStore _workflowRegistryStore;
    private readonly WorkflowVersionStore _workflowVersionStore;
    private readonly WorkflowEngineMappingStore _engineMappingStore;
    private readonly WorkflowInstanceRegistryStore _instanceRegistryStore;
    private readonly WssWorkflowStateStore _wssWorkflowStateStore;
    private readonly WorkflowRetryStore _workflowRetryStore;
    private readonly WorkflowTimeoutStore _workflowTimeoutStore;
    private readonly WorkflowRegistry _workflowRegistry;
    private readonly WssWorkflowEventRouter _wssWorkflowEventRouter;
    private readonly WorkflowRetryPolicyEngine _workflowRetryPolicyEngine;
    private readonly WorkflowTimeoutEngine _workflowTimeoutEngine;
    private readonly WorkflowLifecycleEngine _workflowLifecycleEngine;

    public WssCommandHandler(
        WorkflowDefinitionStore workflowDefinitionStore,
        WorkflowTemplateStore workflowTemplateStore,
        WorkflowRegistryStore workflowRegistryStore,
        WorkflowVersionStore workflowVersionStore,
        WorkflowEngineMappingStore engineMappingStore,
        WorkflowInstanceRegistryStore instanceRegistryStore,
        WssWorkflowStateStore wssWorkflowStateStore,
        WorkflowRetryStore workflowRetryStore,
        WorkflowTimeoutStore workflowTimeoutStore,
        WorkflowRegistry workflowRegistry,
        WssWorkflowEventRouter wssWorkflowEventRouter,
        WorkflowRetryPolicyEngine workflowRetryPolicyEngine,
        WorkflowTimeoutEngine workflowTimeoutEngine,
        WorkflowLifecycleEngine workflowLifecycleEngine)
    {
        _workflowDefinitionStore = workflowDefinitionStore;
        _workflowTemplateStore = workflowTemplateStore;
        _workflowRegistryStore = workflowRegistryStore;
        _workflowVersionStore = workflowVersionStore;
        _engineMappingStore = engineMappingStore;
        _instanceRegistryStore = instanceRegistryStore;
        _wssWorkflowStateStore = wssWorkflowStateStore;
        _workflowRetryStore = workflowRetryStore;
        _workflowTimeoutStore = workflowTimeoutStore;
        _workflowRegistry = workflowRegistry;
        _wssWorkflowEventRouter = wssWorkflowEventRouter;
        _workflowRetryPolicyEngine = workflowRetryPolicyEngine;
        _workflowTimeoutEngine = workflowTimeoutEngine;
        _workflowLifecycleEngine = workflowLifecycleEngine;
    }

    public bool CanHandle(string command) => command.StartsWith("wss.");

    public Task<DispatchResult> HandleAsync(string command, Dictionary<string, object> payload)
    {
        return command switch
        {
            // Definitions
            "wss.definition.list" => Task.FromResult(HandleDefinitionList()),
            "wss.definition.get" => Task.FromResult(HandleDefinitionGet(payload)),
            "wss.definition.register" => Task.FromResult(HandleDefinitionRegister(payload)),

            // Graph
            "wss.graph.build" => Task.FromResult(HandleGraphBuild(payload)),
            "wss.graph.validate" => Task.FromResult(HandleGraphValidate(payload)),

            // Templates
            "wss.template.list" => Task.FromResult(HandleTemplateList()),
            "wss.template.get" => Task.FromResult(HandleTemplateGet(payload)),
            "wss.template.register" => Task.FromResult(HandleTemplateRegister(payload)),
            "wss.template.generate" => Task.FromResult(HandleTemplateGenerate(payload)),

            // Registry
            "wss.registry.list" => Task.FromResult(HandleRegistryList()),
            "wss.registry.get" => Task.FromResult(HandleRegistryGet(payload)),
            "wss.registry.register" => Task.FromResult(HandleRegistryRegister(payload)),
            "wss.registry.remove" => Task.FromResult(HandleRegistryRemove(payload)),

            // Versioning
            "wss.version.list" => Task.FromResult(HandleVersionList(payload)),
            "wss.version.get" => Task.FromResult(HandleVersionGet(payload)),
            "wss.version.latest" => Task.FromResult(HandleVersionLatest(payload)),
            "wss.version.register" => Task.FromResult(HandleVersionRegister(payload)),

            // Validation
            "wss.validation.workflow" => Task.FromResult(HandleValidationWorkflow(payload)),
            "wss.validation.template" => Task.FromResult(HandleValidationTemplate(payload)),
            "wss.validation.version" => Task.FromResult(HandleValidationVersion(payload)),

            // Dependency
            "wss.dependency.analyze" => Task.FromResult(HandleDependencyAnalyze(payload)),
            "wss.dependency.get" => Task.FromResult(HandleDependencyGet(payload)),
            "wss.dependency.resolve" => Task.FromResult(HandleDependencyResolve(payload)),

            // Engine Mapping
            "wss.engine.list" => Task.FromResult(HandleEngineList()),
            "wss.engine.register" => Task.FromResult(HandleEngineRegister(payload)),
            "wss.engine.resolve" => Task.FromResult(HandleEngineResolve(payload)),

            // Instance
            "wss.instance.list" => Task.FromResult(HandleInstanceList()),
            "wss.instance.get" => Task.FromResult(HandleInstanceGet(payload)),
            "wss.instance.create" => Task.FromResult(HandleInstanceCreate(payload)),
            "wss.instance.update" => Task.FromResult(HandleInstanceUpdate(payload)),
            "wss.instance.remove" => Task.FromResult(HandleInstanceRemove(payload)),

            // State
            "wss.state.list" => Task.FromResult(HandleStateList()),
            "wss.state.get" => Task.FromResult(HandleStateGet(payload)),
            "wss.state.save" => Task.FromResult(HandleStateSave(payload)),
            "wss.state.update" => Task.FromResult(HandleStateUpdate(payload)),
            "wss.state.delete" => Task.FromResult(HandleStateDelete(payload)),

            // Events
            "wss.events.publish" => HandleEventsPublish(payload),
            "wss.events.types" => Task.FromResult(HandleEventsTypes()),

            // Retry
            "wss.retry.get" => Task.FromResult(HandleRetryGet(payload)),
            "wss.retry.register" => Task.FromResult(HandleRetryRegister(payload)),
            "wss.retry.reset" => Task.FromResult(HandleRetryReset(payload)),

            // Timeout
            "wss.timeout.register" => Task.FromResult(HandleTimeoutRegister(payload)),
            "wss.timeout.check" => Task.FromResult(HandleTimeoutCheck(payload)),
            "wss.timeout.clear" => Task.FromResult(HandleTimeoutClear(payload)),

            // Lifecycle
            "wss.lifecycle.start" => HandleLifecycleStart(payload),
            "wss.lifecycle.advance" => HandleLifecycleAdvance(payload),
            "wss.lifecycle.complete" => HandleLifecycleComplete(payload),
            "wss.lifecycle.fail" => HandleLifecycleFail(payload),
            "wss.lifecycle.terminate" => HandleLifecycleTerminate(payload),

            _ => Task.FromResult(DispatchResult.Fail($"Unknown WSS command: {command}"))
        };
    }

    // ── Definitions ──────────────────────────────────────────────────────

    private DispatchResult HandleDefinitionList()
    {
        var engine = new WorkflowDefinitionEngine(null);
        var definitions = engine.ListWorkflowDefinitions();

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["workflows"] = definitions.Select(d => new Dictionary<string, object>
            {
                ["workflowId"] = d.WorkflowId,
                ["name"] = d.Name,
                ["description"] = d.Description,
                ["version"] = d.Version,
                ["steps"] = d.Steps,
                ["createdAt"] = d.CreatedAt
            }).ToList()
        });
    }

    private DispatchResult HandleDefinitionGet(Dictionary<string, object> payload)
    {
        var engine = new WorkflowDefinitionEngine(null);
        var workflowId = (string)payload["workflowId"];
        var definition = engine.GetWorkflowDefinition(workflowId);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["workflowId"] = definition.WorkflowId,
            ["name"] = definition.Name,
            ["description"] = definition.Description,
            ["version"] = definition.Version,
            ["steps"] = definition.Steps,
            ["createdAt"] = definition.CreatedAt
        });
    }

    private DispatchResult HandleDefinitionRegister(Dictionary<string, object> payload)
    {
        var engine = new WorkflowDefinitionEngine(null);

        var workflowId = (string)payload["workflowId"];
        var name = (string)payload["name"];
        var description = (string)payload["description"];
        var version = (string)payload["version"];
        var steps = BuildSteps(payload);

        var definition = engine.RegisterWorkflowDefinition(workflowId, name, description, version, steps);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["workflowId"] = definition.WorkflowId,
            ["name"] = definition.Name,
            ["description"] = definition.Description,
            ["version"] = definition.Version,
            ["steps"] = definition.Steps,
            ["createdAt"] = definition.CreatedAt
        });
    }

    // ── Graph ────────────────────────────────────────────────────────────

    private DispatchResult HandleGraphBuild(Dictionary<string, object> payload)
    {
        var workflowId = (string)payload["workflowId"];
        var engine = new WorkflowDefinitionEngine(null);
        var definition = engine.GetWorkflowDefinition(workflowId);

        var graphEngine = new WorkflowGraphEngine();
        var stepDefs = definition.Steps.Select(s => new WorkflowStepDefinition(
            s.StepId, s.Name, s.EngineName, "", s.NextSteps, null)).ToList();
        var graph = graphEngine.BuildGraph(stepDefs);
        var startSteps = graphEngine.GetStartSteps(graph);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["workflowId"] = workflowId,
            ["transitions"] = graph.Transitions,
            ["startSteps"] = startSteps
        });
    }

    private DispatchResult HandleGraphValidate(Dictionary<string, object> payload)
    {
        var workflowId = (string)payload["workflowId"];
        var transitions = (Dictionary<string, List<string>>)payload["transitions"];

        var readOnlyTransitions = transitions.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<string>)kv.Value) as IReadOnlyDictionary<string, IReadOnlyList<string>>;

        var graphEngine = new WorkflowGraphEngine();
        var violations = graphEngine.ValidateGraph(new WorkflowGraph(workflowId, readOnlyTransitions));

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["workflowId"] = workflowId,
            ["isValid"] = violations.Count == 0,
            ["violations"] = violations
        });
    }

    // ── Templates ────────────────────────────────────────────────────────

    private DispatchResult HandleTemplateList()
    {
        var graphEngine = new WorkflowGraphEngine();
        var engine = new WorkflowTemplateEngine(null, graphEngine);
        var templates = engine.ListTemplates();

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["templates"] = templates
        });
    }

    private DispatchResult HandleTemplateGet(Dictionary<string, object> payload)
    {
        var graphEngine = new WorkflowGraphEngine();
        var engine = new WorkflowTemplateEngine(null, graphEngine);
        var templateId = (string)payload["templateId"];
        var template = engine.GetTemplate(templateId);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["templateId"] = template.TemplateId,
            ["name"] = template.Name,
            ["version"] = template.Version,
            ["description"] = template.Description,
            ["steps"] = template.Steps,
            ["graph"] = template.Graph
        });
    }

    private DispatchResult HandleTemplateRegister(Dictionary<string, object> payload)
    {
        var graphEngine = new WorkflowGraphEngine();
        var engine = new WorkflowTemplateEngine(null, graphEngine);

        var templateId = (string)payload["templateId"];
        var name = (string)payload["name"];
        var version = (int)payload["version"];
        var description = (string)payload["description"];
        var steps = (List<Dictionary<string, object>>)payload["steps"];
        var transitions = (Dictionary<string, List<string>>)payload["transitions"];

        var templateSteps = steps.Select(s => new WorkflowTemplateStep(
            (string)s["stepId"],
            s.TryGetValue("description", out var desc) ? (string)desc : "",
            (string)s["engine"],
            (string)s["command"],
            s.TryGetValue("parameters", out var p) ? (IReadOnlyDictionary<string, string>)p : new Dictionary<string, string>(),
            null)).ToList();

        var readOnlyTransitions = transitions.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<string>)kv.Value) as IReadOnlyDictionary<string, IReadOnlyList<string>>;

        var template = new WorkflowTemplate(templateId, name, version, description, templateSteps, new WorkflowGraph(templateId, readOnlyTransitions));
        engine.RegisterTemplate(template);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["templateId"] = template.TemplateId,
            ["name"] = template.Name,
            ["version"] = template.Version,
            ["description"] = template.Description,
            ["steps"] = template.Steps,
            ["graph"] = template.Graph
        });
    }

    private DispatchResult HandleTemplateGenerate(Dictionary<string, object> payload)
    {
        var graphEngine = new WorkflowGraphEngine();
        var engine = new WorkflowTemplateEngine(null, graphEngine);

        var templateId = (string)payload["templateId"];
        var parameters = (Dictionary<string, string>)payload["parameters"];

        var definition = engine.GenerateWorkflowDefinition(templateId, parameters);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["workflowId"] = definition.WorkflowId,
            ["name"] = definition.Name,
            ["description"] = definition.Description,
            ["version"] = definition.Version,
            ["steps"] = definition.Steps,
            ["createdAt"] = definition.CreatedAt
        });
    }

    // ── Registry ─────────────────────────────────────────────────────────

    private DispatchResult HandleRegistryList()
    {
        var workflows = _workflowRegistry.ListWorkflows();

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["workflows"] = workflows.Select(d => new Dictionary<string, object>
            {
                ["workflowId"] = d.WorkflowId,
                ["name"] = d.Name,
                ["description"] = d.Description,
                ["version"] = d.Version,
                ["steps"] = d.Steps,
                ["createdAt"] = d.CreatedAt
            }).ToList()
        });
    }

    private DispatchResult HandleRegistryGet(Dictionary<string, object> payload)
    {
        var workflowId = (string)payload["workflowId"];
        var workflow = _workflowRegistry.GetWorkflow(workflowId);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["workflowId"] = workflow.WorkflowId,
            ["name"] = workflow.Name,
            ["description"] = workflow.Description,
            ["version"] = workflow.Version,
            ["steps"] = workflow.Steps,
            ["createdAt"] = workflow.CreatedAt
        });
    }

    private DispatchResult HandleRegistryRegister(Dictionary<string, object> payload)
    {
        var definition = BuildWorkflowDefinition(payload);
        _workflowRegistry.RegisterWorkflow(definition);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["workflowId"] = definition.WorkflowId,
            ["name"] = definition.Name,
            ["description"] = definition.Description,
            ["version"] = definition.Version,
            ["steps"] = definition.Steps,
            ["createdAt"] = definition.CreatedAt
        });
    }

    private DispatchResult HandleRegistryRemove(Dictionary<string, object> payload)
    {
        var workflowId = (string)payload["workflowId"];
        _workflowRegistry.RemoveWorkflow(workflowId);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["workflowId"] = workflowId,
            ["removed"] = true
        });
    }

    // ── Versioning ───────────────────────────────────────────────────────

    private DispatchResult HandleVersionList(Dictionary<string, object> payload)
    {
        var engine = new WorkflowVersioningEngine(null);
        var workflowId = (string)payload["workflowId"];
        var versions = engine.ListWorkflowVersions(workflowId);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["workflowId"] = workflowId,
            ["versions"] = versions.Select(v => new Dictionary<string, object>
            {
                ["workflowId"] = v.WorkflowId,
                ["name"] = v.Name,
                ["description"] = v.Description,
                ["version"] = v.Version,
                ["steps"] = v.Steps,
                ["createdAt"] = v.CreatedAt
            }).ToList()
        });
    }

    private DispatchResult HandleVersionGet(Dictionary<string, object> payload)
    {
        var engine = new WorkflowVersioningEngine(null);
        var workflowId = (string)payload["workflowId"];
        var version = (string)payload["version"];
        var definition = engine.GetWorkflowVersion(workflowId, version);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["workflowId"] = definition.WorkflowId,
            ["name"] = definition.Name,
            ["description"] = definition.Description,
            ["version"] = definition.Version,
            ["steps"] = definition.Steps,
            ["createdAt"] = definition.CreatedAt
        });
    }

    private DispatchResult HandleVersionLatest(Dictionary<string, object> payload)
    {
        var engine = new WorkflowVersioningEngine(null);
        var workflowId = (string)payload["workflowId"];
        var definition = engine.GetLatestWorkflow(workflowId);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["workflowId"] = definition.WorkflowId,
            ["name"] = definition.Name,
            ["description"] = definition.Description,
            ["version"] = definition.Version,
            ["steps"] = definition.Steps,
            ["createdAt"] = definition.CreatedAt
        });
    }

    private DispatchResult HandleVersionRegister(Dictionary<string, object> payload)
    {
        var engine = new WorkflowVersioningEngine(null);
        var definition = BuildWorkflowDefinition(payload);
        var registered = engine.RegisterWorkflowVersion(definition);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["workflowId"] = registered.WorkflowId,
            ["name"] = registered.Name,
            ["description"] = registered.Description,
            ["version"] = registered.Version,
            ["steps"] = registered.Steps,
            ["createdAt"] = registered.CreatedAt
        });
    }

    // ── Validation ───────────────────────────────────────────────────────

    private DispatchResult HandleValidationWorkflow(Dictionary<string, object> payload)
    {
        var definition = BuildWorkflowDefinition(payload);
        var orchestrator = CreateValidationOrchestrator();
        var result = orchestrator.ValidateCompleteWorkflow(definition);

        return FormatValidationResult(result);
    }

    private DispatchResult HandleValidationTemplate(Dictionary<string, object> payload)
    {
        var templateId = (string)payload["templateId"];
        var parameters = (Dictionary<string, string>)payload["parameters"];

        var orchestrator = CreateValidationOrchestrator();
        var result = orchestrator.ValidateWorkflowTemplate(templateId, parameters);

        return FormatValidationResult(result);
    }

    private DispatchResult HandleValidationVersion(Dictionary<string, object> payload)
    {
        var workflowId = (string)payload["workflowId"];
        var version = (string)payload["version"];

        var orchestrator = CreateValidationOrchestrator();
        var result = orchestrator.ValidateWorkflowVersion(workflowId, version);

        return FormatValidationResult(result);
    }

    // ── Dependency ───────────────────────────────────────────────────────

    private DispatchResult HandleDependencyAnalyze(Dictionary<string, object> payload)
    {
        var definition = BuildWorkflowDefinition(payload);
        var analyzer = new WorkflowDependencyAnalyzer(null);
        var result = analyzer.AnalyzeWorkflowDependencies(definition);

        return FormatDependencyResult(result);
    }

    private DispatchResult HandleDependencyGet(Dictionary<string, object> payload)
    {
        var workflowId = (string)payload["workflowId"];
        var engine = new WorkflowDefinitionEngine(null);
        var definition = engine.GetWorkflowDefinition(workflowId);

        var analyzer = new WorkflowDependencyAnalyzer(null);
        var result = analyzer.AnalyzeWorkflowDependencies(definition);

        return FormatDependencyResult(result);
    }

    private DispatchResult HandleDependencyResolve(Dictionary<string, object> payload)
    {
        var definition = BuildWorkflowDefinition(payload);
        var analyzer = new WorkflowDependencyAnalyzer(null);
        var executionOrder = analyzer.ResolveExecutionOrder(definition);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["workflowId"] = definition.WorkflowId,
            ["executionOrder"] = executionOrder
        });
    }

    // ── Engine Mapping ───────────────────────────────────────────────────

    private DispatchResult HandleEngineList()
    {
        var mapper = new WorkflowStepEngineMapper(null);
        var engines = mapper.ListEngines();

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["engines"] = engines
        });
    }

    private DispatchResult HandleEngineRegister(Dictionary<string, object> payload)
    {
        var mapper = new WorkflowStepEngineMapper(null);
        var engineName = (string)payload["engineName"];
        var runtimeIdentifier = (string)payload["runtimeIdentifier"];

        mapper.RegisterEngine(engineName, runtimeIdentifier);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["engineName"] = engineName,
            ["runtimeIdentifier"] = runtimeIdentifier,
            ["registered"] = true
        });
    }

    private DispatchResult HandleEngineResolve(Dictionary<string, object> payload)
    {
        var mapper = new WorkflowStepEngineMapper(null);
        var engineName = (string)payload["engineName"];
        var exists = mapper.EngineExists(engineName);

        if (!exists)
        {
            return DispatchResult.Ok(new Dictionary<string, object>
            {
                ["engineName"] = engineName,
                ["exists"] = false,
                ["runtimeIdentifier"] = ""
            });
        }

        var runtimeIdentifier = mapper.ResolveEngine(engineName);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["engineName"] = engineName,
            ["exists"] = true,
            ["runtimeIdentifier"] = runtimeIdentifier
        });
    }

    // ── Instance ─────────────────────────────────────────────────────────

    private DispatchResult HandleInstanceList()
    {
        var registry = new WorkflowInstanceRegistry(null);
        var instances = registry.ListInstances();

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["instances"] = instances.Select(i => new Dictionary<string, object>
            {
                ["instanceId"] = i.InstanceId,
                ["workflowId"] = i.WorkflowId,
                ["version"] = i.WorkflowVersion,
                ["currentStep"] = i.CurrentStep,
                ["status"] = i.Status.ToString(),
                ["createdAt"] = i.StartedAt,
                ["completedAt"] = (object?)i.CompletedAt ?? "",
                ["context"] = i.Context
            }).ToList()
        });
    }

    private DispatchResult HandleInstanceGet(Dictionary<string, object> payload)
    {
        var registry = new WorkflowInstanceRegistry(null);
        var instanceId = (string)payload["instanceId"];
        var instance = registry.GetInstance(instanceId);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["instanceId"] = instance.InstanceId,
            ["workflowId"] = instance.WorkflowId,
            ["version"] = instance.WorkflowVersion,
            ["currentStep"] = instance.CurrentStep,
            ["status"] = instance.Status.ToString(),
            ["createdAt"] = instance.StartedAt,
            ["completedAt"] = (object?)instance.CompletedAt ?? "",
            ["context"] = instance.Context
        });
    }

    private DispatchResult HandleInstanceCreate(Dictionary<string, object> payload)
    {
        var registry = new WorkflowInstanceRegistry(null);
        var workflowId = (string)payload["workflowId"];
        var version = (string)payload["version"];
        var context = payload.TryGetValue("context", out var ctx) ? ctx as Dictionary<string, object> : null;

        var instance = registry.CreateInstance(workflowId, version, context);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["instanceId"] = instance.InstanceId,
            ["workflowId"] = instance.WorkflowId,
            ["version"] = instance.WorkflowVersion,
            ["currentStep"] = instance.CurrentStep,
            ["status"] = instance.Status.ToString(),
            ["createdAt"] = instance.StartedAt
        });
    }

    private DispatchResult HandleInstanceUpdate(Dictionary<string, object> payload)
    {
        var registry = new WorkflowInstanceRegistry(null);
        var instanceId = (string)payload["instanceId"];
        var currentStep = (string)payload["currentStep"];
        var status = (WorkflowInstanceStatus)payload["status"];

        var instance = registry.UpdateInstanceState(instanceId, currentStep, status);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["instanceId"] = instance.InstanceId,
            ["workflowId"] = instance.WorkflowId,
            ["version"] = instance.WorkflowVersion,
            ["currentStep"] = instance.CurrentStep,
            ["status"] = instance.Status.ToString(),
            ["createdAt"] = instance.StartedAt,
            ["completedAt"] = (object?)instance.CompletedAt ?? ""
        });
    }

    private DispatchResult HandleInstanceRemove(Dictionary<string, object> payload)
    {
        var registry = new WorkflowInstanceRegistry(null);
        var instanceId = (string)payload["instanceId"];
        registry.RemoveInstance(instanceId);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["instanceId"] = instanceId,
            ["removed"] = true
        });
    }

    // ── State ────────────────────────────────────────────────────────────

    private DispatchResult HandleStateList()
    {
        var states = _wssWorkflowStateStore.ListActiveStates();

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["states"] = states.Select(s => new Dictionary<string, object>
            {
                ["instanceId"] = s.InstanceId,
                ["workflowId"] = s.WorkflowId,
                ["workflowVersion"] = s.WorkflowVersion,
                ["currentStep"] = s.CurrentStep,
                ["completedSteps"] = s.CompletedSteps,
                ["status"] = s.Status.ToString(),
                ["startedAt"] = s.StartedAt,
                ["updatedAt"] = s.UpdatedAt
            }).ToList()
        });
    }

    private DispatchResult HandleStateGet(Dictionary<string, object> payload)
    {
        var instanceId = (string)payload["instanceId"];
        var state = _wssWorkflowStateStore.GetState(instanceId);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["instanceId"] = state.InstanceId,
            ["workflowId"] = state.WorkflowId,
            ["workflowVersion"] = state.WorkflowVersion,
            ["currentStep"] = state.CurrentStep,
            ["completedSteps"] = state.CompletedSteps,
            ["status"] = state.Status.ToString(),
            ["startedAt"] = state.StartedAt,
            ["updatedAt"] = state.UpdatedAt,
            ["executionContext"] = state.ExecutionContext
        });
    }

    private DispatchResult HandleStateSave(Dictionary<string, object> payload)
    {
        var instanceId = (string)payload["instanceId"];
        var workflowId = (string)payload["workflowId"];
        var workflowVersion = (string)payload["workflowVersion"];
        var executionContext = payload.TryGetValue("executionContext", out var ctx)
            ? ctx as IReadOnlyDictionary<string, object> ?? new Dictionary<string, object>()
            : new Dictionary<string, object>();

        var state = new WorkflowState(
            instanceId,
            workflowId,
            workflowVersion,
            "",
            Array.Empty<string>(),
            WorkflowInstanceStatus.Created,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            executionContext);

        _wssWorkflowStateStore.SaveState(state);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["instanceId"] = state.InstanceId,
            ["workflowId"] = state.WorkflowId,
            ["workflowVersion"] = state.WorkflowVersion,
            ["status"] = state.Status.ToString(),
            ["startedAt"] = state.StartedAt
        });
    }

    private DispatchResult HandleStateUpdate(Dictionary<string, object> payload)
    {
        var instanceId = (string)payload["instanceId"];
        var currentStep = (string)payload["currentStep"];
        var status = (WorkflowInstanceStatus)payload["status"];

        var updated = _wssWorkflowStateStore.UpdateState(instanceId, currentStep, status);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["instanceId"] = updated.InstanceId,
            ["workflowId"] = updated.WorkflowId,
            ["currentStep"] = updated.CurrentStep,
            ["status"] = updated.Status.ToString(),
            ["updatedAt"] = updated.UpdatedAt
        });
    }

    private DispatchResult HandleStateDelete(Dictionary<string, object> payload)
    {
        var instanceId = (string)payload["instanceId"];
        _wssWorkflowStateStore.DeleteState(instanceId);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["instanceId"] = instanceId,
            ["deleted"] = true
        });
    }

    // ── Events ───────────────────────────────────────────────────────────

    private async Task<DispatchResult> HandleEventsPublish(Dictionary<string, object> payload)
    {
        var eventType = (string)payload["eventType"];
        var workflowId = (string)payload["workflowId"];
        var instanceId = (string)payload["instanceId"];
        var eventPayload = payload.TryGetValue("payload", out var p) ? p as IDictionary<string, object> : null;

        await _wssWorkflowEventRouter.PublishEvent(eventType, workflowId, instanceId, eventPayload);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["eventType"] = eventType,
            ["workflowId"] = workflowId,
            ["instanceId"] = instanceId,
            ["published"] = true
        });
    }

    private DispatchResult HandleEventsTypes()
    {
        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["eventTypes"] = WorkflowEventTypes.All
        });
    }

    // ── Retry ────────────────────────────────────────────────────────────

    private DispatchResult HandleRetryGet(Dictionary<string, object> payload)
    {
        var instanceId = (string)payload["instanceId"];
        var stepId = (string)payload["stepId"];
        var count = _workflowRetryPolicyEngine.GetRetryCount(instanceId, stepId);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["instanceId"] = instanceId,
            ["stepId"] = stepId,
            ["retryCount"] = count
        });
    }

    private DispatchResult HandleRetryRegister(Dictionary<string, object> payload)
    {
        var instanceId = (string)payload["instanceId"];
        var stepId = (string)payload["stepId"];

        _workflowRetryPolicyEngine.RegisterRetryAttempt(instanceId, stepId);
        var count = _workflowRetryPolicyEngine.GetRetryCount(instanceId, stepId);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["instanceId"] = instanceId,
            ["stepId"] = stepId,
            ["retryCount"] = count
        });
    }

    private DispatchResult HandleRetryReset(Dictionary<string, object> payload)
    {
        var instanceId = (string)payload["instanceId"];
        var stepId = (string)payload["stepId"];

        _workflowRetryPolicyEngine.ResetRetryCount(instanceId, stepId);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["instanceId"] = instanceId,
            ["stepId"] = stepId,
            ["retryCount"] = 0
        });
    }

    // ── Timeout ──────────────────────────────────────────────────────────

    private DispatchResult HandleTimeoutRegister(Dictionary<string, object> payload)
    {
        var instanceId = (string)payload["instanceId"];
        var stepId = payload.TryGetValue("stepId", out var sid) ? sid as string : null;
        var timeoutSecondsObj = payload["timeoutSeconds"];
        var timeoutSeconds = timeoutSecondsObj is int ts ? ts : (timeoutSecondsObj is double td ? (int)td : 30);

        if (string.IsNullOrWhiteSpace(stepId))
        {
            _workflowTimeoutEngine.RegisterWorkflowTimeout(instanceId, TimeSpan.FromSeconds(timeoutSeconds));
        }
        else
        {
            _workflowTimeoutEngine.RegisterStepTimeout(instanceId, stepId, TimeSpan.FromSeconds(timeoutSeconds));
        }

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["instanceId"] = instanceId,
            ["stepId"] = stepId ?? "workflow",
            ["timeoutSeconds"] = timeoutSeconds,
            ["registered"] = true
        });
    }

    private DispatchResult HandleTimeoutCheck(Dictionary<string, object> payload)
    {
        var instanceId = (string)payload["instanceId"];
        var stepId = payload.TryGetValue("stepId", out var sid) ? sid as string : null;

        TimeoutDecision decision;
        if (string.IsNullOrWhiteSpace(stepId))
        {
            decision = _workflowTimeoutEngine.CheckWorkflowTimeout(instanceId);
        }
        else
        {
            decision = _workflowTimeoutEngine.CheckStepTimeout(instanceId, stepId);
        }

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["isTimeout"] = decision.IsTimeout,
            ["instanceId"] = decision.InstanceId,
            ["stepId"] = decision.StepId,
            ["timeoutDuration"] = decision.TimeoutDuration.TotalSeconds,
            ["exceededBy"] = decision.ExceededBy.TotalSeconds
        });
    }

    private DispatchResult HandleTimeoutClear(Dictionary<string, object> payload)
    {
        var instanceId = (string)payload["instanceId"];
        var stepId = (string)payload["stepId"];

        _workflowTimeoutEngine.ClearTimeout(instanceId, stepId);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["instanceId"] = instanceId,
            ["stepId"] = stepId,
            ["cleared"] = true
        });
    }

    // ── Lifecycle ────────────────────────────────────────────────────────

    private async Task<DispatchResult> HandleLifecycleStart(Dictionary<string, object> payload)
    {
        var workflowId = (string)payload["workflowId"];
        var version = (string)payload["version"];
        var context = payload.TryGetValue("context", out var ctx) ? ctx as IDictionary<string, object> : null;

        var decision = await _workflowLifecycleEngine.StartWorkflow(workflowId, version, context);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["instanceId"] = decision.InstanceId,
            ["nextStep"] = decision.NextStep ?? "",
            ["status"] = decision.Status.ToString(),
            ["reason"] = decision.Reason ?? ""
        });
    }

    private async Task<DispatchResult> HandleLifecycleAdvance(Dictionary<string, object> payload)
    {
        var instanceId = (string)payload["instanceId"];
        var decision = await _workflowLifecycleEngine.AdvanceStep(instanceId);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["instanceId"] = decision.InstanceId,
            ["currentStep"] = decision.CurrentStep ?? "",
            ["nextStep"] = decision.NextStep ?? "",
            ["status"] = decision.Status.ToString(),
            ["reason"] = decision.Reason ?? ""
        });
    }

    private async Task<DispatchResult> HandleLifecycleComplete(Dictionary<string, object> payload)
    {
        var instanceId = (string)payload["instanceId"];
        var stepId = payload.TryGetValue("stepId", out var sid) ? sid as string : null;

        LifecycleDecision decision;
        if (string.IsNullOrWhiteSpace(stepId))
        {
            decision = await _workflowLifecycleEngine.CompleteWorkflow(instanceId);
        }
        else
        {
            decision = await _workflowLifecycleEngine.CompleteStep(instanceId, stepId);
        }

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["instanceId"] = decision.InstanceId,
            ["currentStep"] = decision.CurrentStep ?? "",
            ["nextStep"] = decision.NextStep ?? "",
            ["status"] = decision.Status.ToString(),
            ["reason"] = decision.Reason ?? ""
        });
    }

    private async Task<DispatchResult> HandleLifecycleFail(Dictionary<string, object> payload)
    {
        var instanceId = (string)payload["instanceId"];
        var stepId = (string)payload["stepId"];
        var reason = (string)payload["reason"];

        var decision = await _workflowLifecycleEngine.FailStep(instanceId, stepId, reason);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["instanceId"] = decision.InstanceId,
            ["currentStep"] = decision.CurrentStep ?? "",
            ["nextStep"] = decision.NextStep ?? "",
            ["status"] = decision.Status.ToString(),
            ["reason"] = decision.Reason ?? ""
        });
    }

    private async Task<DispatchResult> HandleLifecycleTerminate(Dictionary<string, object> payload)
    {
        var instanceId = (string)payload["instanceId"];
        var decision = await _workflowLifecycleEngine.TerminateWorkflow(instanceId);

        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["instanceId"] = decision.InstanceId,
            ["currentStep"] = decision.CurrentStep ?? "",
            ["status"] = decision.Status.ToString(),
            ["reason"] = decision.Reason ?? ""
        });
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private WorkflowValidationOrchestrator CreateValidationOrchestrator()
    {
        var graphEngine = new WorkflowGraphEngine();
        var definitionEngine = new WorkflowDefinitionEngine(null);
        var templateEngine = new WorkflowTemplateEngine(null, graphEngine);
        var versioningEngine = new WorkflowVersioningEngine(null);
        return new WorkflowValidationOrchestrator(definitionEngine, graphEngine, templateEngine, versioningEngine);
    }

    private WorkflowDefinition BuildWorkflowDefinition(Dictionary<string, object> payload)
    {
        var workflowId = (string)payload["workflowId"];
        var name = (string)payload["name"];
        var description = (string)payload["description"];
        var version = (string)payload["version"];
        var steps = BuildSteps(payload);

        return new WorkflowDefinition(workflowId, name, description, version, steps, DateTimeOffset.UtcNow);
    }

    private static IReadOnlyList<WorkflowStep> BuildSteps(Dictionary<string, object> payload)
    {
        var rawSteps = (List<Dictionary<string, object>>)payload["steps"];

        return rawSteps.Select(s => new WorkflowStep(
            (string)s["stepId"],
            (string)s["name"],
            (string)s["engineName"],
            s.TryGetValue("nextSteps", out var ns) ? (List<string>)ns : new List<string>()
        )).ToList();
    }

    private static DispatchResult FormatValidationResult(WorkflowValidationResult result)
    {
        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["isValid"] = result.IsValid,
            ["errors"] = result.Errors.Select(e => new Dictionary<string, object>
            {
                ["code"] = e.Code,
                ["message"] = e.Message,
                ["component"] = e.Component,
                ["stepId"] = (object?)e.StepId ?? ""
            }).ToList(),
            ["warnings"] = result.Warnings.Select(w => new Dictionary<string, object>
            {
                ["code"] = w.Code,
                ["message"] = w.Message,
                ["component"] = w.Component,
                ["stepId"] = (object?)w.StepId ?? ""
            }).ToList()
        });
    }

    private static DispatchResult FormatDependencyResult(WorkflowDependencyResult result)
    {
        return DispatchResult.Ok(new Dictionary<string, object>
        {
            ["workflowId"] = result.WorkflowId,
            ["dependencies"] = result.Dependencies,
            ["executionOrder"] = result.ExecutionOrder,
            ["missingDependencies"] = result.MissingDependencies,
            ["circularDependencies"] = result.CircularDependencies,
            ["externalWorkflowDependencies"] = result.ExternalWorkflowDependencies,
            ["hasIssues"] = result.HasIssues
        });
    }
}
